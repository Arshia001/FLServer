using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class BotConfig
    {
        public BotConfig(int id, string name, uint level, ushort avatarHeadShape, ushort avatarMouth, ushort avatarEyes, ushort? avatarHair, ushort? avatarGlasses)
        {
            ID = id;
            Name = name;
            Level = level;
            AvatarHeadShape = avatarHeadShape;
            AvatarMouth = avatarMouth;
            AvatarEyes = avatarEyes;
            AvatarHair = avatarHair;
            AvatarGlasses = avatarGlasses;
        }

        public int ID { get; }
        public string Name { get; }
        public uint Level { get; }
        public ushort AvatarHeadShape { get; }
        public ushort AvatarMouth { get; }
        public ushort AvatarEyes { get; }
        public ushort? AvatarHair { get; }
        public ushort? AvatarGlasses { get; }

        public void Validate()
        {
            Validation.CheckNotDefaultStruct(ID, "Bot ID");
            Validation.CheckString(Name, $"bot {ID} name");
            Validation.CheckNotDefaultStruct(Level, $"bot {ID} level");
            Validation.CheckNotDefaultStruct(AvatarEyes, $"bot {ID} avatar eyes");
            Validation.CheckNotDefaultStruct(AvatarGlasses, $"bot {ID} avatar head shape");
            Validation.CheckNotDefaultStruct(AvatarGlasses!.Value, $"bot {ID} avatar glasses");
            Validation.CheckNotDefaultStruct(AvatarHair, $"bot {ID} avatar head shape");
            Validation.CheckNotDefaultStruct(AvatarHair!.Value, $"bot {ID} avatar hair");
            Validation.CheckNotDefaultStruct(AvatarHeadShape, $"bot {ID} avatar head shape");
            Validation.CheckNotDefaultStruct(AvatarMouth, $"bot {ID} avatar mouth");
        }
    }
}
