namespace FLGrainInterfaces.Configuration
{
    static class ExpressionUtil
    {
        public static (string, object) GetObjectWithName(PlayerState playerState) => ("player", playerState);
    }
}
