using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    public static class EditDistance
    {
        public static bool IsLessThan(string word1, string word2, int maxDistance)
        {
            string smaller, longer;
            if (word1.Length < word2.Length)
            {
                smaller = word1.ToLower();
                longer = word2.ToLower();
            }
            else
            {
                smaller = word2.ToLower();
                longer = word1.ToLower();
            }

            if (longer.Length - smaller.Length > maxDistance)
                return false;

            int[] distances = new int[2 * maxDistance + 1]; //?? what if maxdistance > smaller.length?
            int[] prevRowDistances = new int[2 * maxDistance + 1];

            for (var i = maxDistance + 1; i <= 2 * maxDistance; ++i)
                prevRowDistances[i] = i - maxDistance;

            for (int i = 1; i <= longer.Length; ++i)
            {
                for (int _j = 0; _j <= 2 * maxDistance; ++_j)
                {
                    var j = i + _j - maxDistance;
                    if (j < 0)
                        continue;
                    if (j > smaller.Length)
                        break;

                    if (j == 0)
                        distances[_j] = i;
                    else if (smaller[j - 1] == longer[i - 1])
                        distances[_j] = prevRowDistances[_j];
                    else
                        distances[_j] = 1 + Math.Min(Math.Min(
                            _j <= 0 ? int.MaxValue : distances[_j - 1],
                            prevRowDistances[_j]),
                            _j >= 2 * maxDistance ? int.MaxValue : prevRowDistances[_j + 1]);
                }

                (prevRowDistances, distances) = (distances, prevRowDistances); // we don't care about the initial contents of distances, so we just use prevRowDistances as an optimization to avoid memory allocation
            }

            return prevRowDistances[maxDistance + smaller.Length - longer.Length] <= maxDistance;
        }
    }
}
