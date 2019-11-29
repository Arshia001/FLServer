using Bond;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
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
    class MatchMakingEntry
    {
        [Id(0)]
        public IGame? Game { get; }
        [Id(1)]
        public uint Score { get; }
        [Id(2)]
        public uint Level { get; }
        [Id(3)]
        public Guid FirstPlayerID { get; }

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
        readonly IPersistentState<MatchMakingGrainState> state;
        readonly IConfigReader configReader;

        HashSet<MatchMakingEntry> entries = new HashSet<MatchMakingEntry>();

        public MatchMakingGrain([PersistentState("State")] IPersistentState<MatchMakingGrainState> state, IConfigReader configReader)
        {
            this.state = state;
            this.configReader = configReader;
        }

        public override Task OnActivateAsync()
        {
            var savedEntries = state.State.Entries;
            if (savedEntries != null && savedEntries.Any())
                entries = new HashSet<MatchMakingEntry>(savedEntries);
            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync()
        {
            state.State.Entries = new List<MatchMakingEntry>(entries);
            return state.WriteStateAsync();
        }

        public async Task AddGame(IGame game, IPlayer firstPlayer)
        {
            var (score, level) = await firstPlayer.GetMatchMakingInfo();
            var entry = new MatchMakingEntry(game, score, level, firstPlayer.GetPrimaryKey());
            entries.Add(entry);
        }

        bool Within(uint a, uint b, uint delta) => Math.Abs(a - b) <= delta;

        public async Task<(Guid gameID, PlayerInfo? opponentInfo, byte numRounds, bool myTurnFirst)> FindOrCreateGame(IPlayer player)
        {
            var config = configReader.Config;
            var (score, level) = await player.GetMatchMakingInfo();
            var playerID = player.GetPrimaryKey();
            var match = entries.FirstOrDefault(e =>
                Within(e.Level, level, config.ConfigValues.MatchmakingLevelDifference) &&
                Within(e.Score, score, config.ConfigValues.MatchmakingScoreDifference) &&
                e.FirstPlayerID != playerID
            );

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
                IGame gameToEnter;
                do
                    gameToEnter = GrainFactory.GetGrain<IGame>(Guid.NewGuid());
                while (await gameToEnter.GetState() != GameState.New);

                var numRounds = await player.JoinGameAsFirstPlayer(gameToEnter);

                return (gameToEnter.GetPrimaryKey(), null, numRounds, true);
            }
        }
    }
}
