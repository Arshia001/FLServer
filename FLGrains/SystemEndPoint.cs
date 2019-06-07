using FLGameLogic;
using FLGrainInterfaces;
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
    class SystemEndPoint : SystemEndPointBase
    {
        readonly IConfigReader configReader;

        public SystemEndPoint(IConfigReader configReader) => this.configReader = configReader;

        protected override async Task<(OwnPlayerInfo playerInfo, byte numRoundsToWinToGetReward)> GetStartupInfo(Guid clientID)
        {
            var playerInfo = await GrainFactory.GetGrain<IPlayer>(clientID).PerformStartupTasksAndGetInfo().UnwrapImmutable();
            return (playerInfo, configReader.Config.ConfigValues.NumRoundsToWinToGetReward);
        }

        protected override Task<string> TakeRewardForWinningRounds(Guid clientID) =>
            GrainFactory.GetGrain<IPlayer>(clientID).TakeRewardForWinningRounds();
    }
}
