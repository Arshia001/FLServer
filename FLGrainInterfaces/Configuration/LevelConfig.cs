using FLGrainInterfaces.Util;

namespace FLGrainInterfaces.Configuration
{
    public class LevelConfig
    {
        public uint Level { get; private set; }
        public RunnableExpression<uint> RequiredXP { get; private set; }

        public uint GetRequiredXP(PlayerState playerState) => RequiredXP.Evaluate(this, ExpressionUtil.GetObjectWithName(playerState));
    }
}
