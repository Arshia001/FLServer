using FLGrainInterfaces.Util;
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
    static class ExpressionUtil
    {
        public static (string, object) GetObjectWithName(PlayerState playerState) => ("player", playerState);
    }

    public class CategoryConfig
    {
        public class Entry
        {
            public string Word { get; }
            public IReadOnlyList<string> Corrections { get; }

            public Entry(string word, IEnumerable<string> corrections)
            {
                Word = word;
                Corrections = corrections.ToList();
            }
        }

        public string Name { get; }
        public IReadOnlyList<Entry> Words { get; }

        public CategoryConfig(string name, IEnumerable<Entry> words)
        {
            Name = name;
            Words = words.ToList();
        }
    }

    public class LevelConfig
    {
        public uint Level { get; private set; }
        public RunnableExpression<uint> RequiredXP { get; set; }

        public uint GetRequiredXP(PlayerState playerState) => RequiredXP.Evaluate(this, ExpressionUtil.GetObjectWithName(playerState));
    }

    public class ConfigData : ICloneable
    {
        [JsonIgnore]
        public List<CategoryConfig> Categories { get; set; } //?? move out of config and into a separate database table: category_name text, category_words map<text, set<text>>

        public List<LevelConfig> PlayerLevels { get; set; }

        [JsonIgnore]
        public int Version { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class ReadOnlyConfigData
    {
        public IReadOnlyList<FLGameLogic.WordCategory> CategoriesAsGameLogicFormat { get; }
        public IReadOnlyDictionary<string, FLGameLogic.WordCategory> CategoriesByName { get; }

        public IReadOnlyDictionary<uint, LevelConfig> PlayerLevels { get; }


        public int Version => data.Version;


        ConfigData data;


        public ReadOnlyConfigData(ConfigData data)
        {
            this.data = data;

            CategoriesAsGameLogicFormat = data.Categories.Select(c => new FLGameLogic.WordCategory(c.Name, c.Words.ToDictionary(w => w.Word, w => w.Corrections.AsEnumerable()))).ToList();
            CategoriesByName = CategoriesAsGameLogicFormat.ToDictionary(c => c.CategoryName);

            PlayerLevels = data.PlayerLevels.ToDictionary(l => l.Level);
        }
    }


    public interface ISystemConfig : IGrainWithIntegerKey
    {
        Task UpdateConfigFromDatabase();

        Task<Immutable<ConfigData>> GetConfig();

        Task UploadConfig(string jsonConfig);
    }
}
