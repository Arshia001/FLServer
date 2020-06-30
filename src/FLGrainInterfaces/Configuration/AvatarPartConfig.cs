using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class TypelessAvatarPartConfig
    {
        public TypelessAvatarPartConfig(ushort id, uint price, ushort minimumLevel)
        {
            ID = id;
            Price = price;
            MinimumLevel = minimumLevel;
        }

        public ushort ID { get; }
        public uint Price { get; }
        public ushort MinimumLevel { get; }
    }

    public class AvatarPartConfig
    {
        public AvatarPartConfig(ushort id, uint price, ushort minimumLevel, AvatarPartType type)
        {
            ID = id;
            Price = price;
            Type = type;
            MinimumLevel = minimumLevel;
        }

        public AvatarPartConfig(TypelessAvatarPartConfig jsonConfig, AvatarPartType type)
            : this(jsonConfig.ID, jsonConfig.Price, jsonConfig.MinimumLevel, type) { }

        public ushort ID { get; }
        public uint Price { get; }
        public ushort MinimumLevel { get; }
        public AvatarPartType Type { get; }
    }
}
