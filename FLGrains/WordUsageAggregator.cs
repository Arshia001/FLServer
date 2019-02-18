using FLGrainInterfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordUsageData = System.Collections.Generic.Dictionary<string, int>;

namespace FLGrains
{
    class WordUsageAggregator : Aggregator<WordUsageData, WordUsageData>, IWordUsageAggregator, IWordUsageAggregateRetriever
    {
        protected override WordUsageData AddDelta(WordUsageData current, WordUsageData delta)
        {
            foreach (var (key, value) in delta)
            {
                if (!current.TryGetValue(key, out var currentValue))
                    currentValue = 0;

                current[key] = value + currentValue;
            }

            return current;
        }

        protected override WordUsageData GetDefault() => new WordUsageData();
    }

    class WordUsageAggregationWorker : AggregationWorker<string, WordUsageData>, IWordUsageAggregationWorker
    {
        protected override TimeSpan UpdateInterval => TimeSpan.FromSeconds(10); //?? TimeSpan.FromMinutes(10);


        protected override WordUsageData AddDelta(WordUsageData current, string delta)
        {
            if (!current.TryGetValue(delta, out var currentValue))
                currentValue = 0;

            current[delta] = currentValue + 1;
            return current;
        }

        protected override IAggregator<WordUsageData> GetAggregator() => GrainFactory.GetGrain<IWordUsageAggregator>(this.GetPrimaryKeyString());

        protected override WordUsageData GetDefault() => new WordUsageData();
    }

    class WordUsageAggregatorCache : AggregatorCache<WordUsageData, Dictionary<string, byte>>, IWordUsageAggregatorCache
    {
        protected override TimeSpan UpdateInterval => TimeSpan.FromSeconds(10); //?? TimeSpan.FromMinutes(1);

        protected override IAggregateRetriever<WordUsageData> GetAggregateRetriever() => GrainFactory.GetGrain<IWordUsageAggregateRetriever>(this.GetPrimaryKeyString());

        protected override Dictionary<string, byte> TransformData(WordUsageData data)
        {
            var total = data.Sum(kv => (long)kv.Value);

            if (total == 0)
                return data.ToDictionary(kv => kv.Key, kv => (byte)2);

            var mean = total / (float)data.Count;
            var threshold3 = mean * 0.7f; //?? to config parameters
            var threshold2 = mean * 1.3f;

            return data.ToDictionary(kv => kv.Key, kv => (byte)(kv.Value < threshold3 ? 3 : kv.Value < threshold2 ? 2 : 1));
        }

        public async Task<byte> GetScore(string word)
        {
            var current = await GetData();

            if (current.TryGetValue(word, out var result))
                return result;

            return 0;
        }
    }
}
