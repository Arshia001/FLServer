using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public struct VideoAdLimitConfig
    {
        public TimeSpan? Interval { get; private set; }
        public uint? NumberAllowedPerDay { get; private set; }

        public void Validate(string nameInError)
        {
            Validation.CheckNotEqual(Interval, null, $"{nameInError} -> interval");
            Validation.CheckNotEqual(NumberAllowedPerDay, null, $"{nameInError} -> ads allowed per day");
        }
    }
}
