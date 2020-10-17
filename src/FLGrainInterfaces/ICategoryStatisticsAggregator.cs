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
    public class CategoryStatisticsData
    {
        [Id(0)]
        public Dictionary<string, int> WordScores { get; set; } = new Dictionary<string, int>();

        [Id(1)]
        public ulong UpVotes { get; set; }

        [Id(2)]
        public ulong DownVotes { get; set; }
    }

    public class AggregatedCategoryStatisticsData
    {
        public Dictionary<string, byte> WordScores { get; set; } = new Dictionary<string, byte>();
        public ulong UpVotes { get; set; }
        public ulong DownVotes { get; set; }
    }

    public abstract class CategoryStatisticsDelta
    {
        public class WordUsage : CategoryStatisticsDelta
        {
            public string Word { get; set; }

            public WordUsage(string word) => Word = word;
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
        Task<IEnumerable<byte>> GetScores(IEnumerable<string> words);
        Task<(ulong upVotes, ulong downVotes)> GetVotes();
    }
}
