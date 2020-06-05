using FLGameLogic;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrainInterfaces.Utility;
using FLGrains.ServiceInterfaces;
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
        readonly ISystemSettingsProvider systemSettings;

        public SystemEndPoint(IConfigReader configReader, ILeaderBoardPlayerInfoCacheService leaderBoardPlayerInfoCache, ISystemSettingsProvider systemSettings)
        {
            this.leaderBoardPlayerInfoCache = leaderBoardPlayerInfoCache;
            this.systemSettings = systemSettings;
            this.configReader = configReader;
        }

        protected override async Task<(OwnPlayerInfoDTO playerInfo, ConfigValuesDTO configData, IEnumerable<GoldPackConfigDTO> goldPacks,
            VideoAdTrackerInfoDTO coinRewardVideo, VideoAdTrackerInfoDTO getCategoryAnswersVideo, IEnumerable<CoinGiftInfoDTO> coinGifts,
            IEnumerable<AvatarPartConfigDTO> avatarParts)> 
            GetStartupInfo(Guid clientID)
        {
            static VideoAdTrackerInfoDTO GetTrackerInfoDTO(VideoAdLimitTrackerInfo state, VideoAdLimitConfig config) =>
                new VideoAdTrackerInfoDTO(
                    timeSinceLastWatched: state.LastAdWatchedTime.HasValue ? DateTime.Now - state.LastAdWatchedTime.Value : default(TimeSpan?),
                    numberWatchedToday: state.NumberWatchedToday,
                    interval: config.Interval ?? TimeSpan.Zero,
                    numberPerDay: config.NumberAllowedPerDay ?? 0
                    );

            var (playerInfo, coinRewardVideo, getCategoryAnswersVideo, gifts) = await GrainFactory.GetGrain<IPlayer>(clientID).PerformStartupTasksAndGetInfo();
            var config = configReader.Config;
            return (
                playerInfo,
                config.ConfigValues,
                config.GoldPacks.Values.Select(g => (GoldPackConfigDTO)g),
                GetTrackerInfoDTO(coinRewardVideo, config.ConfigValues.CoinRewardVideo),
                GetTrackerInfoDTO(getCategoryAnswersVideo, config.ConfigValues.GetCategoryAnswersVideo),
                gifts.Select(g => (CoinGiftInfoDTO)g),
                config.AvatarParts.SelectMany(kv => kv.Value.Select(a => new AvatarPartConfigDTO(a.Value.Type, a.Value.ID, a.Value.Price)))
                );
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

        protected override async Task<Guid?> Login(Guid clientID, string email, string password)
        {
            var player = await PlayerIndex.GetByEmail(GrainFactory, email);

            if (player == null || !await player.ValidatePassword(password))
                return null;

            return player.GetPrimaryKey();
        }

        protected override async Task SendPasswordRecoveryLink(Guid clientID, string email)
        {
            var player = await PlayerIndex.GetByEmail(GrainFactory, email);

            if (player == null)
                return;

            await player.SendPasswordRecoveryLink();
        }
    }
}
