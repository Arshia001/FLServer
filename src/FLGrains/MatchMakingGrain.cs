using Bond;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrainInterfaces.Utility;
using FLGrains.Utility;
using Microsoft.Extensions.Logging;
using Orleans;
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

        [Obsolete("For deserialization only")] public MatchMakingEntry() { }

        public MatchMakingEntry(IGame game, uint score, uint level, Guid firstPlayerID)
        {
            Game = game;
            Score = score;
            Level = level;
            FirstPlayerID = firstPlayerID;
        }
    }

    [Schema, BondSerializationTag("#mm")]
    class MatchMakingGrainState
    {
        [Id(0)]
        public List<MatchMakingEntry>? Entries { get; set; }
    }

    class MatchMakingGrain : Grain, IMatchMakingGrain
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

            return Task.CompletedTask;
        });

        Task WriteStateTimer(object _unused) => state.PerformLazyPersistIfPending();

        public override Task OnDeactivateAsync() => state.PerformLazyPersistIfPending();

        public Task AddGame(IGame game, IPlayer firstPlayer) =>
            // We don't actually update the state as that incurs a performance penalty.
            // Instead, we simply mark the state wrapper with a pending write. The actual
            // updating of state happens in the State_Persist event handler above.
            state.UseStateAndLazyPersist(async _state =>
            {
                var (score, level) = await firstPlayer.GetMatchMakingInfo();
                var entry = new MatchMakingEntry(game, score, level, firstPlayer.GetPrimaryKey());
                entries.Add(entry);
            });

        bool Within(uint a, uint b, uint delta) => Math.Abs((int)a - (int)b) <= delta;

        public async Task<(Guid gameID, PlayerInfo? opponentInfo, byte numRounds, bool myTurnFirst)> FindOrCreateGame(IPlayer player, Guid? lastOpponentID)
        {
            var config = configReader.Config;
            var (score, level) = await player.GetMatchMakingInfo();
            var playerID = player.GetPrimaryKey();

            var detailedLog = this.detailedLog.Value;

            if (detailedLog)
                logger.LogInformation($"Matching player ID {playerID} with level {level} and score {score}");

            var match = entries.FirstOrDefault(IsMatch(score, level, playerID, config, lastOpponentID, detailedLog));

            if (match != null)
            {
                var game = match.Game ?? throw new Exception("Entry without game reference");
                var (opponentID, numRounds) = await player.JoinGameAsSecondPlayer(game);
                var opponentInfo = await PlayerInfoHelper.GetInfo(GrainFactory, opponentID);
                entries.Remove(match);
                return (game.GetPrimaryKey(), await PlayerInfoHelper.GetInfo(GrainFactory, opponentID), numRounds, false);
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

                return (gameToEnter.GetPrimaryKey(), null, numRounds, true);
            }
        }

        Func<MatchMakingEntry, bool> IsMatch(uint score, uint level, Guid playerID, ReadOnlyConfigData config, Guid? lastOpponentID, bool detailedLog) =>
            e =>
            {
                if (detailedLog)
                    logger.LogInformation($"Testing {e.FirstPlayerID} with level {e.Level} and score {e.Score}");

                if (lastOpponentID.HasValue && e.FirstPlayerID == lastOpponentID.Value)
                {
                    if (detailedLog)
                        logger.LogInformation($"Same opponent as last for this player, won't match: {lastOpponentID}");
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
    }
}
