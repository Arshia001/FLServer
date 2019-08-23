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
        readonly ILeaderBoardPlayerInfoCacheService leaderBoardPlayerInfoCache;

        public SystemEndPoint(IConfigReader configReader, ILeaderBoardPlayerInfoCacheService leaderBoardPlayerInfoCache)
        {
            this.leaderBoardPlayerInfoCache = leaderBoardPlayerInfoCache;
            this.configReader = configReader;
        }

        protected override async Task<(OwnPlayerInfo playerInfo, ConfigValuesDTO configData)> GetStartupInfo(Guid clientID)
        {
            var playerInfo = await GrainFactory.GetGrain<IPlayer>(clientID).PerformStartupTasksAndGetInfo().UnwrapImmutable();
            return (playerInfo, configReader.Config.ConfigValues);
        }

        protected override Task<(ulong totalGold, TimeSpan timeUntilNextReward)> TakeRewardForWinningRounds(Guid clientID) =>
            GrainFactory.GetGrain<IPlayer>(clientID).TakeRewardForWinningRounds();

        protected override async Task<IEnumerable<LeaderBoardEntryDTO>> GetLeaderBoard(Guid clientID, LeaderBoardSubject subject, LeaderBoardGroup group)
        {
            if (group != LeaderBoardGroup.All)
                throw new VerbatimException("Only All leader board group is supported at this time");

            var entries = await LeaderBoardUtil.GetLeaderBoard(GrainFactory, subject).GetScoresForDisplay(clientID);
            return await leaderBoardPlayerInfoCache.ConvertToDTO(clientID, entries.Value.entries);
        }
    }
}
