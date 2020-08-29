using Bond;
using FLGameLogic;
using FLGameLogicServer;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using FLGrains.Utility;
using LightMessage.OrleansUtils.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FLGrains
{
    [Schema, BondSerializationTag("#g")]
    public class GameGrainState
    {
        [Id(0)]
        public SerializedGameData? GameData { get; set; }

        [Id(1)]
        public Guid[] PlayerIDs { get; set; } = Array.Empty<Guid>();

        [Id(2)]
        public int[] LastProcessedEndTurns { get; set; } = new[] { -1, -1 };

        [Id(3)]
        public int GroupChooser { get; set; } = -1;

        [Id(4)]
        public List<ushort>? GroupChoices { get; set; }

        [Id(5)]
        public int[] TimeExtensionsForThisRound { get; set; } = new[] { 0, 0 };

        [Id(6)]
        public int NumGroupRefreshesRemainingForThisRound { get; set; }

        [Id(7)]
        public int[] TimeExtensionsForEntireGame { get; set; } = new[] { 0, 0 };

        [Id(8)]
        public int[] WordsRevealedForThisRound { get; set; } = new[] { 0, 0 };

        [Id(9)]
        public CompetitionResult? DesiredBotMatchOutcome { get; set; }
    }

    static class GameReminderNames
    {
        public const string EndTurn = "e";
        public const string BotPlay = "bp";
    }

    //?? Now, we only need a way to reactivate these if one of them goes down... Same old challenge.
    //!! cache player names along with games?
    class Game : Grain, IGame, IRemindable
    {
        class EndRoundTimerData
        {
            public int playerIndex;
            public int roundIndex;
            public IDisposable? timerHandle;

            public EndRoundTimerData(int playerIndex, int roundIndex)
            {
                this.playerIndex = playerIndex;
                this.roundIndex = roundIndex;
            }
        }

        GameLogicServer? gameLogic;
        readonly IConfigReader configReader;
        readonly GrainStateWrapper<GameGrainState> state;
        readonly ILogger logger;
        readonly IBotDatabase botDatabase;

        // Did we already try to add this game to the match making queue during this activation's life?
        bool triedReAddingToMatchMaking = false;

        public Game(IConfigReader configReader, [PersistentState("State")] IPersistentState<GameGrainState> state, ILogger<Game> logger, IBotDatabase botDatabase)
        {
            this.configReader = configReader;
            this.logger = logger;
            this.botDatabase = botDatabase;

            this.state = new GrainStateWrapper<GameGrainState>(state);
            this.state.Persist += State_Persist;
        }

        private void State_Persist(object sender, PersistStateEventArgs<GameGrainState> e) =>
            e.State.GameData = gameLogic?.Serialize();

        int NumJoinedPlayers => state.UseState(state => state.PlayerIDs.Length == 0 ? 0 : state.PlayerIDs[1] == Guid.Empty ? 1 : 2);

        bool IsBotMatch => state.UseState(state => state.PlayerIDs.Length >= 2 && botDatabase.IsBotID(state.PlayerIDs[1]));

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

                        var category = config.GetCategory(c);
                        if (category != null)
                            return category;

                        // In case a category was deleted...
                        return config.CategoriesAsGameLogicFormat[RandomHelper.GetInt32(config.CategoriesAsGameLogicFormat.Count)];
                    });
                }
            });

            var numPlayers = NumJoinedPlayers;
            if (numPlayers >= 1)
                await SetTimerOrProcessEndTurnIfNecessary(0);
            if (numPlayers >= 2)
                await SetTimerOrProcessEndTurnIfNecessary(1);
        }

        public override Task OnDeactivateAsync() => state.PerformLazyPersistIfPending();

        GameLogicServer GameLogic => gameLogic ?? throw new Exception("Internal error: game logic not initialized");

        async Task SetTimerOrProcessEndTurnIfNecessary(int playerIndex)
        {
            var now = DateTime.Now;
            var roundIndex = GameLogic.NumTurnsTakenByIncludingCurrent(playerIndex) - 1;
            if (GameLogic.IsTurnInProgress(playerIndex, now))
                SetEndTurnTimerImpl(playerIndex, GameLogic.GetTurnEndTime(playerIndex) - now, roundIndex);
            else if (!EndTurnProcessed(playerIndex, roundIndex))
                await HandleEndTurn(playerIndex, roundIndex); // In case the game grain went down while the game was in progress
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

                gameLogic = new GameLogicServer(config.ConfigValues.NumRoundsPerGame, config.ConfigValues.GameInactivityTimeout);

                state.PlayerIDs = new[] { playerOneID, Guid.Empty };

                return (byte)gameLogic.NumRounds;
            });
        }

        public Task<(Guid opponentID, byte numRounds, TimeSpan? expiryTimeRemaining)> AddSecondPlayer(PlayerInfoDTO playerTwo) =>
            state.UseStateAndPersist(async state =>
            {
                var gameState = GetStateInternal(state);
                if (gameState == GameState.New)
                    throw new VerbatimException("Game not ready to accept second player");

                if (gameState != GameState.WaitingForSecondPlayer)
                    return (Guid.Empty, default(byte), default(TimeSpan?));

                if (state.PlayerIDs[0] == playerTwo.ID)
                    throw new VerbatimException("Player cannot join game with self");

                GameLogic.SecondPlayerJoined();

                state.PlayerIDs[1] = playerTwo.ID;

                await RegisterExpiryReminderIfNecessary();

                var timeRemaining = GetExpiryTimeRemaining();

                await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentJoined(state.PlayerIDs[0], (this).GetPrimaryKey(), playerTwo, timeRemaining);

                return (state.PlayerIDs[0], (byte)GameLogic.NumRounds, timeRemaining);
            });

        public Task AddBotAsSecondPlayer() =>
            state.UseStateAndPersist(async state =>
            {
                var config = configReader.Config.ConfigValues;

                var gameState = GetStateInternal(state);
                if (gameState != GameState.WaitingForSecondPlayer)
                    return;

                GameLogic.SecondPlayerJoined();

                var bot = botDatabase.GetRandom();
                state.PlayerIDs[1] = bot.ID;

                await RegisterExpiryReminderIfNecessary();

                var timeRemaining = GetExpiryTimeRemaining();

                var matchHistory = await GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]).GetMatchResultHistory();
                var score = matchHistory.Take((int)config.BotMatchOutcomeNumMatches).Sum(r => r switch { CompetitionResult.Win => 1, CompetitionResult.Loss => -1, _ => 0 });
                if (score > config.BotMatchOutcomeLossThreshold)
                    state.DesiredBotMatchOutcome = CompetitionResult.Loss;
                else if (score < config.BotMatchOutcomeWinThreshold)
                    state.DesiredBotMatchOutcome = CompetitionResult.Win;
                else
                    state.DesiredBotMatchOutcome = null;

                await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentJoined(state.PlayerIDs[0], (this).GetPrimaryKey(), bot, timeRemaining);

                // The bot just joined the game, the result of its first round should be available after a delay
                await RegisterBotPlayReminder(false);
            });

        // True to wait a longer time, as if the opponent has played and now the bot must come back online.
        // False to wait a shorter while, as if the bot is playing its second round and must be done soon.
        Task RegisterBotPlayReminder(bool longWait)
        {
            var config = configReader.Config.ConfigValues;

            var waitTime = longWait ?
                RandomHelper.GetDouble(config.BotPlayMinWaitMinutes, config.BotPlayMaxWaitMinutes) :
                1.0;

            return RegisterOrUpdateReminder(GameReminderNames.BotPlay, TimeSpan.FromMinutes(waitTime), TimeSpan.FromMinutes(1));
        }

        async Task PlayBotTurn()
        {
            try
            {
                var config = configReader.Config;

                var botID = state.UseState(state => state.PlayerIDs[1]);
                var (category, _, _, mustChoose, groups) = await StartRound(botID);
                if (mustChoose)
                    (category, _, _) = await ChooseGroup(botID, groups.First().ID);

                var desiredOutcome = state.UseState(state => state.DesiredBotMatchOutcome);
                var (playerScore, botScore) = (GameLogic.GetNumRoundsWon(0), GameLogic.GetNumRoundsWon(1));

                bool? shouldWinRound;
                if (desiredOutcome == CompetitionResult.Loss && botScore <= playerScore)
                    shouldWinRound = true;
                else if (desiredOutcome == CompetitionResult.Win && botScore >= playerScore)
                    shouldWinRound = false;
                else
                    shouldWinRound = null;

                var words = config.CategoriesAsGameLogicFormatByName[category!].Answers.ToHashSet();

                var opponentScore = GameLogic.GetPlayerAnswers(0, GameLogic.RoundNumber).Sum(w => w.score);

                var score = 0u;
                var numPlayed = 0u;

                Func<bool> shouldStop;

                if (!shouldWinRound.HasValue)
                {
                    var numWords = RandomHelper.GetInt32(3, 12);
                    shouldStop = () => numPlayed >= numWords && words.Count > 0;
                }
                else
                {
                    if (opponentScore == 0)
                    {
                        var numWords = shouldWinRound.Value ? RandomHelper.GetInt32(8, 12) : RandomHelper.GetInt32(3, 5);
                        shouldStop = () => numPlayed >= numWords && words.Count > 0;
                    }
                    else
                    {
                        if (shouldWinRound.Value)
                            shouldStop = () => words.Count == 0 || score > opponentScore;
                        else
                        {
                            var scoreLimit = opponentScore <= 3 ? 0 : RandomHelper.GetInt32(0, opponentScore - 3);
                            shouldStop = () => words.Count == 0 || score > scoreLimit;
                        }
                    }
                }

                while (!shouldStop())
                {
                    var word = words.ElementAt(RandomHelper.GetInt32(words.Count));
                    var (wordScore, _) = await PlayWord(botID, word);
                    score += wordScore;
                    ++numPlayed;
                    words.Remove(word);
                }

                await EndRound(botID);

                if (GameLogic.Turn == 1)
                    await RegisterBotPlayReminder(false);
                else
                    await UnregisterReminder(GameReminderNames.BotPlay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to play bot turn, will stop reminder");
                try
                {
                    await UnregisterReminder(GameReminderNames.BotPlay);
                }
                catch { }
            }
        }

        TimeSpan? GetExpiryTimeRemaining() => GameLogic.ExpiryTime == null ? default(TimeSpan?) : GameLogic.ExpiryTime.Value - DateTime.Now;

        (bool shouldChooseCategory, string? category, int roundIndex, TimeSpan? roundTime) StartRound(int playerIndex)
        {
            var configValues = configReader.Config.ConfigValues;
            var roundTime = configValues.ClientTimePerRound + configValues.ExtraTimePerRound;

            int roundIndex = GameLogic.NumTurnsTakenBy(playerIndex);
            var result = GameLogic.StartRound(playerIndex, roundTime, out var category);

            if (result == StartRoundResult.MustChooseCategory)
                return (true, default(string), roundIndex, default(TimeSpan?));

            if (!result.IsSuccess())
                throw new Exception("Failed to start round, resulting in " + result.ToString());

            SetEndTurnTimerImpl(playerIndex, roundTime, roundIndex);

            return (false, category, roundIndex, (TimeSpan?)(configValues.ClientTimePerRound));
        }

        private void SetEndTurnTimerImpl(int playerIndex, TimeSpan roundTime, int roundIndex)
        {
            var endRoundData = new EndRoundTimerData(playerIndex, roundIndex);
            var timerHandle = RegisterTimer(OnTurnEnded, endRoundData, roundTime, TimeSpan.MaxValue);
            endRoundData.timerHandle = timerHandle;
        }

        public async Task<(string? category, bool? haveAnswers, TimeSpan? roundTime, bool mustChooseGroup, IEnumerable<GroupInfoDTO> groups)> StartRound(Guid id)
        {
            var index = Index(id);

            var (mustChooseCategory, category, _, roundTime) = StartRound(index);

            if (mustChooseCategory || category == null)
            {
                return await state.UseStateAndPersist(state =>
                {
                    var config = configReader.Config;
                    if (state.GroupChoices == null || state.GroupChooser != index)
                    {
                        state.GroupChooser = index;
                        state.GroupChoices =
                            RandomHelper.GetUnique(0, config.Groups.Count, config.ConfigValues.NumGroupChoices)
                            .Select(i => config.Groups[i].ID).ToList();
                        state.NumGroupRefreshesRemainingForThisRound = (int)config.ConfigValues.RefreshGroupsAllowedPerRound;
                    }
                    return (default(string), default(bool?), default(TimeSpan?), true, state.GroupChoices.Select(i => (GroupInfoDTO)config.GroupsByID[i]).ToList().AsEnumerable());
                });
            }
            else
            {
                await state.UseStateAndPersist(state =>
                {
                    state.TimeExtensionsForThisRound[index] = 0;
                    state.WordsRevealedForThisRound[index] = 0;
                });

                return (category, await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime, false, Enumerable.Empty<GroupInfoDTO>());
            }
        }

        public async Task<(string category, bool haveAnswers, TimeSpan roundTime)> ChooseGroup(Guid id, ushort groupID)
        {
            var index = Index(id);

            var (mustChooseCategory, category, roundIndex, roundTime) = StartRound(index);
            if (!mustChooseCategory)
                return (category ?? throw new Exception("Don't have a category"), await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime!.Value);

            state.UseState(state =>
            {
                if (state.GroupChooser != index || state.GroupChoices == null)
                    throw new VerbatimException("Not this player's turn to choose a group");

                if (!state.GroupChoices.Contains(groupID))
                    throw new VerbatimException($"Specified group {groupID} is not a valid choice out of ({string.Join(", ", state.GroupChoices)})");
            });

            GrainFactory.GetGrain<IPlayer>(id).AddStats(new List<StatisticValueDTO> { new StatisticValueDTO(Statistics.GroupChosen_Param, groupID, 1) }).Ignore();

            var config = configReader.Config;

            var categories = config.CategoryNamesByGroupID[groupID];
            var currentCategories = GameLogic.CategoryNames;
            string categoryName;
            do
                categoryName = categories[RandomHelper.GetInt32(categories.Count)];
            while (currentCategories.Contains(categoryName));

            var result = GameLogic.SetCategory(roundIndex, config.CategoriesAsGameLogicFormatByName[categoryName]);

            if (!result.IsSuccess())
                throw new VerbatimException($"Failed to set category, result is {result}");

            (mustChooseCategory, category, _, roundTime) = StartRound(index);
            if (mustChooseCategory)
                throw new VerbatimException("Still need to choose category after setting it once");

            await state.UseStateAndPersist(state =>
            {
                state.TimeExtensionsForThisRound[index] = 0;
                state.WordsRevealedForThisRound[index] = 0;
                state.GroupChooser = -1;
                state.GroupChoices = null;
            });

            return (category ?? throw new Exception("Don't have a category"), await GrainFactory.GetGrain<IPlayer>(id).HaveAnswersForCategory(category), roundTime!.Value);
        }

        Task OnTurnEnded(object state)
        {
            var data = (EndRoundTimerData)state;
            data.timerHandle?.Dispose();

            return HandleEndTurn(data.playerIndex, data.roundIndex);
        }

        bool EndTurnProcessed(int playerIndex, int roundIndex) => state.UseState(state => state.LastProcessedEndTurns[playerIndex] >= roundIndex);

        private Task HandleEndTurn(int playerIndex, int roundIndex) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (EndTurnProcessed(playerIndex, roundIndex))
                    return false;

                state.LastProcessedEndTurns[playerIndex] = roundIndex;

                var config = configReader.Config;

                if (playerIndex == 0 && roundIndex == 0)
                    await GrainFactory.GetGrain<IMatchMakingGrain>(0).AddGame(this.AsReference<IGame>(), GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]));

                var opponentFinishedThisRound = GameLogic.PlayerFinishedTurn(1 - playerIndex, roundIndex);

                var myID = this.GetPrimaryKey();

                var sentEndTurn = await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentTurnEnded(state.PlayerIDs[1 - playerIndex], myID, (byte)roundIndex,
                    opponentFinishedThisRound ? GameLogic.GetPlayerAnswers(playerIndex, roundIndex).Select(w => (WordScorePairDTO)w).ToList() : null, GetExpiryTimeRemaining());

                if (!sentEndTurn && !opponentFinishedThisRound)
                    await GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1 - playerIndex]).SendMyTurnStartedNotification(state.PlayerIDs[playerIndex]);

                GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[playerIndex]).OnRoundCompleted(this.AsReference<IGame>(), GameLogic.GetPlayerScores(playerIndex)[roundIndex]).Ignore();

                if (opponentFinishedThisRound)
                {
                    var score0 = GameLogic.GetPlayerScores(0)[roundIndex];
                    var score1 = GameLogic.GetPlayerScores(1)[roundIndex];

                    var category = GameLogic.Categories[roundIndex].CategoryName;
                    var groupID = config.CategoriesByName[category].Group.ID;

                    GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]).OnRoundResult(this.AsReference<IGame>(), CompetitionResultHelper.Get(score0, score1), groupID).Ignore();
                    GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1]).OnRoundResult(this.AsReference<IGame>(), CompetitionResultHelper.Get(score1, score0), groupID).Ignore();
                }

                if (GameLogic.Finished)
                {
                    var wins0 = GameLogic.GetNumRoundsWon(0);
                    var wins1 = GameLogic.GetNumRoundsWon(1);

                    var me = this.AsReference<IGame>();
                    IPlayer player0 = GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]);
                    IPlayer player1 = GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1]);

                    var initScore0 = await player0.GetScore();
                    var initScore1 = await player1.GetScore();

                    var scoreGain = ScoreGainCalculator.CalculateGain(initScore0, initScore1, CompetitionResultHelper.Get(wins0, wins1), config);

                    var (score0, rank0, level0, xp0, gold0) = await player0.OnGameResult(me, CompetitionResultHelper.Get(wins0, wins1), wins0, scoreGain, false, state.PlayerIDs[1]);
                    var (score1, rank1, level1, xp1, gold1) = await player1.OnGameResult(me, CompetitionResultHelper.Get(wins1, wins0), wins1, scoreGain, false, state.PlayerIDs[0]);

                    if (!await GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(state.PlayerIDs[0], myID, wins0, wins1, score0, rank0, level0, xp0, gold0))
                        await player0.SendGameEndedNotification(state.PlayerIDs[1]);
                    if (!await GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(state.PlayerIDs[1], myID, wins1, wins0, score1, rank1, level1, xp1, gold1))
                        await player1.SendGameEndedNotification(state.PlayerIDs[0]);

                    var expiryReminder = await GetReminder("e");
                    if (expiryReminder != null)
                        await UnregisterReminder(expiryReminder);

                    //!! keep game history separately, deactivate this grain
                }
                else
                {
                    await RegisterExpiryReminderIfNecessary();

                    if (IsBotMatch)
                        await RegisterBotPlayReminder(true);
                }

                return true;
            });

        async Task RegisterExpiryReminderIfNecessary()
        {
            if (GameLogic.ExpiryTime.HasValue)
            {
                // This one minute limit is imposed by Orleans on reminder intervals. In our current
                // scenario, a one minute difference is probably not noticeable for a 24-hour timeout.
                var timeUntilExpiry = GameLogic.ExpiryTime.Value - DateTime.Now + TimeSpan.FromSeconds(10);
                if (timeUntilExpiry > TimeSpan.FromMinutes(1))
                    await RegisterOrUpdateReminder("e", timeUntilExpiry, TimeSpan.FromMinutes(1));
                else
                {
                    logger.Info("Less than one minute remaining to expiry time, will terminate game immediately");
                    await UnregisterReminder(GameReminderNames.EndTurn);
                    await HandleGameExpiry();
                }
            }
        }

        public Task ReceiveReminder(string reminderName, TickStatus status) =>
            reminderName switch
            {
                GameReminderNames.EndTurn => HandleGameExpiry(),
                GameReminderNames.BotPlay => PlayBotTurn(),
                _ => Task.CompletedTask,
            };

        async Task HandleGameExpiry()
        {
            await UnregisterReminder(GameReminderNames.EndTurn);

            if (!GameLogic.Expired)
            {
                var remaining = GameLogic.ExpiryTime.HasValue ? (GameLogic.ExpiryTime.Value - DateTime.Now).TotalSeconds.ToString() : "NO EXPIRY TIME";
                logger.LogWarning($"Expiry reminder went off but game is not expired with {remaining} seconds left, going to terminate the game anyway");
            }

            await state.UseState(async state =>
            {
                var myID = this.GetPrimaryKey();

                var config = configReader.Config;
                var me = this.AsReference<IGame>();

                IPlayer player0 = GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]);
                IPlayer player1 = GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[1]);

                var initScore0 = await player0.GetScore();
                var initScore1 = await player1.GetScore();

                var expiredFor = GameLogic.ExpiredFor;

                var scoreGain = ScoreGainCalculator.CalculateGain(initScore0, initScore1, expiredFor == 0 ? CompetitionResult.Loss : CompetitionResult.Win, config);

                var (score0, rank0, level0, xp0, gold0) = await player0.OnGameResult(me, expiredFor == 0 ? CompetitionResult.Loss : CompetitionResult.Win, GameLogic.GetNumRoundsWon(0), scoreGain, true, state.PlayerIDs[1]);
                var (score1, rank1, level1, xp1, gold1) = await player1.OnGameResult(me, expiredFor == 0 ? CompetitionResult.Win : CompetitionResult.Loss, GameLogic.GetNumRoundsWon(1), scoreGain, true, state.PlayerIDs[0]);

                // No push notifications here...
                await GrainFactory.GetGrain<IGameEndPoint>(0).SendGameExpired(state.PlayerIDs[0], myID, expiredFor == 1, score0, rank0, level0, xp0, gold0);
                await GrainFactory.GetGrain<IGameEndPoint>(0).SendGameExpired(state.PlayerIDs[1], myID, expiredFor == 0, score1, rank1, level1, xp1, gold1);

                //!! keep game history separately, deactivate this grain
            });
        }

        private async Task UnregisterReminder(string name)
        {
            try
            {
                var expiryReminder = await GetReminder(name);
                if (expiryReminder != null)
                    await UnregisterReminder(expiryReminder);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unregister end turn reminder");
            }
        }

        public async Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word)
        {
            var index = Index(id);
            var maxEditDistances = configReader.Config.MaxEditDistanceToCorrentByLetterCount;

            word = word.Trim().Replace('ي', 'ی').Replace('ك', 'ک');

            var result = await GameLogic.PlayWord(index, word, (c, w) => GetWordScore(GameLogic.RoundNumber, index, c, w), i => maxEditDistances[i]);

            if (result.result == PlayWordResult.Error_TurnOver)
                throw new VerbatimException("Player's turn is already over");

            if (result.result != PlayWordResult.Duplicate && result.score > 0)
                GrainFactory.GetGrain<ICategoryStatisticsAggregationWorker>(result.category.CategoryName)
                    .AddDelta(new CategoryStatisticsDelta.WordUsage(result.corrected)).Ignore();

            var stats = new List<StatisticValueDTO>();
            if (result.result == PlayWordResult.Duplicate)
                stats.Add(new StatisticValueDTO(Statistics.WordsPlayedDuplicate, 0, 1));
            else
                stats.Add(new StatisticValueDTO(Statistics.WordsPlayedScore_Param, result.score, 1));

            if (result.corrected != word)
                stats.Add(new StatisticValueDTO(Statistics.WordsCorrected, result.score, 1));

            GrainFactory.GetGrain<IPlayer>(id).AddStats(stats).Ignore();

            return (result.score, result.corrected);
        }

        Task<byte> GetWordScore(int round, int playerIndex, WordCategory category, string word)
        {
            // If other player played this word, we want to make sure we give this one the same score
            if (GameLogic.PlayerFinishedTurn(1 - playerIndex, round))
            {
                var answer = GameLogic.GetPlayerAnswers(1 - playerIndex, round).FirstOrDefault(w => w.word == word);
                if (answer.word != null)
                    return Task.FromResult(answer.score);
            }

            return GrainFactory.GetGrain<ICategoryStatisticsAggregatorCache>(category.CategoryName).GetScore(word);
        }

        public async Task<Immutable<(IEnumerable<WordScorePair>? opponentWords, TimeSpan? expiryTimeRemaining)>> EndRound(Guid playerID)
        {
            int index = Index(playerID);

            GameLogic.ForceEndTurn(index);

            var turnIndex = GameLogic.NumTurnsTakenBy(index) - 1;

            await HandleEndTurn(index, turnIndex);

            if (GameLogic.PlayerFinishedTurn(1 - index, turnIndex))
                return (GameLogic.GetPlayerAnswers(1 - index, turnIndex).AsEnumerable().AsNullable(), GetExpiryTimeRemaining()).AsImmutable();
            else
                return (default(IEnumerable<WordScorePair>), GetExpiryTimeRemaining()).AsImmutable();
        }

        public Task<(ulong? gold, TimeSpan? remainingTime)> IncreaseRoundTime(Guid playerID) =>
            state.UseStateAndMaybePersist(async state =>
            {
                var config = configReader.Config;

                var index = Index(playerID);
                if (state.TimeExtensionsForThisRound[index] >= config.ConfigValues.NumTimeExtensionsPerRound ||
                    !GameLogic.CanExtendTime(index))
                    return (false, (default(ulong?), default(TimeSpan?)));

                var prices = config.ConfigValues.RoundTimeExtensionPrices!;
                var price =
                    state.TimeExtensionsForEntireGame[index] < prices.Count ?
                    prices[state.TimeExtensionsForEntireGame[index]] :
                    prices[prices.Count - 1];

                var gold = await GrainFactory.GetGrain<IPlayer>(playerID).IncreaseRoundTime(this.GetPrimaryKey(), price);
                if (!gold.HasValue)
                    return (false, (default(ulong?), default(TimeSpan?)));

                var extension = config.ConfigValues.RoundTimeExtension;
                var endTime = GameLogic.ExtendRoundTime(index, extension);
                ++state.TimeExtensionsForThisRound[index];
                ++state.TimeExtensionsForEntireGame[index];

                return (true, (gold, endTime == null ? default(TimeSpan?) : extension));
            });

        public Task<(ulong? gold, string? word, byte? wordScore)> RevealWord(Guid playerID) =>
            state.UseStateAndMaybePersist(async state =>
            {
                var config = configReader.Config;

                var index = Index(playerID);
                if (!GameLogic.IsTurnInProgress(index))
                    return (false, (default(ulong?), default(string), default(byte?)));

                var turnIndex = GameLogic.NumTurnsTakenBy(index);
                var category = GameLogic.Categories[turnIndex];
                var answers = GameLogic.GetPlayerAnswers(index, turnIndex);

                if (answers.Count == category.Answers.Count)
                    return (false, (default(ulong?), default(string), default(byte?)));

                var prices = config.ConfigValues.RevealWordPrices!;
                var price =
                    state.WordsRevealedForThisRound[index] < prices.Count ?
                    prices[state.WordsRevealedForThisRound[index]] :
                    prices[prices.Count - 1];

                var gold = await GrainFactory.GetGrain<IPlayer>(playerID).RevealWord(this.GetPrimaryKey(), price);
                if (!gold.HasValue)
                    return (false, (default(ulong?), default(string), default(byte?)));

                string word;
                do
                    word = category.Answers[RandomHelper.GetInt32(category.Answers.Count)];
                while (answers.Any(a => a.word == word));

                var (score, _) = await PlayWord(playerID, word);

                ++state.WordsRevealedForThisRound[index];

                return (true, (gold, word, score));
            });

        public Task<List<GroupConfig>?> RefreshGroups(Guid guid) =>
            state.UseStateAndMaybePersist(state =>
            {
                var index = Index(guid);

                if (state.GroupChooser != index || state.GroupChoices == null || state.NumGroupRefreshesRemainingForThisRound <= 0)
                    return (false, default(List<GroupConfig>));

                --state.NumGroupRefreshesRemainingForThisRound;

                var config = configReader.Config;
                state.GroupChoices =
                    RandomHelper.GetUniqueExcept(0, config.Groups.Count, config.ConfigValues.NumGroupChoices,
                        i => state.GroupChoices.Contains(config.Groups[i].ID), state.GroupChoices.Count)
                    .Select(i => config.Groups[i].ID).ToList();

                return (true, state.GroupChoices.Select(i => config.GroupsByID[i]).ToList());
            });

        GameState GetStateInternal(GameGrainState state)
        {
            if (state.PlayerIDs.Length == 0 || gameLogic == null)
                return GameState.New;

            if (state.PlayerIDs[1] == Guid.Empty)
                return GameState.WaitingForSecondPlayer;

            if (GameLogic.Finished)
                return GameState.Finished;

            if (GameLogic.Expired)
                return GameState.Expired;

            return GameState.InProgress;
        }

        public Task<GameState> GetState() => state.UseState(state => Task.FromResult(GetStateInternal(state)));

        public Task<Guid[]> GetPlayerIDs() => state.UseState(state => Task.FromResult(state.PlayerIDs));

        public Task<GameInfoDTO> GetGameInfo(Guid playerID) =>
            state.UseState(async state =>
            {
                if (GetStateInternal(state) == GameState.New)
                    throw new Exception("Game not in progress");

                int index = Index(playerID);
                var turnsTakenInclCurrent = GameLogic.NumTurnsTakenByIncludingCurrent(index);
                var turnsTaken = GameLogic.NumTurnsTakenBy(index);

                var categories = GameLogic.Categories.Take(turnsTakenInclCurrent).Select(c => c.CategoryName).ToList();
                var otherPlayerInfo =
                    state.PlayerIDs[1 - index] == Guid.Empty ? null :
                    (botDatabase.GetByID(state.PlayerIDs[1 - index]) ?? // If the opponent isn't a bot, we get null back
                        await PlayerInfoHelper.GetInfo(GrainFactory, state.PlayerIDs[1 - index]));

                var ownedCategories = await GrainFactory.GetGrain<IPlayer>(playerID).HaveAnswersForCategories(categories);

                // The client is assumed to always be behind the server by ExtraTimePerRound to compensate for network delays
                var clientNow = DateTime.Now + configReader.Config.ConfigValues.ExtraTimePerRound;

                return new GameInfoDTO
                (
                    otherPlayerInfo: otherPlayerInfo,
                    numRounds: (byte)GameLogic.Categories.Count,
                    categories: categories,
                    myWordsPlayed: GameLogic.GetPlayerAnswers(index).Take(turnsTakenInclCurrent).Select(ws => ws.Select(w => (WordScorePairDTO)w).ToList()).ToList(),
                    theirWordsPlayed: GameLogic.GetPlayerAnswers(1 - index)?.Take(turnsTaken).Select(ws => ws.Select(w => (WordScorePairDTO)w).ToList()).ToList(), // don't return words for the round currently in progress
                    myTurnFirst: GameLogic.FirstTurn == index,
                    numTurnsTakenByOpponent: (byte)GameLogic.NumTurnsTakenByIncludingCurrent(1 - index),
                    haveCategoryAnswers: ownedCategories,
                    expired: GameLogic.Expired,
                    expiredForMe: GameLogic.ExpiredFor == index,
                    expiryTimeRemaining: GetStateInternal(state) == GameState.InProgress && GameLogic.ExpiryTime.HasValue ? GameLogic.ExpiryTime.Value - DateTime.Now : default(TimeSpan?),
                    roundTimeExtensions: (uint)state.TimeExtensionsForEntireGame[index],
                    myTurnTimeRemaining: GameLogic.IsTurnInProgress(index, clientNow) ? GameLogic.GetTurnEndTime(index) - clientNow : default
                );
            });

        public Task<SimplifiedGameInfoDTO> GetSimplifiedGameInfo(Guid playerID) =>
            state.UseState(async state =>
            {
                int index = Index(playerID);
                var turnsTaken = GameLogic.NumTurnsTakenBy(index);

                var isWinner = index == 0 ? GameLogic.Winner == GameResult.Win0 : GameLogic.Winner == GameResult.Win1;
                var gameState = GetStateInternal(state);

                // Try to add this game to the matchmaking queue again, it may have been missed
                if (!triedReAddingToMatchMaking && gameState == GameState.WaitingForSecondPlayer && GameLogic.NumTurnsTakenBy(0) > 0)
                {
                    triedReAddingToMatchMaking = true;
                    await GrainFactory.GetGrain<IMatchMakingGrain>(0).AddGame(this.AsReference<IGame>(), GrainFactory.GetGrain<IPlayer>(state.PlayerIDs[0]));
                }

                var otherPlayerInfo =
                    state.PlayerIDs[1 - index] == Guid.Empty ? null :
                    (botDatabase.GetByID(state.PlayerIDs[1 - index]) ?? // If the opponent isn't a bot, we get null back
                        await PlayerInfoHelper.GetInfo(GrainFactory, state.PlayerIDs[1 - index]));

                // The client is assumed to always be behind the server by ExtraTimePerRound to compensate for network delays
                var clientNow = DateTime.Now + configReader.Config.ConfigValues.ExtraTimePerRound;

                return new SimplifiedGameInfoDTO
                (
                    gameID: this.GetPrimaryKey(),
                    gameState: gameState,
                    otherPlayerName: otherPlayerInfo?.Name,
                    otherPlayerAvatar: otherPlayerInfo?.Avatar,
                    myTurn: GameLogic.Turn == index,
                    myScore: GameLogic.GetNumRoundsWon(index),
                    theirScore: GameLogic.GetNumRoundsWon(1 - index),
                    winnerOfExpiredGame: gameState == GameState.Expired && isWinner,
                    expiryTimeRemaining: gameState == GameState.InProgress && GameLogic.ExpiryTime.HasValue ? GameLogic.ExpiryTime.Value - DateTime.Now : default(TimeSpan?),
                    myTurnTimeRemaining: GameLogic.IsTurnInProgress(index, clientNow) ? GameLogic.GetTurnEndTime(index) - clientNow : default
                );
            });
    }
}
