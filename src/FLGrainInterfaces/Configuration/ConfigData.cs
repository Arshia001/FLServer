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

        [JsonIgnore]
        public uint LatestClientVersion { get; set; }

        [JsonIgnore]
        public uint LastCompatibleClientVersion { get; set; }

        [JsonIgnore]
        public AvatarConfig? AvatarConfig { get; set; }

        [JsonIgnore]
        public List<BotConfig>? Bots { get; set; }

        public List<LevelConfig>? PlayerLevels { get; set; }

        public List<GoldPackConfig>? GoldPacks { get; set; }

        public EditDistanceConfig? EditDistanceConfig { get; set; }

        public ConfigValues? ConfigValues { get; set; }

        public InitialAvatarConfig? InitialAvatar { get; set; }

        [JsonIgnore]
        public int Version { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
