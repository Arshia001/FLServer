using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FLGrainInterfaces.Configuration
{
    public class ReadOnlyConfigData
    {
        public ReadOnlyConfigData(ConfigData data)
        {
            this.data = data;

            GroupsByID = data.Groups.ToDictionary(g => g.ID);

            CategoriesByName = data.Categories!.ToDictionary(c => c.Name);
            CategoryNamesByGroupID = data.Categories!.GroupBy(c => c.Group.ID).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(c => c.Name).ToList());

            CategoriesAsGameLogicFormat = data.Categories!.Select(
                c => new FLGameLogicServer.WordCategory(c.Name, c.Words.ToDictionary(w => w.Word, w => w.Corrections.AsEnumerable()))).ToList();
            CategoriesAsGameLogicFormatByName = CategoriesAsGameLogicFormat.ToDictionary(c => c.CategoryName);

            RenamedCategoriesByOldName = data.RenamedCategories!.ToDictionary(r => r.OldName, r => r.NewName);

            PlayerLevels = data.PlayerLevels.ToDictionary(l => l.Level);

            GoldPacks = data.GoldPacks?.ToDictionary(c => c.Sku ?? throw new Exception("Null SKU not allowed"))
                ?? throw new Exception("Groups not specified in config");

            MaxEditDistanceToCorrentByLetterCount = Enumerable.Range(0, 100)
                .Select(i => data.EditDistanceConfig?.GetMaxDistanceToCorrectByLetterCount(i) ?? throw new Exception("MaxDistanceToCorrectByLetterCount not specified"))
                .ToList();
        }

        public IReadOnlyList<GroupConfig> Groups => data.Groups ?? throw new Exception("Groups not specified in config");
        public IReadOnlyDictionary<ushort, GroupConfig> GroupsByID { get; }

        public IReadOnlyDictionary<string, CategoryConfig> CategoriesByName { get; }
        public IReadOnlyDictionary<ushort, IReadOnlyList<string>> CategoryNamesByGroupID { get; }
        public IReadOnlyList<FLGameLogicServer.WordCategory> CategoriesAsGameLogicFormat { get; }
        public IReadOnlyDictionary<string, FLGameLogicServer.WordCategory> CategoriesAsGameLogicFormatByName { get; }
        public IReadOnlyDictionary<string, string> RenamedCategoriesByOldName { get; }

        public IReadOnlyDictionary<uint, LevelConfig> PlayerLevels { get; }

        public IReadOnlyDictionary<string, GoldPackConfig> GoldPacks { get; }

        public IReadOnlyList<byte> MaxEditDistanceToCorrentByLetterCount { get; }

        public ConfigValues ConfigValues => data.ConfigValues ?? throw new Exception("ConfigValues not specified");

        public uint LatestClientVersion => data.LatestClientVersion;

        public int Version => data.Version;

        readonly ConfigData data;

        public FLGameLogicServer.WordCategory? GetCategory(string nameNewOrOld)
        {
            FLGameLogicServer.WordCategory category;

            if (CategoriesAsGameLogicFormatByName.TryGetValue(nameNewOrOld, out category))
                return category;

            // We may accidentally introduce loops into the system by undoing a rename and forgetting to
            // clean the renamed category entries. This is to prevent an infinite loop in that situation.
            var visited = new HashSet<string>();

            while (RenamedCategoriesByOldName.TryGetValue(nameNewOrOld, out var newName))
            {
                if (CategoriesAsGameLogicFormatByName.TryGetValue(newName, out category))
                    return category;

                if (visited.Contains(newName))
                    return null;

                visited.Add(newName);
                nameNewOrOld = newName;
            }

            return null;
        }

        public static void Validate(ConfigData data)
        {
            Validation.CheckList(data.Groups, "groups");
            Validation.CheckList(data.Categories, "categories");
            Validation.CheckList(data.PlayerLevels, "player levels");
            Validation.CheckList(data.GoldPacks, "gold packs");

            if (data.RenamedCategories == null)
                Validation.FailWith("No renamed categories");

            if (data.EditDistanceConfig == null || data.EditDistanceConfig.MaxDistanceToCorrectByLetterCount == null)
                Validation.FailWith("No edit distance config");

            if (data.ConfigValues == null)
                Validation.FailWith("No config values");

            try
            {
                var _unused = new ReadOnlyConfigData(data);
            }
            catch (Exception ex)
            {
                Validation.FailWith($"Failed to index config data due to:\n{ex}");
            }

            ConfigValues.Validate(data.ConfigValues);

            if (data.Groups!.Count < data.ConfigValues.NumGroupChoices)
                Validation.FailWith($"Too few groups, need at least {data.ConfigValues.NumGroupChoices}");
        }
    }
}
