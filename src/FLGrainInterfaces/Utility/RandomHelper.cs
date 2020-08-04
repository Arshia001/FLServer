using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FLGrainInterfaces
{
    public static class RandomHelper
    {
        public static IEnumerable<int> GetUnique(int min, int max, int count)
        {
            if (count > max - min)
                throw new Exception($"Interval [{min},{max}) is too short to contain {count} unique numbers");

            var set = new HashSet<int>();
            while (set.Count < count)
                set.Add(GetInt32(min, max));

            return set;
        }

        public static IEnumerable<int> GetUniqueExcept(int min, int max, int count, Func<int, bool> shouldExclude, int numExcluded)
        {
            if (count > max - min + numExcluded)
                throw new Exception($"Interval [{min},{max}) except {numExcluded} values is too short to contain {count} unique numbers");

            var set = new HashSet<int>();
            while (set.Count < count)
            {
                var next = GetInt32(min, max);
                if (!shouldExclude(next))
                    set.Add(next);
            }

            return set;
        }

        public static int GetInt32(int max) => RandomNumberGenerator.GetInt32(max);

        public static int GetInt32(int min, int max) => RandomNumberGenerator.GetInt32(max - min) + min;

        public static double GetDouble() => RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;

        public static double GetDouble(double min, double max) => GetDouble() * (max - min) + min;
    }
}
