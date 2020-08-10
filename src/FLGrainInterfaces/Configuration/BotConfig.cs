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
            Validation.CheckNotEqual(ID, 0, "Bot ID");
            Validation.CheckString(Name, $"bot {ID} name");
            Validation.CheckNotEqual(Level, 0u, $"bot {ID} level");
            Validation.CheckNotEqual(AvatarEyes, 0, $"bot {ID} avatar eyes");
            Validation.CheckNotEqual(AvatarGlasses, null, $"bot {ID} avatar head shape");
            Validation.CheckNotEqual(AvatarGlasses!.Value, 0, $"bot {ID} avatar glasses");
            Validation.CheckNotEqual(AvatarHair, null, $"bot {ID} avatar head shape");
            Validation.CheckNotEqual(AvatarHair!.Value, 0, $"bot {ID} avatar hair");
            Validation.CheckNotEqual(AvatarHeadShape, 0, $"bot {ID} avatar head shape");
            Validation.CheckNotEqual(AvatarMouth, 0, $"bot {ID} avatar mouth");
        }
    }
}
