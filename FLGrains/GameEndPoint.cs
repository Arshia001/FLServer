using FLGameLogic;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using LightMessage.Common.Messages;
using LightMessage.OrleansUtils.GrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class GameEndPoint : GameEndPointBase
    {
        protected override async Task<(Guid gameID, PlayerInfo? opponentInfo, byte numRounds, bool myTurnFirst)> NewGame(Guid clientID)
        {
            var player = GrainFactory.GetGrain<IPlayer>(clientID);

            if (!await player.CanEnterGame())
                throw new VerbatimException("Cannot enter game at this time");

            return await GrainFactory.GetGrain<IMatchMakingGrain>(0).FindOrCreateGame(player);
        }

        protected override async Task<IEnumerable<WordScorePairDTO>?> EndRound(Guid clientID, Guid gameID)
        {
            var result = await GrainFactory.GetGrain<IGame>(gameID).EndRound(clientID);
            return result.Value?.Select(w => (WordScorePairDTO)w);
        }

        protected override async Task<IEnumerable<SimplifiedGameInfo>> GetAllGames(Guid clientID)
        {
            var games = (await GrainFactory.GetGrain<IPlayer>(clientID).GetGames()).Value;
            return await Task.WhenAll(games.Reverse().Select(g => g.GetSimplifiedGameInfo(clientID)));
        }

        protected override Task Vote(Guid clientID, string category, bool up) =>
            GrainFactory.GetGrain<ICategoryStatisticsAggregationWorker>(category)
                .AddDelta(up ? new CategoryStatisticsDelta.UpVote() : (CategoryStatisticsDelta)new CategoryStatisticsDelta.DownVote());
    }
}
