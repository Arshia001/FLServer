using FLGrainInterfaces.Util;
using System;

namespace FLGrainInterfaces.Configuration
{
    public class LevelConfig
    {
        public uint Level { get; private set; }
        public RunnableNonNullExpression<uint>? RequiredXP { get; private set; }

        public uint GetRequiredXP(PlayerState playerState) => RequiredXP?.Evaluate(this, ExpressionUtil.GetObjectWithName(playerState))
            ?? throw new Exception($"Cannot get required XP for level {Level}");
    }
}
