using Orleans;
using FLGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class LeaderBoardPlayerInfoCacheService : ILeaderBoardPlayerInfoCacheService
    {
        readonly MemoryCache leaderBoardInfoCache = new MemoryCache("LBInfo");
        readonly IGrainFactory grainFactory;

        public LeaderBoardPlayerInfoCacheService(IGrainFactory grainFactory) => this.grainFactory = grainFactory;

        public async Task<IReadOnlyList<PlayerLeaderBoardInfo>> GetProfiles(Guid clientID, IReadOnlyList<LeaderBoardEntry> entries)
        {
            // Cache user info for one hour, evict and update afterwards in case they update their profiles
            var cacheExpiration = DateTimeOffset.Now.AddHours(1);
            var numTop = 200ul;

            var profiles = new PlayerLeaderBoardInfo[entries.Count];
            for (int Idx = 0; Idx < profiles.Length; ++Idx)
            {
                if (entries[Idx].ID == clientID)
                    continue;

                if (entries[Idx].Rank <= numTop)
                    profiles[Idx] = (PlayerLeaderBoardInfo)leaderBoardInfoCache[entries[Idx].ID.ToString()];

                if (profiles[Idx] == null)
                {
                    profiles[Idx] = await grainFactory.GetGrain<IPlayer>(entries[Idx].ID).GetLeaderBoardInfo();
                    if (entries[Idx].Rank <= numTop)
                        leaderBoardInfoCache.Add(entries[Idx].ID.ToString(), profiles[Idx], cacheExpiration);
                }
            }

            return profiles;
        }
    }
}
