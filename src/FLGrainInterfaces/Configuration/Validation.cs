using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    static class Validation
    {
        [DoesNotReturn] public static void FailWith(string error) => throw new ArgumentException(error);

        public static void CheckList<T>(IReadOnlyList<T>? list, string name)
        {
            if (list == null || list.Count == 0)
                FailWith($"No {name}");
        }

        public static void CheckString(string str, string name)
        {
            if (string.IsNullOrEmpty(str))
                FailWith($"{name} should not be null or empty");
        }

        public static void CheckNotEqual<T>(T t, T test, string name)
        {
            if (
                (t == null && test == null) ||
                (t != null && test != null && t.Equals(test))
                )
                FailWith($"{name} shouldn't be {(test == null ? "null" : test.ToString())}");
        }
    }
}
