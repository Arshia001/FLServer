using FLGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains.ServiceInterfaces
{
    public interface ILeaderBoardPlayerInfoCacheService
    {
        Task<IReadOnlyList<PlayerLeaderBoardInfoDTO>> GetProfiles(Guid clientID, IReadOnlyList<LeaderBoardEntry> entries);
    }
}
