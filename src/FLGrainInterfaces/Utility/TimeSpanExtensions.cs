using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Abs(this TimeSpan timeSpan) =>
            timeSpan >= TimeSpan.Zero ? timeSpan : -timeSpan;
    }
}
