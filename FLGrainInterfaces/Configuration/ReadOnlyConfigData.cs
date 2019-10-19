﻿using System.Collections.Generic;
using System.Linq;

namespace FLGrainInterfaces.Configuration
{
    public class ReadOnlyConfigData
    {
        public IReadOnlyList<GroupConfig> Groups => data.Groups;
        public IReadOnlyDictionary<ushort, GroupConfig> GroupsByID { get; }

        public IReadOnlyDictionary<string, CategoryConfig> CategoriesByName { get; }
        public IReadOnlyDictionary<ushort, IReadOnlyList<string>> CategoryNamesByGroupID { get; }
        public IReadOnlyList<FLGameLogicServer.WordCategory> CategoriesAsGameLogicFormat { get; }
        public IReadOnlyDictionary<string, FLGameLogicServer.WordCategory> CategoriesAsGameLogicFormatByName { get; }

        public IReadOnlyDictionary<uint, LevelConfig> PlayerLevels { get; }

        public IReadOnlyDictionary<string, GoldPackConfig> GoldPacks { get; }

        public IReadOnlyList<byte> MaxEditDistanceToCorrentByLetterCount { get; }

        public ConfigValues ConfigValues => data.ConfigValues;


        public int Version => data.Version;


        ConfigData data;


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

            GoldPacks = data.GoldPacks.ToDictionary(c => c.Sku);

            MaxEditDistanceToCorrentByLetterCount = Enumerable.Range(0, 100).Select(i => data.EditDistanceConfig.GetMaxDistanceToCorrectByLetterCount(i)).ToList();
        }
    }
}
