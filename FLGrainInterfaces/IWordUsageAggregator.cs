using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WordUsageData = System.Collections.Generic.Dictionary<string, int>;

namespace FLGrainInterfaces
{
    public interface IWordUsageAggregator : IGrainWithStringKey, IAggregator<WordUsageData> { }

    public interface IWordUsageAggregateRetriever : IGrainWithStringKey, IAggregateRetriever<WordUsageData> { }

    public interface IWordUsageAggregationWorker : IGrainWithStringKey, IAggregationWorker<string, WordUsageData> { }

    public interface IWordUsageAggregatorCache : IGrainWithStringKey, IAggregatorCache<Dictionary<string, byte>>
    {
        Task<byte> GetScore(string word);
    }
}
