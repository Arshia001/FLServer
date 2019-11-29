/* Taken mostly from Redis (https://github.com/antirez/redis), copyright notice:
 * Copyright (c) 2006-2015, Salvatore Sanfilippo
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials 
 *      provided with the distribution.
 * * Neither the name of Redis nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, 
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
 * AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED 
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;

namespace FLGrainInterfaces.Util
{
    public class SkipList<TValue, TScore> : IEnumerable<KeyValuePair<TValue, TScore>>
        where TValue : struct, IEquatable<TValue>, IComparable
        where TScore : struct, IComparable
    {
        public class Node
        {
            SkipListNode slNode;

            public TValue Value => slNode.Value;
            public TScore Score => slNode.Score;

            internal Node(SkipListNode slNode)
            {
                this.slNode = slNode;
            }
        }

        [DebuggerDisplay("{Value} {Score} {Level.Length}")]
        internal class SkipListNode
        {
            public struct SkipListLevel
            {
                public SkipListNode? Forward;
                public ulong Span;
            }

            public TValue Value;
            public TScore Score;
            public SkipListNode? Backward;
            public SkipListLevel[] Level;

            public SkipListNode(int Level, TScore Score, TValue Value)
            {
                this.Level = new SkipListLevel[Level];
                this.Score = Score;
                this.Value = Value;
            }
        }

        Dictionary<TValue, TScore> ValueCache;
        SkipListNode Head;
        SkipListNode? Tail;
        SkipListNode[]? DeserializationTails;
        public ulong Length { get; private set; }
        int Level;

        readonly Random Rand;
        readonly int MaxLevel;
        readonly float P;

        public SkipList(int MaxLevel = 32, float P = 0.25f)
        {
            this.MaxLevel = MaxLevel;
            this.P = P;
            Rand = new Random();
            ValueCache = new Dictionary<TValue, TScore>();

            Level = 1;
            Length = 0;
            Head = new SkipListNode(MaxLevel, default, default);
            for (int j = 0; j < MaxLevel; j++)
            {
                Head.Level[j].Forward = null;
                Head.Level[j].Span = 0;
            }
            Head.Backward = null;
            Tail = null;
        }

        int RandomLevel()
        {
            int level = 1;
            while ((Rand.Next() & 0xFFFF) < (P * 0xFFFF) && level < MaxLevel)
                level += 1;
            return level;
        }

        public TScore GetScore(TValue Value)
        {
            if (ValueCache.TryGetValue(Value, out var Score))
                return Score;

            return default(TScore);
        }

        public bool TryGetScore(TValue Value, out TScore score) => ValueCache.TryGetValue(Value, out score);

        public void Add(TValue Value, TScore Score)
        {
            Delete(Value); // If it exists; otherwise, this has no effect and no performance penalty

            SkipListNode x;
            SkipListNode[] update = new SkipListNode[MaxLevel];
            ulong[] rank = new ulong[MaxLevel];
            int i, level;

            x = Head;
            for (i = this.Level - 1; i >= 0; i--)
            {
                /* store rank that is crossed to reach the insert position */
                rank[i] = i == (this.Level - 1) ? 0 : rank[i + 1];
                var thisLevel = x.Level[i];
                var comp = thisLevel.Forward?.Score.CompareTo(Score) ?? 1;
                while (thisLevel.Forward != null &&
                    (comp > 0 || (comp == 0 && thisLevel.Forward.Value.CompareTo(Value) > 0)))
                {
                    rank[i] += thisLevel.Span;
                    x = thisLevel.Forward;
                    comp = thisLevel.Forward?.Score.CompareTo(Score) ?? 1;
                }
                update[i] = x;
            }

            level = RandomLevel();
            if (level > this.Level)
            {
                for (i = this.Level; i < level; i++)
                {
                    rank[i] = 0;
                    update[i] = this.Head;
                    update[i].Level[i].Span = this.Length;
                }
                this.Level = level;
            }

            x = new SkipListNode(level, Score, Value);
            for (i = 0; i < level; i++)
            {
                x.Level[i].Forward = update[i].Level[i].Forward;
                update[i].Level[i].Forward = x;

                /* update span covered by update[i] as x is inserted here */
                x.Level[i].Span = update[i].Level[i].Span - (rank[0] - rank[i]);
                update[i].Level[i].Span = (rank[0] - rank[i]) + 1;
            }

            /* increment span for untouched levels */
            for (i = level; i < this.Level; i++)
            {
                update[i].Level[i].Span++;
            }

            x.Backward = (update[0] == this.Head) ? null : update[0];
            var level0 = x.Level[0];
            if (level0.Forward != null)
                level0.Forward.Backward = x;
            else
                this.Tail = x;
            this.Length++;

            ValueCache.Add(Value, Score);
        }

        public void AddLast_ForDeserialization(TValue Value, TScore Score)
        {
            if (DeserializationTails == null)
            {
                if (Length != 0)
                    throw new InvalidOperationException("Cannot use deserialization features when not empty");
                DeserializationTails = new SkipListNode[MaxLevel];
                DeserializationTails[0] = Head;
            }

            SkipListNode x;
            int i, level;

            level = RandomLevel();
            if (level > this.Level)
            {
                for (i = this.Level; i < level; i++)
                {
                    if (DeserializationTails[i] == null)
                    {
                        DeserializationTails[i] = Head;
                        DeserializationTails[i].Level[i].Span = Length;
                    }
                }
                this.Level = level;
            }

            x = new SkipListNode(level, Score, Value);
            for (i = 0; i < level; i++)
            {
                DeserializationTails[i].Level[i].Forward = x;

                x.Level[i].Span = 0; // No span either
                DeserializationTails[i].Level[i].Span++;
            }

            for (i = level; i < this.Level; i++)
            {
                DeserializationTails[i].Level[i].Span++;
            }

            x.Backward = (DeserializationTails[0] == this.Head) ? null : DeserializationTails[0];
            this.Tail = x;
            this.Length++;

            for (i = 0; i < level; ++i)
                DeserializationTails[i] = x;

            ValueCache.Add(Value, Score);
        }

        public void FinalizeDeserialization()
        {
            DeserializationTails = null;
        }

        void DeleteNode(SkipListNode x, SkipListNode[] update)
        {
            int i;
            for (i = 0; i < this.Level; i++)
            {
                if (update[i].Level[i].Forward == x)
                {
                    update[i].Level[i].Span += x.Level[i].Span - 1;
                    update[i].Level[i].Forward = x.Level[i].Forward;
                }
                else
                {
                    update[i].Level[i].Span -= 1;
                }
            }
            var level0 = x.Level[0];
            if (level0.Forward != null)
            {
                level0.Forward.Backward = x.Backward;
            }
            else
            {
                this.Tail = x.Backward;
            }
            while (this.Level > 1 && this.Head.Level[this.Level - 1].Forward == null)
                this.Level--;
            this.Length--;
        }

        public bool Delete(TValue obj)
        {
            if (!ValueCache.TryGetValue(obj, out var score))
                return false;

            ValueCache.Remove(obj);

            var update = new SkipListNode[MaxLevel];
            SkipListNode x;
            int i;

            x = this.Head;
            for (i = this.Level - 1; i >= 0; i--)
            {
                var thisLevel = x.Level[i];
                var comp = thisLevel.Forward?.Score.CompareTo(score) ?? 1;
                while (thisLevel.Forward != null &&
                    (comp > 0 || (comp == 0 && thisLevel.Forward.Value.CompareTo(obj) > 0)))
                {
                    x = thisLevel.Forward;
                    comp = thisLevel.Forward?.Score.CompareTo(score) ?? 1;
                }
                update[i] = x;
            }

            var toDelete = x.Level[0].Forward;
            if (toDelete != null && score.CompareTo(x.Score) == 0 && x.Value.Equals(obj))
            {
                DeleteNode(toDelete, update);
                return true;
            }
            return false;
        }

        public ulong GetRank(TValue o)
        {
            if (!ValueCache.TryGetValue(o, out var score))
                return 0;

            SkipListNode x;
            ulong rank = 0;
            int i;

            x = this.Head;
            for (i = this.Level - 1; i >= 0; i--)
            {
                var thisLevel = x.Level[i];
                var comp = thisLevel.Forward?.Score.CompareTo(score) ?? 1;
                while (thisLevel.Forward != null &&
                    (comp > 0 || (comp == 0 && thisLevel.Forward.Value.CompareTo(o) >= 0)))
                {
                    rank += thisLevel.Span;
                    x = thisLevel.Forward;
                    comp = thisLevel.Forward?.Score.CompareTo(score) ?? 1;
                }

                if (x.Value.Equals(o))
                {
                    return rank;
                }
            }
            return 0;
        }

        public Node? GetElementByRank(ulong rank)
        {
            var Res = GetElementByRank_Int(rank);
            return Res == null ? null : new Node(Res);
        }

        SkipListNode? GetElementByRank_Int(ulong rank)
        {
            SkipListNode x;
            ulong traversed = 0;
            int i;

            x = this.Head;
            for (i = this.Level - 1; i >= 0; i--)
            {
                var thisLevel = x.Level[i];
                while (thisLevel.Forward != null && (traversed + thisLevel.Span) <= rank)
                {
                    traversed += thisLevel.Span;
                    x = thisLevel.Forward;
                }
                if (traversed == rank)
                {
                    return x;
                }
            }
            return null;
        }

        public IEnumerable<Node> GetRangeByRank(long start, long end)
        {
            return GetRangeByRankInt(start, end, false);
        }

        public IEnumerable<Node> GetReverseRangeByRank(long start, long end)
        {
            return GetRangeByRankInt(start, end, true);
        }

        IEnumerable<Node> GetRangeByRankInt(long start, long end, bool reverse)
        {
            var llen = (long)Length;
            if (start < 0) start = llen + start;
            if (end < 0) end = llen + end;
            if (start < 0) start = 0;

            if (start > end || start >= llen)
                return new Node[] { };

            if (end >= llen) end = llen - 1;
            var rangelen = (int)(end - start) + 1;

            var Result = new List<Node>();

            SkipListNode? ln;

            if (reverse)
            {
                ln = this.Tail;
                if (start > 0)
                    ln = GetElementByRank_Int((ulong)(llen - start));
            }
            else
            {
                ln = this.Head.Level[0].Forward;
                if (start > 0)
                    ln = GetElementByRank_Int((ulong)(start + 1));
            }

            if (ln != null)
                while (rangelen-- > 0)
                {
                    Result.Add(new Node(ln));
                    ln = (reverse ? ln.Backward : ln.Level[0].Forward) ?? throw new Exception("Failed to get next node in traversal");
                }
            else
                throw new Exception("Failed to find range start node");

            return Result;
        }

        public IEnumerator<KeyValuePair<TValue, TScore>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class Enumerator : IEnumerator<KeyValuePair<TValue, TScore>>
        {
            SkipList<TValue, TScore> List;
            SkipListNode Node;

            public Enumerator(SkipList<TValue, TScore> List)
            {
                this.List = List;
                Node = List.Head;
            }

            public KeyValuePair<TValue, TScore> Current => new KeyValuePair<TValue, TScore>(Node.Value, Node.Score);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var level0 = Node.Level[0];
                if (level0.Forward != null)
                {
                    Node = level0.Forward;
                    return true;
                }
                else
                    return false;
            }

            public void Reset()
            {
                Node = List.Head;
            }
        }
    }
}
