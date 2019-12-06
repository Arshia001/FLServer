using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public static class FLTaskExtensions
    {
        public static Task<T> UnwrapImmutable<T>(this Task<Immutable<T>> task) => task.ContinueWith(t => t.Result.Value, TaskContinuationOptions.OnlyOnRanToCompletion);

        public static Task<bool> True { get; } = Task.FromResult(true);

        public static Task<bool> False { get; } = Task.FromResult(false);
    }
}
