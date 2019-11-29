using Bond;
using Bond.Tag;
using FLGameLogic;
using FLGameLogicServer;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.Utilities;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    [Schema, BondSerializationTag("#g")]
    public class GameGrainState : IOnDeserializedHandler
    {
        [Id(0)]
        public SerializedGameData GameData { get; set; }

        [Id(1)]
        public Guid[] PlayerIDs { get; set; }

        [Id(2)]
        public int[] LastProcessedEndTurns { get; set; } //?? use to reprocess turn end notifications in case grain goes down

        [Id(3)]
        public int GroupChooser { get; set; } = -1;

        [Id(4)]
        public List<ushort> GroupChoices { get; set; }

        public void OnDeserialized()
        {
            if (PlayerIDs == null)
                PlayerIDs = Array.Empty<Guid>();
            if (LastProcessedEndTurns == null)
                LastProcessedEndTurns = new[] { -1, -1 };
        }
    }

    //?? Now, we only need a way to reactivate these if one of them goes down... Same old challenge.
    //?? cache player names along with games?
    class Game : Grain, IGame
    {
        class EndRoundTimerData
        {
            public int playerIndex;
            public int roundIndex;
            public IDisposable timerHandle;
        }

        GameLogicServer gameLogic;
        readonly IConfigReader configReader;
        readonly Random random = new Random();
        readonly GrainStateWrapper<GameGrainState> state;

        public Game(IConfigReader configReader, [PersistentState("State")] IPersistentState<GameGrainState> state)
        {
            this.configReader = configReader;
            this.state = new GrainStateWrapper<GameGrainState>(state);
            this.state.Persist += State_Persist;
        }

        private void State_Persist(object sender, PersistStateEventArgs<GameGrainState> e) =>
            e.State.GameData = gameLogic?.Serialize();

        int NumJoinedPlayers => state.UseState(state => state.PlayerIDs.Length == 0 ? 0 : state.PlayerIDs[1] == Guid.Empty ? 1 : 2);

        public override async Task OnActivateAsync()
        {
            state.UseState(state =>
            {
                if (state.GameData != null)
                {
                    var config = configReader.Config;
                    gameLogic = GameLogicServer.DeserializeFrom(state.GameData, c =>
                    {
                        if (c == null)
                            return null;

                        if (config.CategoriesAsGameLogicFormatByName.TryGetValue(c, out var category))
                            return category;

                        // In case a category was deleted...
                        return config.CategoriesAsGameLogicFormat[random.Next(config.CategoriesAsGameLogicFormat.Count)];
                    });
                }
            });

            var numPlayers = NumJoinedPlayers;
            if (numPlayers >= 1)
                await SetTimerOrProcessEndTurnIfNecessary(0);
            if (numPlayers >= 2)
                await SetTimerOrProcessEndTurnIfNecessary(1);
        }

        async Task SetTimerOrProcessEndTurnIfNecessary(int playerIndex)
        {
            var now = DateTime.Now;
            var roundIndex = gameLogic.NumTurnsTakenByIncludingCurrent(playerIndex) - 1;
            if (gameLogic.IsTurnInProgress(playerIndex, now))
                SetEndTurnTimerImpl(playerIndex, gameLogic.GetTurnEndTime(playerIndex) - now, roundIndex);
            else if (state.UseState(state => state.LastProcessedEndTurns[playerIndex]) < roundIndex)
                await HandleEndTurn(playerIndex, roundIndex);
        }

        int Index(Guid playerID) =>
            state.UseState(state =>
                state.PlayerIDs[0] == playerID ? 0 : (state.PlayerIDs[1] == playerID ? 1 : throw new Exception("Unknown player ID " + playerID.ToString()))
            );

        public Task<byte> StartNew(Guid playerOneID)
        {
            return state.UseStateAndPersist(state =>
            {
                if (GetStateInternal(state) != GameState.New)
                    throw new VerbatimException("Game already started");

                var config = configReader.Config;

                gameLogic = new GameLogicServer(config.ConfigValues.NumRoundsPerGame);

                state.PlayerIDs = new[] { playerOneID, Guid.Empty };

                return (byte)gameLogic.NumRounds;
            });
        }

        public Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(PlayerInfo playerTwo) =>
            state.UseStateAndPersist(async state =>
            {
                if (state.PlayerIDs[0] == playerTwo.ID)
                    throw new VerbatimException("Player cannot join game with self");

                state.PlayerIDs[1] = playerTwo.ID;

                await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentJoined(state.PlayerIDs[0], this.GetPrimaryKey(), playerTwo);

                return (state.PlayerIDs[0], (byte)gameLogic.NumRounds);
            });

        (bool shouldChooseCategory, string category, int roundIndex, TimeSpan? roundTime) StartRound(int playerIndex)
        {
            var configValues = configReader.Config.ConfigValues;
            var roundTime = configValues.ClientTimePerRound + configValues.ExtraTimePerRound;

            int roundIndex = gameLogic.NumTurnsTakenBy(playerIndex);
            var result = gameLogic.StartRound(playerIndex, roundTime, out var category);

            if (result == StartRoundResult.MustChooseCategory)
                return (true, default(string), roundIndex, default(TimeSpan?));

            if (!result.IsSuccess())
                throw new Exception("Failed to start round, resulting in " + result.ToString());

            SetEndTurnTimerImpl(playerIndex, roundTime, roundIndex);

            return (false, category, roundIndex, (TimeSpan?)(configValues.ClientTimePerRound));
        }

        private void SetEndTurnTimerImpl(int playerIndex, TimeSpan roundTime, int roundIndex)
        {
            var endRoundData = new EndRoundTimerData { playerIndex = playerIndex, roundIndex = roundIndex };
            var timerHandle = RegisterTimer(OnTurnEnded, endRoundData, roundTime, TimeSpan.MaxValue);
            endRoundData.timerHandle = timerHandle;
        }

        public async Task<(string category, bool? haveAnswers, TimeSpan? roundTime, bool mustChooseGroup, IEnumerable<GroupInfoDTO> groups)> StartRound(Guid id)
        {
            var index = Index(id);

            var (mustChooseCategory, category, _, roundTime) = StartRound(index);

            if (mustChooseCategory)
            {
                return await state.UseStateAndPersist(state =>
                {
                    var config = configReader.Config;
                    if (state.GroupChoices == null || state.GroupChooser != index)
                    {
                        state.GroupChooser = index;
                        state.GroupChoices =
                            new Random().GetUnique(0, config.Groups.Count, config.ConfigValues.NumGroupChoices)
                            .Select(i => config.Groups[i].ID).ToList();
                    }
                    return (default(string), default(bool?), default(TimeSpan?), true, state.GroupChoices.Select(i => (GroupInfoDTO)config.GroupsByID[i]).ToList().AsEnumerable());
                });
            }
            else
                return (category, await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime, false, Enumerable.Empty<GroupInfoDTO>());
        }

        public async Task<(string category, bool haveAnswers, TimeSpan roundTime)> ChooseGroup(Guid id, ushort groupID)
        {
            var index = Index(id);

            var (mustChooseCategory, category, roundIndex, roundTime) = StartRound(index);
            if (!mustChooseCategory)
                return (category, await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime.Value);

            state.UseState(state =>
            {
                if (state.GroupChooser != index || state.GroupChoices == null)
                    throw new VerbatimException("Not this player's turn to choose a group");

                if (!state.GroupChoices.Contains(groupID))
                    throw new VerbatimException($"Specified group {groupID} is not a valid choice out of ({string.Join(", ", state.GroupChoices)})");
            });

            GrainFactory.GetGrain<IPlayer>(id).AddStats(new List<StatisticValue> { new StatisticValue(Statistics.GroupChosen_Param, groupID, 1) }).Ignore();

            var config = configReader.Config;

            var categories = config.CategoryNamesByGroupID[groupID];
            var random = new Random();
            var currentCategories = gameLogic.CategoryNames;
            string categoryName;
            do
                categoryName = categories[random.Next(categories.Count)];
            while (currentCategories.Contains(categoryName));

            var result = gameLogic.SetCategory(roundIndex, config.CategoriesAsGameLogicFormatByName[categoryName]);

            if (!result.IsSuccess())
                throw new VerbatimException($"Failed to set category, result is {result}");

            (mustChooseCategory, category, _, roundTime) = StartRound(index);
            if (mustChooseCategory)
                throw new VerbatimException("Still need to choose category after setting it once");

            await state.UseStateAndPersist(state =>
            {
                state.GroupChooser = -1;
                state.GroupChoices = null;
            });

            return (category, await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime.Value);
        }

        Task OnTurnEnded(object state)
        {
            var data = (EndRoundTimerData)state;
            data.timerHandle.Dispose();

            return HandleEndTurn(data.playerIndex, data.roundIndex);
        }

        private Task HandleEndTurn(int playerIndex, int roundIndex) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (state.LastProcessedEndTurns[playerIndex] >= roundIndex)
                    return false;

                state.LastProcessedEndTurns[playerIndex] = roundIndex;

                var opponentFinishedThisRound = gameLogic.PlayerFinishedTurn(1 - playerIndex, roundIndex);

                var myID = this.GetPrimaryKey();

                var sentEndTurn = await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentTurnEnded(state.PlayerIDs[1 - playerIndex], myID, (byte)roundIndex,
                    opponentFinishedThisRound ? gameLogic.GetPlayerAnswers(playerIndex, roundIndex).Select(w => (WordScorePairDTO)w).ToList() : null);

                if (!sentEndTurn && !opponentFinishedThisRound)
                    await GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1 - playerIndex]).SendMyTurnStartedNotification(state.PlayerIDs[playerIndex]);

                GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[playerIndex]).OnRoundCompleted(this.AsReference<IGame>(), gameLogic.GetPlayerScores(playerIndex)[roundIndex]).Ignore();

                if (opponentFinishedThisRound)
                {
                    var score0 = gameLogic.GetPlayerScores(0)[roundIndex];
                    var score1 = gameLogic.GetPlayerScores(1)[roundIndex];

                    var category = gameLogic.Categories[roundIndex].CategoryName;
                    var groupID = configReader.Config.CategoriesByName[category].Group.ID;

                    GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]).OnRoundResult(this.AsReference<IGame>(), CompetitionResultHelper.Get(score0, score1), groupID).Ignore();
                    GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1]).OnRoundResult(this.AsReference<IGame>(), CompetitionResultHelper.Get(score1, score0), groupID).Ignore();
                }

                if (gameLogic.Finished)
                {
                    var wins0 = gameLogic.GetNumRoundsWon(0);
                    var wins1 = gameLogic.GetNumRoundsWon(1);

                    var me = this.AsReference<IGame>();
                    IPlayer player0 = GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]);
                    IPlayer player1 = GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1]);

                    var (score0, rank0) = await player0.OnGameResult(me, CompetitionResultHelper.Get(wins0, wins1), wins0);
                    var (score1, rank1) = await player1.OnGameResult(me, CompetitionResultHelper.Get(wins1, wins0), wins1);

                    if (!await GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(state.PlayerIDs[0], myID, wins0, wins1, score0, rank0))
                        await player0.SendGameEndedNotification(state.PlayerIDs[1]);
                    if (!await GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(state.PlayerIDs[1], myID, wins1, wins0, score1, rank1))
                        await player1.SendGameEndedNotification(state.PlayerIDs[0]);

                    // await ClearStateAsync(); // keep game history (separately)
                    DeactivateOnIdle();
                }

                return true;
            });

        public async Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word)
        {
            var index = Index(id);
            var maxEditDistances = configReader.Config.MaxEditDistanceToCorrentByLetterCount;

            var result = await gameLogic.PlayWord(index, word, (c, w) => GetWordScore(gameLogic.RoundNumber, index, c, w), i => maxEditDistances[i]);

            if (result.result == PlayWordResult.Error_TurnOver)
                throw new VerbatimException("Player's turn is already over");

            if (result.result != PlayWordResult.Duplicate)
                GrainFactory.GetGrain<ICategoryStatisticsAggregationWorker>(result.category.CategoryName)
                    .AddDelta(new CategoryStatisticsDelta.WordUsage { Word = result.corrected }).Ignore();

            var stats = new List<StatisticValue>();
            if (result.result == PlayWordResult.Duplicate)
                stats.Add(new StatisticValue(Statistics.WordsPlayedDuplicate, 0, 1));
            else
                stats.Add(new StatisticValue(Statistics.WordsPlayedScore_Param, result.score, 1));

            if (result.corrected != word)
                stats.Add(new StatisticValue(Statistics.WordsCorrected, result.score, 1));

            GrainFactory.GetGrain<IPlayer>(id).AddStats(stats).Ignore();

            return (result.score, result.corrected);
        }

        Task<byte> GetWordScore(int round, int playerIndex, WordCategory category, string word)
        {
            // If other player played this word, we want to make sure we give this one the same score
            if (gameLogic.PlayerFinishedTurn(1 - playerIndex, round))
            {
                var answer = gameLogic.GetPlayerAnswers(1 - playerIndex, round).FirstOrDefault(w => w.word == word);
                if (answer.word != null)
                    return Task.FromResult(answer.score);
            }

            return GrainFactory.GetGrain<ICategoryStatisticsAggregatorCache>(category.CategoryName).GetScore(word);
        }

        public async Task<Immutable<IEnumerable<WordScorePair>>> EndRound(Guid playerID)
        {
            int index = Index(playerID);

            gameLogic.ForceEndTurn(index);

            var turnIndex = gameLogic.NumTurnsTakenBy(index) - 1;

            await HandleEndTurn(index, turnIndex);

            if (gameLogic.PlayerFinishedTurn(1 - index, turnIndex))
                return gameLogic.GetPlayerAnswers(1 - index, turnIndex).AsEnumerable().AsImmutable();
            else
                return default(IEnumerable<WordScorePair>).AsImmutable();
        }

        public Task<TimeSpan?> IncreaseRoundTime(Guid playerID)
        {
            var extension = configReader.Config.ConfigValues.RoundTimeExtension;
            var index = Index(playerID);
            var endTime = gameLogic.ExtendRoundTime(index, extension);
            return Task.FromResult(endTime == null ? default(TimeSpan?) : extension);
        }

        public async Task<(string word, byte wordScore)?> RevealWord(Guid playerID)
        {
            var index = Index(playerID);
            if (!gameLogic.IsTurnInProgress(index))
                return null;

            var turnIndex = gameLogic.NumTurnsTakenBy(index);
            var category = gameLogic.Categories[turnIndex];
            var answers = gameLogic.GetPlayerAnswers(index, turnIndex);

            if (answers.Count == category.Answers.Count)
                return null;

            var random = new Random();
            string word;
            do
                word = category.Answers[random.Next(category.Answers.Count)];
            while (answers.Any(a => a.word == word));

            var (score, _) = await PlayWord(playerID, word);
            return (word, score);
        }

        public Task<List<GroupConfig>> RefreshGroups(Guid guid) =>
            state.UseStateAndMaybePersist(state =>
            {
                var index = Index(guid);

                if (state.GroupChooser != index || state.GroupChoices == null)
                    return Task.FromResult((false, default(List<GroupConfig>)));

                var config = configReader.Config;
                state.GroupChoices =
                    new Random().GetUniqueExcept(0, config.Groups.Count, config.ConfigValues.NumGroupChoices,
                        i => state.GroupChoices.Contains(config.Groups[i].ID), state.GroupChoices.Count)
                    .Select(i => config.Groups[i].ID).ToList();

                return Task.FromResult((true, state.GroupChoices.Select(i => config.GroupsByID[i]).ToList()));
            });

        GameState GetStateInternal(GameGrainState state)
        {
            if (state.PlayerIDs.Length == 0 || state.PlayerIDs.Length == 0)
                return GameState.New;

            if (state.PlayerIDs[1] == Guid.Empty)
                return GameState.WaitingForSecondPlayer;

            if (gameLogic.Finished)
                return GameState.Finished;

            return GameState.InProgress;
        }

        public Task<GameState> GetState() => state.UseState(state => Task.FromResult(GetStateInternal(state)));

        public Task<GameInfo> GetGameInfo(Guid playerID) =>
            state.UseState(async state =>
            {
                if (GetStateInternal(state) == GameState.New)
                    throw new Exception("Game not in progress");

                int index = Index(playerID);
                var turnsTakenInclCurrent = gameLogic.NumTurnsTakenByIncludingCurrent(index);
                var turnsTaken = gameLogic.NumTurnsTakenBy(index);

                var categories = gameLogic.Categories.Take(turnsTakenInclCurrent).Select(c => c.CategoryName).ToList();
                var playerInfo = state.PlayerIDs[1 - index] == Guid.Empty ? null :
                    await PlayerInfoHelper.GetInfo(GrainFactory, state.PlayerIDs[1 - index]);

                var ownedCategories = await GrainFactory.GetGrain<IPlayer>(playerID).HaveAnswersForCategories(categories);

                return new GameInfo
                (
                    otherPlayerInfo: playerInfo,
                    numRounds: (byte)gameLogic.Categories.Count,
                    categories: categories,
                    myWordsPlayed: gameLogic.GetPlayerAnswers(index).Take(turnsTakenInclCurrent).Select(ws => ws.Select(w => (WordScorePairDTO)w).ToList()).ToList(),
                    theirWordsPlayed: gameLogic.GetPlayerAnswers(1 - index)?.Take(turnsTaken).Select(ws => ws.Select(w => (WordScorePairDTO)w).ToList()).ToList(), // don't return words for the round currently in progress
                    myTurnEndTime: gameLogic.GetTurnEndTime(index),
                    myTurnFirst: gameLogic.FirstTurn == index,
                    numTurnsTakenByOpponent: (byte)gameLogic.NumTurnsTakenByIncludingCurrent(1 - index),
                    haveCategoryAnswers: ownedCategories
                );
            });

        public Task<SimplifiedGameInfo> GetSimplifiedGameInfo(Guid playerID) =>
            state.UseState(async state =>
            {
                int index = Index(playerID);
                var turnsTaken = gameLogic.NumTurnsTakenBy(index);

                return new SimplifiedGameInfo
                (
                    gameID: this.GetPrimaryKey(),
                    gameState: GetStateInternal(state),
                    otherPlayerName: state.PlayerIDs[1 - index] == Guid.Empty ? null : (await PlayerInfoHelper.GetInfo(GrainFactory, state.PlayerIDs[1 - index])).Name,
                    myTurn: gameLogic.Turn == index,
                    myScore: gameLogic.GetNumRoundsWon(index),
                    theirScore: gameLogic.GetNumRoundsWon(1 - index)
                );
            });

        public Task<bool> WasFirstTurnPlayed() //?? remove - see comment on interface
        {
            return Task.FromResult(gameLogic.PlayerFinishedTurn(0, 0));
        }
    }
}
