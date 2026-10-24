using System;

namespace GuenBanKhai.Calendar
{
    /// <summary>One moment with its derived values. Only GameClock creates these, so the values always agree.</summary>
    public readonly struct CalendarDate : IEquatable<CalendarDate>
    {
        public const int MinutesPerDay = 1440;

        internal CalendarDate(long totalMinutes, int year, int seasonIndex, long dayInSeason, int clockMinute, int dayOfWeek)
        {
            TotalMinutes = totalMinutes;
            Year = year;
            SeasonIndex = seasonIndex;
            DayInSeason = dayInSeason;
            ClockMinute = clockMinute;
            DayOfWeek = dayOfWeek;
        }

        /// <summary>Minutes since the start of Day 0.</summary>
        public long TotalMinutes { get; }
        public int Year { get; }
        public int SeasonIndex { get; }
        /// <summary>0-based day within the current season.</summary>
        public long DayInSeason { get; }
        /// <summary>Displayed time of day in minutes, [0, 1440).</summary>
        public int ClockMinute { get; }
        /// <summary>0-based weekday; GameClock.GetDayName turns it into a name.</summary>
        public int DayOfWeek { get; }

        /// <summary>Absolute day since Day 0.</summary>
        public long Day => TotalMinutes / MinutesPerDay;
        public int Hour => ClockMinute / 60;
        public int Minute => ClockMinute % 60;

        public bool Equals(CalendarDate other) =>
            TotalMinutes == other.TotalMinutes && Year == other.Year && SeasonIndex == other.SeasonIndex &&
            DayInSeason == other.DayInSeason && ClockMinute == other.ClockMinute && DayOfWeek == other.DayOfWeek;

        public override bool Equals(object obj) => obj is CalendarDate other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(TotalMinutes, Year, SeasonIndex, DayInSeason, ClockMinute, DayOfWeek);

        public override string ToString() =>
            $"Y{Year} S{SeasonIndex} D{DayInSeason} {Hour:00}:{Minute:00} (day {Day}, total {TotalMinutes})";
    }
}
