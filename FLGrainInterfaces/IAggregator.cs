using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IAggregator<TAggregateDelta> : IGrain
    {
        Task AddDelta(TAggregateDelta delta);
    }

    public interface IAggregateRetriever<TData> : IGrain
    {
        Task<TData> GetData();
    }

    public interface IAggregationWorker<TDelta, TAggregateDelta> : IGrain
    {
        Task AddDelta(TDelta delta);
    }

    public interface IAggregatorCache<TData> : IGrain
    {
        Task<TData> GetData();
    }
}
