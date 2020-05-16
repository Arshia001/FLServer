using System;
using System.Collections.Generic;
using System.Text;
using Bond;
using FLGrainInterfaces.Configuration;

namespace FLGrainInterfaces.Utility
{
    [Schema]
    public class VideoAdLimitTrackerState
    {
        [Id(0)]
        public TimeBasedValueState<uint, Unit>? NumberWatchedTodayState { get; set; }

        [Id(1)]
        public DateTime? LastAdWatchedTime { get; set; }
    }

    public class VideoAdLimitTrackerInfo
    {
        public VideoAdLimitTrackerInfo(uint numberWatchedToday, DateTime? lastAdWatchedTime)
        {
            NumberWatchedToday = numberWatchedToday;
            LastAdWatchedTime = lastAdWatchedTime;
        }

        public uint NumberWatchedToday { get; }

        public DateTime? LastAdWatchedTime { get; }
    }

    public class VideoAdLimitTracker
    {
        private readonly Func<VideoAdLimitConfig> getConfig;
        readonly DailyResetValue<uint> numberWatchedToday;
        DateTime? lastWatchedTime;

        public VideoAdLimitTracker(Func<VideoAdLimitConfig> getConfig)
        {
            numberWatchedToday = new DailyResetValue<uint>(0);
            this.getConfig = getConfig;
        }

        public VideoAdLimitTrackerState Serialize() =>
            new VideoAdLimitTrackerState
            {
                NumberWatchedTodayState = numberWatchedToday.Serialize(),
                LastAdWatchedTime = lastWatchedTime
            };

        public VideoAdLimitTrackerInfo GetInfo() =>
            new VideoAdLimitTrackerInfo(
                numberWatchedToday.UpdateAndGetValue(DateTime.Now),
                lastWatchedTime
                );

        public void Deserialize(VideoAdLimitTrackerState state)
        {
            if (state.NumberWatchedTodayState != null)
                numberWatchedToday.Deserialize(state.NumberWatchedTodayState);

            lastWatchedTime = state.LastAdWatchedTime;
        }

        public bool GetCanWatchAndIncrement()
        {
            var now = DateTime.Now;
            var config = getConfig();

            if (config.Interval.HasValue && 
                config.Interval > TimeSpan.Zero && 
                lastWatchedTime.HasValue && 
                now - lastWatchedTime.Value < config.Interval)
                return false;

            var adsWatched = numberWatchedToday.UpdateAndGetValue(now);

            if (config.NumberAllowedPerDay.HasValue && config.NumberAllowedPerDay > 0 && adsWatched >= config.NumberAllowedPerDay)
                return false;

            numberWatchedToday.SetValue(adsWatched + 1, now);

            lastWatchedTime = now;

            return true;
        }

        public TimeSpan GetCoolDownTimeRemaining()
        {
            var now = DateTime.Now;
            var config = getConfig();

            if (config.Interval.HasValue &&
                config.Interval > TimeSpan.Zero &&
                lastWatchedTime.HasValue)
            {
                var elapsed = now - lastWatchedTime.Value;
                return elapsed >= config.Interval.Value ? TimeSpan.Zero : config.Interval.Value - elapsed;
            }
            else
                return TimeSpan.Zero;
        }
    }
}
