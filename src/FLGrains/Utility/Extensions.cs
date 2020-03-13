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

        public static T? AsNullable<T>(this T t) where T : class => t;
    }
}
