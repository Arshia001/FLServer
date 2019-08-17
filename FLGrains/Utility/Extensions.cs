using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrains
{
    static class Extensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> kv, out T1 t1, out T2 t2)
        {
            t1 = kv.Key;
            t2 = kv.Value;
        }

        public static IEnumerable<int> GetUnique(this Random random, int min, int max, int count)
        {
            if (count > max - min)
                throw new Exception($"Interval [{min},{max}) is too short to contain {count} unique numbers");

            var set = new HashSet<int>();
            while (set.Count < count)
                set.Add(random.Next(min, max));

            return set;
        }

        public static IEnumerable<int> GetUniqueExcept(this Random random, int min, int max, int count, Func<int, bool> shouldExclude, int numExcluded)
        {
            if (count > max - min + numExcluded)
                throw new Exception($"Interval [{min},{max}) except {numExcluded} values is too short to contain {count} unique numbers");

            var set = new HashSet<int>();
            while (set.Count < count)
            {
                var next = random.Next(min, max);
                if (!shouldExclude(next))
                    set.Add(next);
            }

            return set;
        }
    }
}
