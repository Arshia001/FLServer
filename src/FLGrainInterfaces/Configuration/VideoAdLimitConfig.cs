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
            Validation.CheckNotDefaultStruct(Interval, $"{nameInError} -> interval");
            Validation.CheckNotDefaultStruct(NumberAllowedPerDay, $"{nameInError} -> ads allowed per day");
        }
    }
}
