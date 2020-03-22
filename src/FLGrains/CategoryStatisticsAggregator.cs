using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class CategoryStatisticsAggregator : Aggregator<CategoryStatisticsData, CategoryStatisticsData>, ICategoryStatisticsAggregator, ICategoryStatisticsAggregateRetriever
    {
        protected override CategoryStatisticsData AddDelta(CategoryStatisticsData current, CategoryStatisticsData delta)
        {
            if (delta == null || delta.WordScores == null)
                return current;

            foreach (var (key, value) in delta.WordScores)
            {
                if (!current.WordScores.TryGetValue(key, out var currentValue))
                    currentValue = 0;

                current.WordScores[key] = value + currentValue;
            }

            return current;
        }

        protected override CategoryStatisticsData GetDefault() => new CategoryStatisticsData();
    }

    [StatelessWorker]
    class CategoryStatisticsAggregationWorker : AggregationWorker<CategoryStatisticsDelta, CategoryStatisticsData>, ICategoryStatisticsAggregationWorker
    {
        protected override TimeSpan UpdateInterval => TimeSpan.FromMinutes(10);


        protected override CategoryStatisticsData AddDelta(CategoryStatisticsData current, CategoryStatisticsDelta delta)
        {
            if (delta is CategoryStatisticsDelta.WordUsage u)
            {
                if (!current.WordScores.TryGetValue(u.Word, out var currentValue))
                    currentValue = 0;

                current.WordScores[u.Word] = currentValue + 1;
            }
            else if (delta is CategoryStatisticsDelta.UpVote)
                ++current.UpVotes;
            else if (delta is CategoryStatisticsDelta.DownVote)
                ++current.DownVotes;

            return current;
        }

        protected override IAggregator<CategoryStatisticsData> GetAggregator() => GrainFactory.GetGrain<ICategoryStatisticsAggregator>(this.GetPrimaryKeyString());

        protected override CategoryStatisticsData GetDefault() => new CategoryStatisticsData() { WordScores = new Dictionary<string, int>() };
    }

    [StatelessWorker]
    class CategoryStatisticsAggregatorCache : AggregatorCache<CategoryStatisticsData, AggregatedCategoryStatisticsData>, ICategoryStatisticsAggregatorCache
    {
        readonly IConfigReader configReader;

        public CategoryStatisticsAggregatorCache(IConfigReader configReader) => this.configReader = configReader;

        protected override TimeSpan UpdateInterval => TimeSpan.FromMinutes(1);

        protected override IAggregateRetriever<CategoryStatisticsData> GetAggregateRetriever() => GrainFactory.GetGrain<ICategoryStatisticsAggregateRetriever>(this.GetPrimaryKeyString());

        protected override AggregatedCategoryStatisticsData TransformData(CategoryStatisticsData data)
        {
            var total = data.WordScores.Sum(kv => (long)kv.Value);

            var result = new AggregatedCategoryStatisticsData
            {
                DownVotes = data.DownVotes,
                UpVotes = data.UpVotes
            };

            if (total == 0)
                result.WordScores = data.WordScores.ToDictionary(kv => kv.Key, kv => (byte)2);
            else
            {
                var mean = total / (float)data.WordScores.Count;
                var config = configReader.Config.ConfigValues;
                var threshold3 = mean * config.WordScoreThreshold3;
                var threshold2 = mean * config.WordScoreThreshold2;

                result.WordScores = data.WordScores.ToDictionary(kv => kv.Key, kv => (byte)(kv.Value < threshold3 ? 3 : kv.Value < threshold2 ? 2 : 1));
            }

            return result;
        }

        public async Task<byte> GetScore(string word)
        {
            var current = await GetData();

            if (current.WordScores.TryGetValue(word, out var result))
                return result;

            return 2;
        }

        public async Task<(ulong upVotes, ulong downVotes)> GetVotes()
        {
            var current = await GetData();
            return (current.UpVotes, current.DownVotes);
        }
    }
}
