using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FLGrainInterfaces.Configuration
{
    public class ReadOnlyConfigData
    {
        public IReadOnlyList<GroupConfig> Groups => data.Groups ?? throw new Exception("Groups not specified in config");
        public IReadOnlyDictionary<ushort, GroupConfig> GroupsByID { get; }

        public IReadOnlyDictionary<string, CategoryConfig> CategoriesByName { get; }
        public IReadOnlyDictionary<ushort, IReadOnlyList<string>> CategoryNamesByGroupID { get; }
        public IReadOnlyList<FLGameLogicServer.WordCategory> CategoriesAsGameLogicFormat { get; }
        public IReadOnlyDictionary<string, FLGameLogicServer.WordCategory> CategoriesAsGameLogicFormatByName { get; }

        public IReadOnlyDictionary<uint, LevelConfig> PlayerLevels { get; }

        public IReadOnlyDictionary<string, GoldPackConfig> GoldPacks { get; }

        public IReadOnlyList<byte> MaxEditDistanceToCorrentByLetterCount { get; }

        public ConfigValues ConfigValues => data.ConfigValues ?? throw new Exception("ConfigValues not specified");


        public int Version => data.Version;


        readonly ConfigData data;


        public ReadOnlyConfigData(ConfigData data)
        {
            this.data = data;

            GroupsByID = data.Groups.ToDictionary(g => g.ID);

            CategoriesByName = data.Categories.ToDictionary(c => c.Name);
            CategoryNamesByGroupID = data.Categories.GroupBy(c => c.Group.ID).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(c => c.Name).ToList());

            CategoriesAsGameLogicFormat = data.Categories.Select(
                c => new FLGameLogicServer.WordCategory(c.Name, c.Words.ToDictionary(w => w.Word, w => w.Corrections.AsEnumerable()))).ToList();
            CategoriesAsGameLogicFormatByName = CategoriesAsGameLogicFormat.ToDictionary(c => c.CategoryName);

            PlayerLevels = data.PlayerLevels.ToDictionary(l => l.Level);

            GoldPacks = data.GoldPacks?.ToDictionary(c => c.Sku ?? throw new Exception("Null SKU not allowed")) 
                ?? throw new Exception("Groups not specified in config");

            MaxEditDistanceToCorrentByLetterCount = Enumerable.Range(0, 100)
                .Select(i => data.EditDistanceConfig?.GetMaxDistanceToCorrectByLetterCount(i) ?? throw new Exception("MaxDistanceToCorrectByLetterCount not specified"))
                .ToList();
        }

        [DoesNotReturn] static void FailWith(string error) => throw new ArgumentException(error);

        public static void Validate(ConfigData data)
        {
            static void CheckList<T>(IReadOnlyList<T>? list, string name)
            {
                if (list == null || list.Count == 0)
                    FailWith($"No {name}");
            }

            CheckList(data.Groups, "groups");
            CheckList(data.Categories, "categories");
            CheckList(data.PlayerLevels, "player levels");
            CheckList(data.GoldPacks, "gold packs");

            if (data.EditDistanceConfig == null || data.EditDistanceConfig.MaxDistanceToCorrectByLetterCount == null)
                FailWith("No edit distance config");

            if (data.ConfigValues == null)
                FailWith("No config values");

            ConfigValues.Validate(data.ConfigValues);
        }
    }
}
