using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class TimeFrame : IComparable<TimeFrame>
    {
        static TimeSpan oneDay = TimeSpan.FromDays(1);

        public TimeFrame(TimeSpan startTime, TimeSpan endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }

        public Interval GetForDate(DateTime date) => GetForDateUnchecked(date.Date);

        Interval GetForDateUnchecked(DateTime definitelyDate)
        {
            var start = definitelyDate + StartTime;
            var end =
                EndTime > StartTime ?
                definitelyDate + EndTime :
                definitelyDate + oneDay + EndTime; // If end time < start time, the frame will extend into the next day, e.g. 23:00 -> 1:00

            return new Interval(start, end);
        }

        public void Validate(string nameInError)
        {
            if (StartTime == EndTime)
                Validation.FailWith($"{nameInError} -> Cannot have time frame with zero length (start time equal to end time)");

            if (StartTime < TimeSpan.Zero || StartTime >= oneDay)
                Validation.FailWith($"{nameInError} -> Start time must be between 00:00:00 and 23:59:59");

            if (EndTime < TimeSpan.Zero || EndTime >= oneDay)
                Validation.FailWith($"{nameInError} -> End time must be between 00:00:00 and 23:59:59");
        }

        public static Interval GetClosestInterval(DateTime time, IEnumerable<TimeFrame> sortedTimeFrames, IEnumerable<DateTime> mustBeAfter)
        {
            var date = time.Date;

            var intervals =
                sortedTimeFrames.Select(f => f.GetForDateUnchecked(date))
                .Append(sortedTimeFrames.First().GetForDate(date.AddDays(1))) // First time frame of next day
                .Append(sortedTimeFrames.Last().GetForDate(date.AddDays(-1))); // Last time frame of last day

            foreach (var t in mustBeAfter)
                intervals = intervals.Where(x => x.GetDistanceFrom(t) >= TimeSpan.Zero);

            var withDistances = intervals.Select(i => (interval: i, distance: i.GetDistanceFrom(time)));

            var bestMatch = withDistances.MinBy(i => i.distance.Abs()).interval;

            var latestStart = mustBeAfter.Append(bestMatch.Start).Max();

            if (latestStart == bestMatch.Start)
                return bestMatch;
            else
                return new Interval(latestStart, bestMatch.End);
        }

        public int CompareTo(TimeFrame other) => StartTime.CompareTo(other.StartTime);

        public class Interval
        {
            public Interval(DateTime start, DateTime end)
            {
                Start = start;
                End = end;
            }

            public DateTime Start { get; }
            public DateTime End { get; }

            /// <summary>
            /// Returns a negative result if the specified time is after this interval, zero if it falls within, or positive otherwise.
            /// </summary>
            public TimeSpan GetDistanceFrom(DateTime time)
            {
                if (time < Start)
                    return Start - time;
                else if (time <= End)
                    return TimeSpan.Zero;
                else
                    return End - time;
            }

            public DateTime GetRandomTimeInside()
            {
                var ticks = (long)((End - Start).Ticks * RandomHelper.GetDouble());
                return Start.AddTicks(ticks);
            }
        }
    }
}
