using FLGrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FLGrains
{
    class GameEndPoint : GameEndPointBase
    {
        protected override async Task<(Guid gameID, PlayerInfoDTO? opponentInfo, byte numRounds, bool myTurnFirst, TimeSpan? expiryTimeRemaining)> NewGame(Guid clientID)
        {
            var player = GrainFactory.GetGrain<IPlayer>(clientID);
            var (canEnter, activeOpponents) = await player.CheckCanEnterGameAndGetActiveOpponents();

            if (!canEnter)
                throw new VerbatimException("Cannot enter game at this time");

            return await GrainFactory.GetGrain<IMatchMakingGrain>(0).FindOrCreateGame(player, activeOpponents);
        }

        protected override async Task<(IEnumerable<WordScorePairDTO>? opponentWords, TimeSpan? expiryTimeRemaining)> EndRound(Guid clientID, Guid gameID)
        {
            var result = await GrainFactory.GetGrain<IGame>(gameID).EndRound(clientID);
            return (result.Value.opponentWords?.Select(w => (WordScorePairDTO)w), result.Value.expiryTimeRemaining);
        }

        protected override async Task<IEnumerable<SimplifiedGameInfoDTO>> GetAllGames(Guid clientID)
        {
            var games = (await GrainFactory.GetGrain<IPlayer>(clientID).GetGames()).Value;
            return await Task.WhenAll(games.Reverse().Select(g => g.GetSimplifiedGameInfo(clientID)));
        }

        protected override Task Vote(Guid clientID, string category, bool up) =>
            GrainFactory.GetGrain<ICategoryStatisticsAggregationWorker>(category)
                .AddDelta(up ? new CategoryStatisticsDelta.UpVote() : (CategoryStatisticsDelta)new CategoryStatisticsDelta.DownVote());
    }
}
