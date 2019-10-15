using System;
using System.Collections.Generic;

namespace Jira.FlowCharts.Simulation
{
    public class Event : IComparable<Event>, IComparable
    {
        public double Time { get; }
        public double StartTime { get; }
        public EventValue Value { get; }

        public Event(double time, double startTime, EventValue value)
        {
            Time = time;
            StartTime = startTime;
            Value = value;
        }

        #region Comparisons

        public int CompareTo(Event other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Time.CompareTo(other.Time);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is Event other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Event)}");
        }

        public static bool operator <(Event left, Event right)
        {
            return Comparer<Event>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(Event left, Event right)
        {
            return Comparer<Event>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(Event left, Event right)
        {
            return Comparer<Event>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(Event left, Event right)
        {
            return Comparer<Event>.Default.Compare(left, right) >= 0;
        }

        #endregion
    }
}
