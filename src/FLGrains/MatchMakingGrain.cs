using Bond;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrainInterfaces.Utility;
using FLGrains.Utility;
using Microsoft.Extensions.Logging;
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
    [Schema]
    class MatchMakingEntry
    {
        [Id(0)]
        public IGame? Game { get; private set; }
        [Id(1)]
        public uint Score { get; private set; }
        [Id(2)]
        public uint Level { get; private set; }
        [Id(3)]
        public Guid FirstPlayerID { get; private set; }
        [Id(4)]
        public DateTime? CreationTime { get; private set; }

        [Obsolete("For deserialization only")] public MatchMakingEntry() { }

        public MatchMakingEntry(IGame game, uint score, uint level, Guid firstPlayerID)
        {
            Game = game;
            Score = score;
            Level = level;
            FirstPlayerID = firstPlayerID;
            CreationTime = DateTime.Now;
        }
    }

    [Schema, BondSerializationTag("#mm")]
    class MatchMakingGrainState
    {
        [Id(0)]
        public List<MatchMakingEntry>? Entries { get; set; }
    }

    static class MatchMakingReminderNames
    {
        public const string AddBotsToStaleMatches = "bot";
    }

    class MatchMakingGrain : Grain, IMatchMakingGrain, IRemindable
    {
        readonly GrainStateWrapper<MatchMakingGrainState> state;
        readonly IConfigReader configReader;
        readonly ILogger<MatchMakingGrain> logger;
        readonly CachedValue<bool> detailedLog;

        HashSet<MatchMakingEntry> entries = new HashSet<MatchMakingEntry>();

        public MatchMakingGrain([PersistentState("State")] IPersistentState<MatchMakingGrainState> state,
            IConfigReader configReader, ILogger<MatchMakingGrain> logger)
        {
            this.state = new GrainStateWrapper<MatchMakingGrainState>(state);
            this.state.Persist += State_Persist;

            this.configReader = configReader;
            this.logger = logger;
            detailedLog = new CachedValue<bool>(() => Environment.GetEnvironmentVariable("FLSERVER_DETAILED_MATCHMAKING_LOG") == "yes", TimeSpan.FromMinutes(1));
        }

        private void State_Persist(object sender, PersistStateEventArgs<MatchMakingGrainState> e) =>
            e.State.Entries = new List<MatchMakingEntry>(entries);

        public override Task OnActivateAsync() => state.UseState(state =>
        {
            var savedEntries = state.Entries;
            if (savedEntries != null && savedEntries.Any())
                entries = new HashSet<MatchMakingEntry>(savedEntries);

            RegisterTimer(WriteStateTimer, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            return RegisterOrUpdateReminder(MatchMakingReminderNames.AddBotsToStaleMatches, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        });

        Task WriteStateTimer(object _unused) => state.PerformLazyPersistIfPending();

        public override Task OnDeactivateAsync() => state.PerformLazyPersistIfPending();

        public Task AddGame(IGame game, IPlayer firstPlayer)
        {
            if (entries.Any(e => e.Game == game))
                return Task.CompletedTask;

            // We don't actually update the state as that incurs a performance penalty.
            // Instead, we simply mark the state wrapper with a pending write. The actual
            // updating of state happens in the State_Persist event handler above.
            return state.UseStateAndLazyPersist(async _state =>
            {
                var (score, level) = await firstPlayer.GetMatchMakingInfo();
                var entry = new MatchMakingEntry(game, score, level, firstPlayer.GetPrimaryKey());
                entries.Add(entry);
            });
        }

        bool Within(uint a, uint b, uint delta) => Math.Abs((int)a - (int)b) <= delta;

        public async Task<(Guid gameID, PlayerInfoDTO? opponentInfo, byte numRounds, bool myTurnFirst, TimeSpan? expiryTimeRemaining)>
            FindOrCreateGame(IPlayer player, Immutable<ISet<Guid>> activeOpponents)
        {
            var config = configReader.Config;
            var (score, level) = await player.GetMatchMakingInfo();
            var playerID = player.GetPrimaryKey();

            var detailedLog = this.detailedLog.Value;

            if (detailedLog)
                logger.LogInformation($"Matching player ID {playerID} with level {level} and score {score}");

            while (true)
            {
                var match = entries.FirstOrDefault(IsMatch(score, level, playerID, config, activeOpponents.Value, detailedLog));

                if (match != null)
                {
                    var game = match.Game;
                    if (game == null)
                    {
                        logger.LogError("Found matchmaking entry with null game reference");
                        entries.Remove(match);
                        continue;
                    }

                    var (opponentID, numRounds, expiryTimeRemaining) = await player.JoinGameAsSecondPlayer(game);

                    if (opponentID == Guid.Empty)
                    {
                        // Game already had a second player
                        entries.Remove(match);
                        continue;
                    }

                    var opponentInfo = await PlayerInfoHelper.GetInfo(GrainFactory, opponentID);
                    entries.Remove(match);
                    return (game.GetPrimaryKey(), opponentInfo, numRounds, false, expiryTimeRemaining);
                }
                else
                {
                    if (detailedLog)
                        logger.LogInformation("No match found, will start new game");

                    IGame gameToEnter;
                    do
                        gameToEnter = GrainFactory.GetGrain<IGame>(Guid.NewGuid());
                    while (await gameToEnter.GetState() != GameState.New);

                    var numRounds = await player.JoinGameAsFirstPlayer(gameToEnter);

                    return (gameToEnter.GetPrimaryKey(), null, numRounds, true, default);
                }
            }
        }

        Func<MatchMakingEntry, bool> IsMatch(uint score, uint level, Guid playerID, ReadOnlyConfigData config, ISet<Guid> activeOpponents, bool detailedLog) =>
            e =>
            {
                if (detailedLog)
                    logger.LogInformation($"Testing {e.FirstPlayerID} with level {e.Level} and score {e.Score}");

                if (activeOpponents != null && activeOpponents.Contains(e.FirstPlayerID))
                {
                    if (detailedLog)
                        logger.LogInformation($"Player already has an active game against this opponent, won't match: {e.FirstPlayerID}");
                    return false;
                }

                if (!Within(e.Level, level, config.ConfigValues.MatchmakingLevelDifference))
                {
                    if (detailedLog)
                        logger.LogInformation($"Level difference exceeds expected range {config.ConfigValues.MatchmakingLevelDifference}, won't match");
                    return false;
                }

                if (!Within(e.Score, score, config.ConfigValues.MatchmakingScoreDifference))
                {
                    if (detailedLog)
                        logger.LogInformation($"Score difference exceeds expected range {config.ConfigValues.MatchmakingScoreDifference}, won't match");
                    return false;
                }

                if (e.FirstPlayerID == playerID)
                {
                    if (detailedLog)
                        logger.LogInformation("This is the same player, won't match");
                    return false;
                }

                if (detailedLog)
                    logger.LogInformation("Match found");

                return true;
            };

        async Task AddBotsToStaleMatched()
        {
            var now = DateTime.Now;
            var timeout = configReader.Config.ConfigValues.MatchMakingWaitBeforeBotMatch;
            var toRemove = new HashSet<MatchMakingEntry>();

            foreach (var entry in entries)
                if (now - entry.CreationTime > timeout)
                {
                    var game = entry.Game;
                    if (game == null)
                        continue; // This is a rather impossible case, but if it does happen, the entry will be removed in the normal matchmaking cycle above

                    try
                    {
                        await game.AddBotAsSecondPlayer();
                        toRemove.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to add bot to game {game.GetPrimaryKey()}");
                    }
                }

            foreach (var entry in toRemove)
                entries.Remove(entry);
        }

        public Task ReceiveReminder(string reminderName, TickStatus status) =>
            reminderName switch
            {
                MatchMakingReminderNames.AddBotsToStaleMatches => AddBotsToStaleMatched(),
                _ => Task.CompletedTask
            };
    }
}
