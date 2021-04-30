using FLGrainInterfaces;
using FLGrainInterfaces.Util;
using Bond;
using Bond.Tag;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLGrainInterfaces.Configuration;

namespace FLGrains
{
    public class LeaderBoardConverter
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static ArraySegment<byte> Convert(SkipList<Guid, ulong> value, ArraySegment<byte> unused, Type expectedType)
        {
            if (value == null)
                return new ArraySegment<byte>();

            var Result = new byte[value.Length * 24];
            int Idx = 0;
            foreach (var KV in value)
            {
                Array.Copy(KV.Key.ToByteArray(), 0, Result, Idx, 16);
                Array.Copy(BitConverter.GetBytes(KV.Value), 0, Result, Idx + 16, 8);
                Idx += 24;
            }

            return new ArraySegment<byte>(Result);
        }

        public static SkipList<Guid, ulong> Convert(ArraySegment<byte> value, SkipList<Guid, ulong> unused, Type expectedType)
        {
            if (value.Count == 0)
                return new SkipList<Guid, ulong>();

            if (value.Array is null)
                throw new ArgumentNullException("value.Array");

            var result = new SkipList<Guid, ulong>();
            using (var stream = new MemoryStream(value.Array, value.Offset, value.Count))
            {
                var idBuf = new byte[16];
                var scoreBuf = new byte[16];
                while (stream.Position < stream.Length)
                {
                    stream.Read(idBuf, 0, 16);
                    stream.Read(scoreBuf, 0, 8);
                    result.AddLast_ForDeserialization(new Guid(idBuf), BitConverter.ToUInt64(scoreBuf, 0));
                }
            }

            result.FinalizeDeserialization();
            return result;
        }
#pragma warning restore IDE0060 // Remove unused parameter
    }

    [Schema, BondSerializationTag("#lb")]
    public class LeaderBoardState : IGenericSerializable
    {
        static LeaderBoardState()
        {
            CustomTypeRegistry.RegisterTypeConverter(typeof(LeaderBoardConverter));
            CustomTypeRegistry.AddTypeMapping(typeof(SkipList<Guid, ulong>), typeof(nullable<blob>), false);
        }

        [Id(0)]
        public SkipList<Guid, ulong> Scores { get; set; }

        public LeaderBoardState()
        {
            Scores = new SkipList<Guid, ulong>();
        }
    }

    class LeaderBoard : Grain<LeaderBoardState>, ILeaderBoard
    {
        IDisposable? writeStateTimer;
        bool changedSinceLastWrite;
        
        readonly ILogger logger;
        readonly IConfigReader configReader;

        public LeaderBoard(ILogger<ILeaderBoard> logger, IConfigReader configReader)
        {
            this.logger = logger;
            this.configReader = configReader;
        }

        public override Task OnActivateAsync()
        {
            writeStateTimer = RegisterTimer(WriteStateTimerCallback, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            return base.OnActivateAsync();
        }

        Task WriteStateTimerCallback(object? state)
        {
            if (changedSinceLastWrite)
            {
                changedSinceLastWrite = false;
                return WriteStateAsync();
            }

            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            writeStateTimer?.Dispose();
            await WriteStateTimerCallback(null);
            await base.OnDeactivateAsync();
        }

        public Task Set(Guid id, ulong score)
        {
            SetImpl(id, score);

            return Task.CompletedTask;
        }

        void SetImpl(Guid id, ulong score)
        {
            if (!State.Scores.TryGetScore(id, out var existing) || existing < score)
            {
                State.Scores.Add(id, score);
                changedSinceLastWrite = true;
            }

            if (logger.IsEnabled(LogLevel.Trace))
                logger.Trace($"LeaderBoard {this.GetPrimaryKeyString()} - Set score {id} -> {score}, now have {State.Scores.Length} scores");
        }

        public Task<ulong> AddDelta(Guid id, ulong deltaScore)
        {
            ulong score = AddDeltaImpl(id, deltaScore);

            return Task.FromResult(score);
        }

        private ulong AddDeltaImpl(Guid id, ulong deltaScore)
        {
            var score = State.Scores.GetScore(id);
            if (deltaScore == 0)
                return score;

            score += deltaScore;
            State.Scores.Add(id, score);
            changedSinceLastWrite = true;

            if (logger.IsEnabled(LogLevel.Trace))
                logger.Trace($"LeaderBoard {this.GetPrimaryKeyString()} - Add score {id} -> {deltaScore}, now have {State.Scores.Length} scores");
            return score;
        }

        ulong GetRankImpl(Guid id)
        {
            return State.Scores.GetRank(id);
        }

        public Task<ulong> GetRank(Guid id)
        {
            return Task.FromResult(GetRankImpl(id));
        }

        public Task<ulong> SetAndGetRank(Guid id, ulong score)
        {
            SetImpl(id, score);
            return Task.FromResult(GetRankImpl(id));
        }

        public Task<(ulong score, ulong rank)> AddDeltaAndGetRank(Guid id, ulong deltaScore)
        {
            var score = AddDeltaImpl(id, deltaScore);
            return Task.FromResult((score, GetRankImpl(id)));
        }

        public Task<Immutable<List<ulong>>> GetRanks(IEnumerable<Guid> ids)
        {
            return Task.FromResult(ids.Select(id => GetRankImpl(id)).ToList().AsImmutable());
        }

        List<LeaderBoardEntry> GetScoresAroundImpl(Guid id, uint countInEachDirection)
        {
            var Rank = State.Scores.GetRank(id) - countInEachDirection;
            return State.Scores.GetRangeByRank((long)Rank, (long)Rank + countInEachDirection * 2)
                .Select(s => new LeaderBoardEntry() { ID = s.Value, Score = s.Score, Rank = ++Rank }) // ++Rank because the ranks in the skiplist are zero based, so we add one BEFORE assigning to each entry
                .ToList();
        }

        public Task<Immutable<List<LeaderBoardEntry>>> GetScoresAround(Guid id, uint countInEachDirection)
        {
            return Task.FromResult(GetScoresAroundImpl(id, countInEachDirection).AsImmutable());
        }

        List<LeaderBoardEntry> GetTopScoresImpl(uint count)
        {
            ulong Rank = 1;
            return State.Scores.GetRangeByRank(0, count - 1)
                .Select(s => new LeaderBoardEntry() { ID = s.Value, Score = s.Score, Rank = Rank++ })
                .ToList();
        }

        public Task<Immutable<List<LeaderBoardEntry>>> GetTopScores(uint count)
        {
            return Task.FromResult(GetTopScoresImpl(count).AsImmutable());
        }

        public Task<Immutable<(ulong ownRank, List<LeaderBoardEntry> entries)>> GetScoresForDisplay(Guid userID)
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.Trace($"LeaderBoard {this.GetPrimaryKeyString()} - query scores for {userID}");

            var config = configReader.Config;
            var topCount = config.ConfigValues.LeaderBoardTopScoreCount;
            var aroundCount = config.ConfigValues.LeaderBoardAroundScoreCount;

            var OwnRank = GetRankImpl(userID);
            if (OwnRank == 0)
                return Task.FromResult((OwnRank, GetTopScoresImpl(topCount)).AsImmutable());
            if (OwnRank < topCount + aroundCount)
                return Task.FromResult((OwnRank, GetTopScoresImpl(Math.Max(topCount, (uint)OwnRank + aroundCount))).AsImmutable());
            else
                return Task.FromResult((OwnRank, GetTopScoresImpl(topCount).Concat(GetScoresAroundImpl(userID, aroundCount)).ToList()).AsImmutable());
        }

        public Task Remove(Guid userID)
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.Trace($"LeaderBoard {this.GetPrimaryKeyString()} - remove score for {userID}");

            State.Scores.Delete(userID);
            return Task.CompletedTask;
        }
    }
}
