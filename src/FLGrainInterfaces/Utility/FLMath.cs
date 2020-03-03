using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Utility
{
    public static class FLMath
    {
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float Clamp(float t, float min, float max) => t < min ? min : t > max ? max : t;

        public static float Clamp01(float t) => Clamp(t, 0, 1);

        public static float InverseLerp(float a, float b, float t) => (t - a) / (b - a);
    }
}
