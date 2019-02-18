using FLGrainInterfaces;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class AggregatorState<T>
    {
        public T Data { get; set; }
    }

    abstract class Aggregator<TData, TAggregateDelta> : Grain<AggregatorState<TData>>, IAggregator<TAggregateDelta>, IAggregateRetriever<TData> 
        where TData: class
    {
        protected abstract TData GetDefault();
        protected abstract TData AddDelta(TData current, TAggregateDelta delta);


        public override Task OnActivateAsync()
        {
            if (State.Data == null)
                State.Data = GetDefault();

            return base.OnActivateAsync();
        }

        public Task AddDelta(TAggregateDelta delta)
        {
            State.Data = AddDelta(State.Data, delta);
            return WriteStateAsync();
        }

        public Task<TData> GetData() => Task.FromResult(State.Data);
    }

    [StatelessWorker]
    abstract class AggregationWorker<TDelta, TAggregateDelta> : Grain, IAggregationWorker<TDelta, TAggregateDelta>
    {
        IDisposable timerHandle;
        bool haveAnyData = false;
        TAggregateDelta aggregate;


        protected abstract TimeSpan UpdateInterval { get; }


        protected abstract TAggregateDelta GetDefault();
        protected abstract TAggregateDelta AddDelta(TAggregateDelta current, TDelta delta);
        protected abstract IAggregator<TAggregateDelta> GetAggregator();


        public override Task OnActivateAsync()
        {
            aggregate = GetDefault();
            return base.OnActivateAsync();
        }

        public Task AddDelta(TDelta delta)
        {
            if (timerHandle == null)
                timerHandle = RegisterTimer(UpdateAggregator, null, UpdateInterval, UpdateInterval);

            haveAnyData = true;
            aggregate = AddDelta(aggregate, delta);

            return Task.CompletedTask;
        }

        Task UpdateAggregator(object _)
        {
            if (!haveAnyData)
                return Task.CompletedTask;

            var current = aggregate;

            aggregate = GetDefault();
            haveAnyData = false;

            return GetAggregator().AddDelta(current);
        }
    }

    [StatelessWorker]
    abstract class AggregatorCache<TData, TTransformedData> : Grain, IAggregatorCache<TTransformedData>
    {
        TTransformedData cached;
        DateTime updateTime;


        protected abstract TimeSpan UpdateInterval { get; }


        protected abstract IAggregateRetriever<TData> GetAggregateRetriever();
        protected abstract TTransformedData TransformData(TData data);


        public async Task<TTransformedData> GetData()
        {
            if (DateTime.Now - updateTime > UpdateInterval)
            {
                try
                {
                    updateTime = DateTime.Now;
                    cached = TransformData(await GetAggregateRetriever().GetData());
                }
                catch
                {
                    updateTime = default;
                }
            }

            return cached;
        }
    }
}
