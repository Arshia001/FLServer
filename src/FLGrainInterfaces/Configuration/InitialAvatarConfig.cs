using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class InitialAvatarConfig
    {
        public InitialAvatarConfig(ushort? headShape, ushort? skinColor, ushort? hair, ushort? hairColor, ushort? eyes,
            ushort? mouth, ushort? beardColor, ushort? glasses)
        {
            HeadShape = headShape;
            SkinColor = skinColor;
            Hair = hair;
            HairColor = hairColor;
            Eyes = eyes;
            Mouth = mouth;
            BeardColor = beardColor;
            Glasses = glasses;
        }

        public ushort? HeadShape { get; }
        public ushort? SkinColor { get; }
        public ushort? Hair { get; }
        public ushort? HairColor { get; }
        public ushort? Eyes { get; }
        public ushort? Mouth { get; }
        public ushort? BeardColor { get; }
        public ushort? Glasses { get; }

        public void Validate()
        {
            Validation.CheckNotEqual(HeadShape, null, "initial avatar head shape");
            Validation.CheckNotEqual(SkinColor, null, "initial avatar skin color");
            Validation.CheckNotEqual(Eyes, null, "initial avatar eyes");
            Validation.CheckNotEqual(Mouth, null, "initial avatar mouth");
        }
    }
}
