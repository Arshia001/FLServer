using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Validate(AvatarConfig avatarConfig)
        {
            Validation.CheckNotDefaultStruct(ID, "Bot ID");
            Validation.CheckString(Name, $"bot {ID} name");
            Validation.CheckNotDefaultStruct(Level, $"bot {ID} level");
            Validation.CheckNotDefaultStruct(AvatarEyes, $"bot {ID} avatar eyes");
            if (!avatarConfig.Eyes.Any(e => e.ID == AvatarEyes))
                Validation.FailWith($"Eye {AvatarEyes} not found for bot {ID} avatar");
            if (AvatarGlasses.HasValue)
            {
                Validation.CheckNotDefaultStruct(AvatarGlasses.Value, $"bot {ID} avatar glasses");
                if (!avatarConfig.Glasses.Any(g => g.ID == AvatarGlasses))
                    Validation.FailWith($"Glasses {AvatarGlasses} not found for bot {ID} avatar");
            }
            if (AvatarHair.HasValue)
            {
                Validation.CheckNotDefaultStruct(AvatarHair.Value, $"bot {ID} avatar hair");
                if (!avatarConfig.Hairs.Any(h => h.ID == AvatarHair))
                    Validation.FailWith($"Hair {AvatarHair} not found for bot {ID} avatar");
            }
            Validation.CheckNotDefaultStruct(AvatarHeadShape, $"bot {ID} avatar head shape");
            if (!avatarConfig.HeadShapes.Any(h => h.ID == AvatarHeadShape))
                Validation.FailWith($"Head shape {AvatarHeadShape} not found for bot {ID} avatar");
            Validation.CheckNotDefaultStruct(AvatarMouth, $"bot {ID} avatar mouth");
            if (!avatarConfig.Mouths.Any(m => m.ID == AvatarMouth))
                Validation.FailWith($"Mouth {AvatarMouth} not found for bot {ID} avatar");
        }
    }
}
