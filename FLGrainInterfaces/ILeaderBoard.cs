using FLGrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public class LeaderBoardEntry
    {
        public Guid ID;
        public ulong Score;
        public ulong Rank;
    }

    [BondSerializationTag("@lb")]
    public interface ILeaderBoard : IGrainWithStringKey
    {
        Task Set(Guid id, ulong score);
        Task<ulong> SetAndGetRank(Guid id, ulong score);
        Task<ulong> AddDelta(Guid id, ulong deltaScore);
        Task<(ulong score, ulong rank)> AddDeltaAndGetRank(Guid id, ulong deltaScore);
        Task<ulong> GetRank(Guid id);
        Task<Immutable<List<ulong>>> GetRanks(IEnumerable<Guid> ids);
        Task<Immutable<List<LeaderBoardEntry>>> GetTopScores(uint count);
        Task<Immutable<List<LeaderBoardEntry>>> GetScoresAround(Guid id, uint countInEachDirection);
        Task<Immutable<(ulong ownRank, List<LeaderBoardEntry> entries)>> GetScoresForDisplay(Guid userID);
        Task Remove(Guid id);
    }

    public static class LeaderBoardUtil
    {
        public static ILeaderBoard GetLeaderBoard(IGrainFactory grainFactory, LeaderBoardSubject subject) =>
            grainFactory.GetGrain<ILeaderBoard>($"S-{subject}");
    }
}
