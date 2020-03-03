using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Singleton<T>(T t) => new[] { t };
    }
}
