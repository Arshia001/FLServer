using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public class CategoryConfig
    {
        public class Entry
        {
            public string Word { get; private set; }
            public byte Score { get; private set; }
            public List<string> Corrections { get; private set; } = new List<string>();
        }

        public string Name { get; private set; }
        public List<Entry> Words { get; private set; }
    }


    public class ConfigData : ICloneable
    {
        public List<CategoryConfig> Categories { get; set; }

        [JsonIgnore]
        public int Version { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class ReadOnlyConfigData
    {
        public IReadOnlyList<CategoryConfig> Categories => data.Categories;
        public IReadOnlyList<FLGameLogic.WordCategory> CategoriesAsGameLogicFormat { get; }
        public IReadOnlyDictionary<string, FLGameLogic.WordCategory> CategoriesByName { get; }


        public int Version => data.Version;


        ConfigData data;


        public ReadOnlyConfigData(ConfigData data)
        {
            this.data = data;

            CategoriesAsGameLogicFormat = data.Categories.Select(c => new FLGameLogic.WordCategory(c.Name, c.Words.ToDictionary(w => w.Word, w => (w.Score, w.Corrections)))).ToList();
            CategoriesByName = CategoriesAsGameLogicFormat.ToDictionary(c => c.CategoryName);
        }
    }


    public interface ISystemConfig : IGrainWithIntegerKey
    {
        Task UpdateConfigFromDatabase();

        Task<Immutable<ConfigData>> GetConfig();

        Task UploadConfig(string jsonConfig);
    }
}
