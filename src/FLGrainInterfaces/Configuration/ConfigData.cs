using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FLGrainInterfaces.Configuration
{
    public class ConfigData : ICloneable
    {
        [JsonIgnore]
        public List<GroupConfig>? Groups { get; set; }

        [JsonIgnore]
        public List<CategoryConfig>? Categories { get; set; }

        [JsonIgnore]
        public List<RenamedCategoryConfig>? RenamedCategories { get; set; }

        public List<LevelConfig>? PlayerLevels { get; set; }

        public List<GoldPackConfig>? GoldPacks { get; set; }

        public EditDistanceConfig? EditDistanceConfig { get; set; }

        public ConfigValues? ConfigValues { get; set; }

        [JsonIgnore]
        public int Version { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
