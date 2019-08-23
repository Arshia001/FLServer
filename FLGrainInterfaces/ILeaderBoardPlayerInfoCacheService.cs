using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface ILeaderBoardPlayerInfoCacheService
    {
        Task<IReadOnlyList<LeaderBoardEntryDTO>> ConvertToDTO(Guid clientID, IReadOnlyList<LeaderBoardEntry> entries);
    }
}
