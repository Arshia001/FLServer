using FLGrains.ServiceInterfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains.Utility
{
    class PersistStateEventArgs<TState> : EventArgs
    {
        public TState State { get; }

        internal PersistStateEventArgs(TState state) => State = state;
    }

    class GrainStateWrapper<TState>
        where TState : new()
    {
        public event EventHandler<PersistStateEventArgs<TState>>? Persist;

        readonly IPersistentState<TState> state;

        bool lazyPersistPending;

        public GrainStateWrapper(IPersistentState<TState> state) => this.state = state;

        public bool LazyPersistPending => lazyPersistPending;

        Task PerformPersist()
        {
            Persist?.Invoke(this, new PersistStateEventArgs<TState>(state.State));
            lazyPersistPending = false;
            return state.WriteStateAsync();
        }

        public Task PerformLazyPersistIfPending() => lazyPersistPending ? PerformPersist() : Task.CompletedTask;

        public Task ClearState() => state.ClearStateAsync();

        public void UseState(Action<TState> f) => f(state.State);

        public T UseState<T>(Func<TState, T> f) => f(state.State);

        public async Task UseStateAndPersist(Func<TState, Task> f)
        {
            await f(state.State);
            await PerformPersist();
        }

        public async Task<T> UseStateAndPersist<T>(Func<TState, Task<T>> f)
        {
            var result = await f(state.State);
            await PerformPersist();
            return result;
        }

        public async Task UseStateAndPersist(Action<TState> f)
        {
            f(state.State);
            await PerformPersist();
        }

        public async Task<T> UseStateAndPersist<T>(Func<TState, T> f)
        {
            var result = f(state.State);
            await PerformPersist();
            return result;
        }

        public void UseStateAndLazyPersist(Action<TState> f)
        {
            f(state.State);
            lazyPersistPending = true;
        }

        public T UseStateAndLazyPersist<T>(Func<TState, T> f)
        {
            var result = f(state.State);
            lazyPersistPending = true;
            return result;
        }

        public async Task UseStateAndMaybePersist(Func<TState, Task<bool>> f)
        {
            if (await f(state.State))
                await PerformPersist();
        }

        public async Task<T> UseStateAndMaybePersist<T>(Func<TState, Task<(bool shouldPersist, T result)>> f)
        {
            var result = await f(state.State);
            if (result.shouldPersist)
                await PerformPersist();
            return result.result;
        }

        public async Task UseStateAndMaybePersist(Func<TState, bool> f)
        {
            if (f(state.State))
                await PerformPersist();
        }

        public async Task<T> UseStateAndMaybePersist<T>(Func<TState, (bool shouldPersist, T result)> f)
        {
            var result = f(state.State);
            if (result.shouldPersist)
                await PerformPersist();
            return result.result;
        }
    }
}
