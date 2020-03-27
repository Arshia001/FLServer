using System;
using System.Collections.Generic;
using System.Text;
using Bond;

namespace FLGrainInterfaces.Utility
{
    [Schema]
    public class TimeBasedValueState<TValue, TAdditionalData>
        where TValue : struct
        where TAdditionalData : struct
    {
        public TimeBasedValueState() { }

        public TimeBasedValueState(DateTime lastRefreshTime, TValue lastValue, TAdditionalData additionalData)
        {
            LastRefreshTime = lastRefreshTime;
            LastValue = lastValue;
            AdditionalData = additionalData;
        }

        [Id(0)]
        public DateTime LastRefreshTime { get; set; }

        [Id(1)]
        public TValue LastValue { get; set; }

        [Id(2)]
        public TAdditionalData AdditionalData { get; set; }
    }

    public abstract class TimeBasedValueBase<T, TAdditionalData>
        where T : struct
        where TAdditionalData : struct
    {
        protected TimeBasedValueBase(T initialValue) => LastValue = initialValue;

        public DateTime LastRefreshTime { get; private set; }

        public T LastValue { get; private set; }

        public T UpdateAndGetValue(DateTime now)
        {
            if (now < LastRefreshTime)
                throw new ArgumentOutOfRangeException(nameof(now), "Provided value is before last refresh time");

            LastValue = RefreshValue(now, LastRefreshTime);
            LastRefreshTime = now;

            return LastValue;
        }

        protected void UpdateValue(T value, DateTime now)
        {
            LastValue = value;
            LastRefreshTime = now;
        }

        public TimeBasedValueState<T, TAdditionalData> Serialize()
        {
            var additionalData = GetAdditionalData();
            return new TimeBasedValueState<T, TAdditionalData>(LastRefreshTime, LastValue, additionalData);
        }

        public void Deserialize(TimeBasedValueState<T, TAdditionalData> state)
        {
            LastRefreshTime = state.LastRefreshTime;
            LastValue = state.LastValue;
            LoadAdditionalData(state.AdditionalData);
        }

        protected abstract T RefreshValue(DateTime now, DateTime lastRefreshTime);

        protected abstract TAdditionalData GetAdditionalData();

        protected abstract void LoadAdditionalData(TAdditionalData additionalData);
    }
}
