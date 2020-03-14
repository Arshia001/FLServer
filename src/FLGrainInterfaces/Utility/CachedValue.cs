using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Utility
{
    public class CachedValue<T>
    {
        T cachedValue;
        DateTime expirationTime;
        TimeSpan expirationInterval;
        Func<T> refreshValue;

        public CachedValue(Func<T> refreshValue, TimeSpan expirationInterval)
        {
            this.refreshValue = refreshValue;
            cachedValue = refreshValue();

            this.expirationInterval = expirationInterval;
            expirationTime = DateTime.Now + expirationInterval;
        }

        public T Value
        {
            get
            {
                var now = DateTime.Now;
                if (now > expirationTime )
                {
                    cachedValue = refreshValue();
                    expirationTime = now + expirationInterval;
                }

                return cachedValue;
            }
        }
    }
}
