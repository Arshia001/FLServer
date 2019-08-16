using Bond;
using Orleans;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    [Schema, BondSerializationTag("~csd")]
    public class CategoryStatisticsData : IOnDeserializedHandler
    {
        [Id(0)]
        public Dictionary<string, int> WordScores { get; set; }

        [Id(1)]
        public ulong UpVotes { get; set; }

        [Id(2)]
        public ulong DownVotes { get; set; }

        public void OnDeserialized()
        {
            if (WordScores == null)
                WordScores = new Dictionary<string, int>();
        }
    }

    public class AggregatedCategoryStatisticsData
    {
        public Dictionary<string, byte> WordScores { get; set; }
        public ulong UpVotes { get; set; }
        public ulong DownVotes { get; set; }
    }

    public abstract class CategoryStatisticsDelta
    {
        public class WordUsage : CategoryStatisticsDelta
        {
            public string Word { get; set; }
        }

        public class UpVote : CategoryStatisticsDelta { }

        public class DownVote : CategoryStatisticsDelta { }
    }

    public interface ICategoryStatisticsAggregator : IGrainWithStringKey, IAggregator<CategoryStatisticsData> { }

    public interface ICategoryStatisticsAggregateRetriever : IGrainWithStringKey, IAggregateRetriever<CategoryStatisticsData> { }

    public interface ICategoryStatisticsAggregationWorker : IGrainWithStringKey, IAggregationWorker<CategoryStatisticsDelta, CategoryStatisticsData> { }

    public interface ICategoryStatisticsAggregatorCache : IGrainWithStringKey, IAggregatorCache<AggregatedCategoryStatisticsData>
    {
        Task<byte> GetScore(string word);
        Task<(ulong upVotes, ulong downVotes)> GetVotes();
    }
}
