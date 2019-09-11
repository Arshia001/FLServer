using Bond;
using Bond.Tag;
using FLGameLogic;
using FLGrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    //?? the current scheme doesn't support immediate lookup of score details for a round
    [Schema, BondSerializationTag("#g")]
    public class GameGrain_State : IOnDeserializedHandler
    {
        [Id(0), Type(typeof(nullable<wstring>[]))]
        public string[] CategoryNames { get; set; }

        [Id(1)]
        public Guid[] PlayerIDs { get; set; }

        [Id(2)]
        public int[] LastProcessedEndTurns { get; set; } //?? use to reprocess turn end notifications in case grain goes down

        [Id(3)]
        public int GroupChooser { get; set; } = -1;

        [Id(4), Type(typeof(nullable<List<ushort>>))]
        public List<ushort> GroupChoices { get; set; }

        public void OnDeserialized()
        {
            if (PlayerIDs == null)
                PlayerIDs = Array.Empty<Guid>();
            if (LastProcessedEndTurns == null)
                LastProcessedEndTurns = new[] { -1, -1 };
        }
    }

    class Game : SaveStateOnDeactivateGrain<GameGrain_State>, IGame
    {
        class EndRoundTimerData
        {
            public int playerIndex;
            public int roundIndex;
            public IDisposable timerHandle;
        }


        readonly IDisposable[] turnTimers = new IDisposable[2]; //?? restore timers when activating
        GameLogicServer gameLogic;
        IConfigReader configReader;
        Random random = new Random();


        int NumJoinedPlayers => State.PlayerIDs.Length == 0 ? 0 : State.PlayerIDs[1] == Guid.Empty ? 1 : 2;


        public Game(IConfigReader configReader)
        {
            this.configReader = configReader;
        }

        public override Task OnActivateAsync()
        {
            //?? restore game state from grain state
            //?? register turn end timers if any
            //?? record active turn end timers in state so we can register even if turn time already passed
            if (State.CategoryNames != null && State.CategoryNames.Length > 0)
            {
                var config = configReader.Config;
                //?? restore game state from grain state
                gameLogic = new GameLogicServer(State.CategoryNames.ToList().Select((n, i) =>
                {
                    if (n == null)
                        return null;

                    if (config.CategoriesAsGameLogicFormatByName.TryGetValue(n, out var category))
                        return category;

                    // In case a category was deleted...
                    category = config.CategoriesAsGameLogicFormat[random.Next(config.CategoriesAsGameLogicFormat.Count)];
                    State.CategoryNames[i] = category.CategoryName;
                    return category;
                }));
            }

            DelayDeactivation(TimeSpan.FromDays(20)); //?? store state in DB -.-

            return Task.CompletedTask;
        }

        int Index(Guid playerID) => State.PlayerIDs[0] == playerID ? 0 : (State.PlayerIDs[1] == playerID ? 1 : throw new Exception("Unknown player ID " + playerID.ToString()));

        public Task<byte> StartNew(Guid playerOneID)
        {
            if (GetStateInternal() != GameState.New)
                throw new Exception("Game already started");

            var config = configReader.Config;
            State.CategoryNames = new string[config.ConfigValues.NumRoundsPerGame];

            gameLogic = new GameLogicServer(config.ConfigValues.NumRoundsPerGame);

            State.PlayerIDs = new[] { playerOneID, Guid.Empty };

            return Task.FromResult((byte)State.CategoryNames.Length);
        }

        public async Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(PlayerInfo playerTwo)
        {
            if (State.PlayerIDs[0] == playerTwo.ID)
                throw new Exception("Player cannot join game with self");

            State.PlayerIDs[1] = playerTwo.ID;

            await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentJoined(State.PlayerIDs[0], this.GetPrimaryKey(), playerTwo);

            return (State.PlayerIDs[0], (byte)State.CategoryNames.Length);
        }

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

            var endRoundData = new EndRoundTimerData { playerIndex = playerIndex, roundIndex = roundIndex };
            var timerHandle = RegisterTimer(OnTurnEnded, endRoundData, roundTime, TimeSpan.MaxValue);
            endRoundData.timerHandle = timerHandle;

            return (false, category, roundIndex, (TimeSpan?)(configValues.ClientTimePerRound));
        }

        //?? this could potentially be bad, timing-wise. Maybe the client should generate their own clock?
        public async Task<(string category, bool? haveAnswers, TimeSpan? roundTime, bool mustChooseGroup, IEnumerable<GroupInfoDTO> groups)> StartRound(Guid id)
        {
            var index = Index(id);

            var (mustChooseCategory, category, _, roundTime) = StartRound(index);

            if (mustChooseCategory)
            {
                var config = configReader.Config;
                if (State.GroupChoices == null || State.GroupChooser != index)
                {
                    State.GroupChooser = index;
                    State.GroupChoices =
                        new Random().GetUnique(0, config.Groups.Count, config.ConfigValues.NumGroupChoices)
                        .Select(i => config.Groups[i].ID).ToList();
                }
                return (default(string), default(bool?), default(TimeSpan?), true, State.GroupChoices.Select(i => (GroupInfoDTO)config.GroupsByID[i]).ToList().AsEnumerable());
            }
            else
                return (category, (bool?)await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime, false, Enumerable.Empty<GroupInfoDTO>());
        }

        public async Task<(string category, bool haveAnswers, TimeSpan roundTime)> ChooseGroup(Guid id, ushort groupID)
        {
            var index = Index(id);

            var (mustChooseCategory, category, roundIndex, roundTime) = StartRound(index);
            if (!mustChooseCategory)
                return (category, await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime.Value);

            if (State.GroupChooser != index || State.GroupChoices == null)
                throw new VerbatimException("Not this player's turn to choose a group");

            if (!State.GroupChoices.Contains(groupID))
                throw new VerbatimException($"Specified group {groupID} is not a valid choice out of ({string.Join(", ", State.GroupChoices)})");

            var config = configReader.Config;

            var categories = config.CategoryNamesByGroupID[groupID];
            var random = new Random();
            string categoryName;
            do
                categoryName = categories[random.Next(categories.Count)];
            while (State.CategoryNames.Contains(categoryName));

            State.CategoryNames[roundIndex] = categoryName;
            var result = gameLogic.SetCategory(roundIndex, config.CategoriesAsGameLogicFormatByName[categoryName]);

            if (!result.IsSuccess())
                throw new VerbatimException($"Failed to set category, result is {result}");

            (mustChooseCategory, category, _, roundTime) = StartRound(index);
            if (mustChooseCategory)
                throw new VerbatimException("Still need to choose category after setting it once");

            State.GroupChooser = -1;
            State.GroupChoices = null;

            return (category, await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime.Value);
        }

        Task OnTurnEnded(object state)
        {
            var data = (EndRoundTimerData)state;
            data.timerHandle.Dispose();

            return HandleEndTurn(data.playerIndex, data.roundIndex);
        }

        private async Task HandleEndTurn(int playerIndex, int roundIndex)
        {
            if (State.LastProcessedEndTurns[playerIndex] >= roundIndex)
                return;

            State.LastProcessedEndTurns[playerIndex] = roundIndex;

            var opponentFinishedThisRound = gameLogic.PlayerFinishedTurn(1 - playerIndex, roundIndex);

            var myID = this.GetPrimaryKey();
            await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentTurnEnded(State.PlayerIDs[1 - playerIndex], myID, (byte)roundIndex,
                opponentFinishedThisRound ? gameLogic.GetPlayerAnswers(playerIndex, roundIndex).Select(w => (WordScorePairDTO)w).ToList() : null);

            if (opponentFinishedThisRound)
            {
                var score0 = gameLogic.GetPlayerScores(0)[roundIndex];
                var score1 = gameLogic.GetPlayerScores(1)[roundIndex];
                if (score0 > score1)
                    await GrainFactory.GetGrain<IPlayer>(State.PlayerIDs[0]).OnRoundWon(this.AsReference<IGame>());
                else if (score1 > score0)
                    await GrainFactory.GetGrain<IPlayer>(State.PlayerIDs[1]).OnRoundWon(this.AsReference<IGame>());
            }

            if (gameLogic.Finished)
            {
                var wins0 = gameLogic.GetNumRoundsWon(0);
                var wins1 = gameLogic.GetNumRoundsWon(1);

                var winner = wins0 > wins1 ? State.PlayerIDs[0] : wins1 > wins0 ? State.PlayerIDs[1] : default(Guid?);

                var me = this.AsReference<IGame>();
                var (score0, rank0) = await GrainFactory.GetGrain<IPlayer>(State.PlayerIDs[0]).OnGameResult(me, winner);
                var (score1, rank1) = await GrainFactory.GetGrain<IPlayer>(State.PlayerIDs[1]).OnGameResult(me, winner);

                await Task.WhenAll(GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(State.PlayerIDs[0], myID, wins0, wins1, score0, rank0),
                    GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(State.PlayerIDs[1], myID, wins1, wins0, score1, rank1));

                // await ClearStateAsync(); // keep game history (separately)
                DeactivateOnIdle();
            }
        }

        public async Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word)
        {
            var index = Index(id);
            var maxEditDistances = configReader.Config.MaxEditDistanceToCorrentByLetterCount;

            var result = await gameLogic.PlayWord(index, word, (c, w) => GetWordScore(gameLogic.RoundNumber, index, c, w), i => maxEditDistances[i]);

            if (result.result != PlayWordResult.Duplicate)
                GrainFactory.GetGrain<ICategoryStatisticsAggregationWorker>(result.category.CategoryName)
                    .AddDelta(new CategoryStatisticsDelta.WordUsage { Word = result.corrected }).Ignore();

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

        public Task<List<GroupConfig>> RefreshGroups(Guid guid)
        {
            var index = Index(guid);

            if (State.GroupChooser != index || State.GroupChoices == null)
                return null;

            var config = configReader.Config;
            State.GroupChoices =
                new Random().GetUniqueExcept(0, config.Groups.Count, config.ConfigValues.NumGroupChoices,
                    i => State.GroupChoices.Contains(config.Groups[i].ID), State.GroupChoices.Count)
                .Select(i => config.Groups[i].ID).ToList();

            return Task.FromResult(State.GroupChoices.Select(i => config.GroupsByID[i]).ToList());
        }

        GameState GetStateInternal()
        {
            if (State.PlayerIDs.Length == 0 || State.PlayerIDs.Length == 0)
                return GameState.New;

            if (State.PlayerIDs[1] == Guid.Empty)
                return GameState.WaitingForSecondPlayer;

            if (gameLogic.Finished)
                return GameState.Finished;

            return GameState.InProgress;
        }

        public Task<GameState> GetState() => Task.FromResult(GetStateInternal());

        public async Task<GameInfo> GetGameInfo(Guid playerID)
        {
            if (GetStateInternal() == GameState.New)
                throw new Exception("Game not in progress");

            int index = Index(playerID);
            var turnsTakenInclCurrent = gameLogic.NumTurnsTakenByIncludingCurrent(index);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            var categories = State.CategoryNames.Take(turnsTakenInclCurrent).ToList();
            var playerInfo = State.PlayerIDs[1 - index] == Guid.Empty ? null :
                await PlayerInfoUtil.GetForPlayerID(GrainFactory, State.PlayerIDs[1 - index]);

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
        }

        public async Task<SimplifiedGameInfo> GetSimplifiedGameInfo(Guid playerID)
        {
            int index = Index(playerID);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            return new SimplifiedGameInfo
            (
                gameID: this.GetPrimaryKey(),
                gameState: GetStateInternal(),
                otherPlayerName: State.PlayerIDs[1 - index] == Guid.Empty ? null : (await PlayerInfoUtil.GetForPlayerID(GrainFactory, State.PlayerIDs[1 - index])).Name,
                myTurn: gameLogic.Turn == index,
                myScore: gameLogic.GetNumRoundsWon(index),
                theirScore: gameLogic.GetNumRoundsWon(1 - index)
            );
        }

        public Task<bool> WasFirstTurnPlayed() //?? remove - see comment on interface 
        {
            return Task.FromResult(gameLogic.PlayerFinishedTurn(0, 0));
        }
    }
}
