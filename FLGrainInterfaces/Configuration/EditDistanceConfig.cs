using FLGrainInterfaces.Util;

namespace FLGrainInterfaces.Configuration
{
    public class EditDistanceConfig
    {
        public RunnableExpression<byte> MaxDistanceToCorrectByLetterCount { get; private set; }

        public byte GetMaxDistanceToCorrectByLetterCount(int letterCount) =>
            MaxDistanceToCorrectByLetterCount.Evaluate(null, new[] { ("letterCount", (object)letterCount) });
    }
}
