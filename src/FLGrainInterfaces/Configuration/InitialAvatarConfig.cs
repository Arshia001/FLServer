using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class InitialAvatarConfig
    {
        public InitialAvatarConfig(ushort? headShape, ushort? hair, ushort? eyes, ushort? mouth, ushort? glasses)
        {
            HeadShape = headShape;
            Hair = hair;
            Eyes = eyes;
            Mouth = mouth;
            Glasses = glasses;
        }

        public ushort? HeadShape { get; }
        public ushort? Hair { get; }
        public ushort? Eyes { get; }
        public ushort? Mouth { get; }
        public ushort? Glasses { get; }

        public void Validate(AvatarConfig avatarConfig)
        {
            Validation.CheckNotEqual(HeadShape, null, "initial avatar head shape");
            Validation.CheckNotEqual(Eyes, null, "initial avatar eyes");
            Validation.CheckNotEqual(Mouth, null, "initial avatar mouth");

            if (avatarConfig.HeadShapes.Where(h => h.ID == HeadShape).FirstOrDefault() == null)
                Validation.FailWith("Initial head shape not found in avatar parts");
            if (avatarConfig.Eyes.Where(e => e.ID == Eyes).FirstOrDefault() == null)
                Validation.FailWith("Initial eyes not found in avatar parts");
            if (avatarConfig.Mouths.Where(m => m.ID == Mouth).FirstOrDefault() == null)
                Validation.FailWith("Initial mouth not found in avatar parts");
            if (Hair != null && avatarConfig.Hairs.Where(h => h.ID == Hair).FirstOrDefault() == null)
                Validation.FailWith("Initial hair not found in avatar parts");
            if (Glasses != null && avatarConfig.Glasses.Where(g => g.ID == Glasses).FirstOrDefault() == null)
                Validation.FailWith("Initial glasses not found in avatar parts");
        }
    }
}
