using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class AvatarConfig
    {
        public AvatarConfig(List<TypelessAvatarPartConfig> headShapes, List<TypelessAvatarPartConfig> hairs,
            List<TypelessAvatarPartConfig> eyes, List<TypelessAvatarPartConfig> mouths,
            List<TypelessAvatarPartConfig> glasses)
        {
            HeadShapes = headShapes;
            Hairs = hairs;
            Eyes = eyes;
            Mouths = mouths;
            Glasses = glasses;
        }

        public List<TypelessAvatarPartConfig> HeadShapes { get; private set; }
        public List<TypelessAvatarPartConfig> Hairs { get; private set; }
        public List<TypelessAvatarPartConfig> Eyes { get; private set; }
        public List<TypelessAvatarPartConfig> Mouths { get; private set; }
        public List<TypelessAvatarPartConfig> Glasses { get; private set; }

        public Dictionary<AvatarPartType, Dictionary<ushort, AvatarPartConfig>> GetIndexedData()
        {
            Dictionary<ushort, AvatarPartConfig> ToDictionary(IEnumerable<TypelessAvatarPartConfig> parts, AvatarPartType type) =>
                parts.ToDictionary(p => p.ID, p => new AvatarPartConfig(p, type));

            return new Dictionary<AvatarPartType, Dictionary<ushort, AvatarPartConfig>>()
            {
                { AvatarPartType.HeadShape, ToDictionary(HeadShapes, AvatarPartType.HeadShape) },
                { AvatarPartType.Hair, ToDictionary(Hairs, AvatarPartType.Hair) },
                { AvatarPartType.Eyes, ToDictionary(Eyes, AvatarPartType.Eyes) },
                { AvatarPartType.Mouth, ToDictionary(Mouths, AvatarPartType.Mouth) },
                { AvatarPartType.Glasses, ToDictionary(Glasses, AvatarPartType.Glasses) },
            };
        }

        public void Validate()
        {
            Validation.CheckList(HeadShapes, "avatar head shapes");
            Validation.CheckList(Hairs, "avatar hairs");
            Validation.CheckList(Eyes, "avatar eyes");
            Validation.CheckList(Mouths, "avatar mouths");
            Validation.CheckList(Glasses, "avatar glasses");
        }
    }
}
