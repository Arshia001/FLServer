using Bond;
using Orleans;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    [Schema, BondSerializationTag("~wud")]
    public class WordUsageData : IOnDeserializedHandler
    {
        [Id(0)]
        public Dictionary<string, int> WordScores { get; set; }

        public void OnDeserialized()
        {
            if (WordScores == null)
                WordScores = new Dictionary<string, int>();
        }
    }

    public interface IWordUsageAggregator : IGrainWithStringKey, IAggregator<WordUsageData> { }

    public interface IWordUsageAggregateRetriever : IGrainWithStringKey, IAggregateRetriever<WordUsageData> { }

    public interface IWordUsageAggregationWorker : IGrainWithStringKey, IAggregationWorker<string, WordUsageData> { }

    public interface IWordUsageAggregatorCache : IGrainWithStringKey, IAggregatorCache<Dictionary<string, byte>>
    {
        Task<byte> GetScore(string word);
    }
}
