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
            void CheckNotEqual<T>(T t, T test, string name)
            {
                if ((t == null && test == null) || (t != null && test != null && t.Equals(test)))
                    ConfigValues.FailWith($"{nameInError} -> {name} shouldn't be {test}");
            }

            CheckNotEqual(Interval, null, "interval");
            CheckNotEqual(NumberAllowedPerDay, null, "ads allowed per day");
        }
    }
}
