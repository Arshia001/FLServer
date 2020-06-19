using System;
using System.Collections.Generic;
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

        public void Validate()
        {
            Validation.CheckNotEqual(HeadShape, null, "initial avatar head shape");
            Validation.CheckNotEqual(Eyes, null, "initial avatar eyes");
            Validation.CheckNotEqual(Mouth, null, "initial avatar mouth");
        }
    }
}
