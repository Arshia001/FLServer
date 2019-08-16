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

    public class GroupConfig
    {
        public ushort ID { get; }
        public string Name { get; }

        public GroupConfig(ushort id, string name)
        {
            ID = id;
            Name = name;
        }
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
        public GroupConfig Group { get; }

        public CategoryConfig(string name, IEnumerable<Entry> words, GroupConfig group)
        {
            Name = name;
            Words = words.ToList();
            Group = group;
        }
    }

    public class LevelConfig
    {
        public uint Level { get; private set; }
        public RunnableExpression<uint> RequiredXP { get; private set; }

        public uint GetRequiredXP(PlayerState playerState) => RequiredXP.Evaluate(this, ExpressionUtil.GetObjectWithName(playerState));
    }

    public class EditDistanceConfig
    {
        public RunnableExpression<byte> MaxDistanceToCorrectByLetterCount { get; private set; }

        public byte GetMaxDistanceToCorrectByLetterCount(int letterCount) =>
            MaxDistanceToCorrectByLetterCount.Evaluate(null, new[] { ("letterCount", (object)letterCount) });
    }

    public class ConfigValues
    {
        public byte NumRoundsPerGame { get; private set; }
        public byte NumCategoryChoices { get; private set; }

        public byte NumRoundsToWinToGetReward { get; private set; }
        public TimeSpan RoundWinRewardInterval { get; private set; }
        public uint NumGoldRewardForWinningRounds { get; private set; }

        public int WinDeltaScore { get; private set; }
        public int LossDeltaScore { get; private set; }
        public int DrawDeltaScore { get; private set; }
    }

    public class ConfigData : ICloneable
    {
        [JsonIgnore]
        public List<GroupConfig> Groups { get; set; }

        [JsonIgnore]
        public List<CategoryConfig> Categories { get; set; }

        public List<LevelConfig> PlayerLevels { get; set; }

        public EditDistanceConfig EditDistanceConfig { get; set; }

        public ConfigValues ConfigValues { get; set; }

        [JsonIgnore]
        public int Version { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class ReadOnlyConfigData
    {
        public IReadOnlyList<GroupConfig> Groups => data.Groups;
        public IReadOnlyDictionary<ushort, GroupConfig> GroupsByID { get; }

        public IReadOnlyDictionary<string, CategoryConfig> CategoriesByName { get; }
        public IReadOnlyDictionary<ushort, IReadOnlyList<string>> CategoryNamesByGroupID { get; }
        public IReadOnlyDictionary<string, FLGameLogic.WordCategory> CategoriesAsGameLogicFormatByName { get; }

        public IReadOnlyDictionary<uint, LevelConfig> PlayerLevels { get; }

        public IReadOnlyList<byte> MaxEditDistanceToCorrentByLetterCount { get; }

        public ConfigValues ConfigValues => data.ConfigValues;


        public int Version => data.Version;


        ConfigData data;


        public ReadOnlyConfigData(ConfigData data)
        {
            this.data = data;

            GroupsByID = data.Groups.ToDictionary(g => g.ID);

            CategoriesByName = data.Categories.ToDictionary(c => c.Name);
            CategoryNamesByGroupID = data.Categories.GroupBy(c => c.Group.ID).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.ToList());

            CategoriesAsGameLogicFormatByName = data.Categories.ToDictionary(c => c.Name,
                c => new FLGameLogic.WordCategory(c.Name, c.Words.ToDictionary(w => w.Word, w => w.Corrections.AsEnumerable())));

            PlayerLevels = data.PlayerLevels.ToDictionary(l => l.Level);

            MaxEditDistanceToCorrentByLetterCount = Enumerable.Range(0, 100).Select(i => data.EditDistanceConfig.GetMaxDistanceToCorrectByLetterCount(i)).ToList();
        }
    }


    public interface ISystemConfig : IGrainWithIntegerKey
    {
        Task UpdateConfigFromDatabase();

        Task<Immutable<ConfigData>> GetConfig();

        Task UploadConfig(string jsonConfig);
    }
}
