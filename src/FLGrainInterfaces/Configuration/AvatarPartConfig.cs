using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class TypelessAvatarPartConfig
    {
        public TypelessAvatarPartConfig(ushort id, uint price)
        {
            ID = id;
            Price = price;
        }

        public ushort ID { get; }
        public uint Price { get; }
    }

    public class AvatarPartConfig
    {
        public AvatarPartConfig(ushort id, uint price, AvatarPartType type)
        {
            ID = id;
            Price = price;
            Type = type;
        }

        public AvatarPartConfig(TypelessAvatarPartConfig jsonConfig, AvatarPartType type)
            : this(jsonConfig.ID, jsonConfig.Price, type) { }

        public ushort ID { get; }
        public uint Price { get; }
        public AvatarPartType Type { get; }
    }
}
