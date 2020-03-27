using System;
using System.Collections.Generic;
using System.Text;
using Bond;

namespace FLGrainInterfaces.Utility
{
    public class DailyResetValue<T> : TimeBasedValueBase<T, Unit>
        where T : struct
    {
        public T StartValue { get; private set; }

        public DailyResetValue(T startValue) : base(startValue) =>
            StartValue = startValue;

        protected override T RefreshValue(DateTime now, DateTime lastRefreshTime) =>
            lastRefreshTime.Date < now.Date ? StartValue : LastValue;

        public void SetValue(T value, DateTime now) => base.UpdateValue(value, now);

        protected override Unit GetAdditionalData() => Unit.Value;

        protected override void LoadAdditionalData(Unit additionalData) { }
    }
}
