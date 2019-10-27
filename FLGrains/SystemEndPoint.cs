﻿using FLGameLogic;
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
    class SystemEndPoint : SystemEndPointBase
    {
        readonly IConfigReader configReader;
        readonly ILeaderBoardPlayerInfoCacheService leaderBoardPlayerInfoCache;

        public SystemEndPoint(IConfigReader configReader, ILeaderBoardPlayerInfoCacheService leaderBoardPlayerInfoCache)
        {
            this.leaderBoardPlayerInfoCache = leaderBoardPlayerInfoCache;
            this.configReader = configReader;
        }

        protected override async Task<(OwnPlayerInfo playerInfo, ConfigValuesDTO configData, IEnumerable<GoldPackConfigDTO> goldPacks)> GetStartupInfo(Guid clientID)
        {
            var playerInfo = await GrainFactory.GetGrain<IPlayer>(clientID).PerformStartupTasksAndGetInfo();
            var config = configReader.Config;
            return (playerInfo, config.ConfigValues, config.GoldPacks.Values.Select(g => (GoldPackConfigDTO)g));
        }

        protected override async Task<IEnumerable<LeaderBoardEntryDTO>> GetLeaderBoard(Guid clientID, LeaderBoardSubject subject, LeaderBoardGroup group)
        {
            if (group != LeaderBoardGroup.All)
                throw new VerbatimException("Only All leader board group is supported at this time");

            var entries = await LeaderBoardUtil.GetLeaderBoard(GrainFactory, subject).GetScoresForDisplay(clientID);
            return entries.Value.entries
                .Zip(await leaderBoardPlayerInfoCache.GetProfiles(clientID, entries.Value.entries), (e, p) => (e, p))
                .Select(e => new LeaderBoardEntryDTO(e.p, e.e.Rank, e.e.Score));
        }
    }
}
