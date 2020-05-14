using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Singleton<T>(T t) => new[] { t };

        public static T MinBy<T, TComp>(this IEnumerable<T> ts, Func<T, TComp> transform)
            where TComp : IComparable<TComp>
        {
            var e = ts.GetEnumerator();

            if (!e.MoveNext())
                throw new InvalidOperationException("Sequence contains no elements");

            var best = e.Current;
            var bestComp = transform(best);

            while (e.MoveNext())
            {
                var current = e.Current;
                var comp = transform(current);
                if (comp.CompareTo(bestComp) < 0)
                {
                    bestComp = comp;
                    best = current;
                }
            }

            return best;
        }
    }
}
