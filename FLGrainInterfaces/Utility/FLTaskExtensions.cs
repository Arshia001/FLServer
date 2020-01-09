using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public static class FLTaskExtensions
    {
        static class NullTaskWrapper<T> where T : class
        {
            public static Task<T?> Value { get; } = Task.FromResult(default(T));
        }

        static class NullTaskWrapperV<T> where T : struct
        {
            public static Task<T?> Value { get; } = Task.FromResult(default(T?));
        }

        public static Task<T> UnwrapImmutable<T>(this Task<Immutable<T>> task) => task.ContinueWith(t => t.Result.Value, TaskContinuationOptions.OnlyOnRanToCompletion);

        public static Task<bool> True { get; } = Task.FromResult(true);

        public static Task<bool> False { get; } = Task.FromResult(false);

        public static Task<T?> Null<T>() where T : class => NullTaskWrapper<T>.Value;

        public static Task<T?> VNull<T>() where T : struct => NullTaskWrapperV<T>.Value;

        public static Task<bool> FromBoolean(bool b) => b ? True : False;
    }
}
