using System;
using UnityEngine;

namespace BaseGame.Calendar
{
    /// <summary>
    /// What a save stores. GameClock keeps its live state in one of these, so a save always holds every state field.
    /// Speed, pause sources and fast-forward are left out on purpose: they belong to the session, not the world.
    /// </summary>
    [Serializable]
    public struct CalendarSnapshot : IEquatable<CalendarSnapshot>
    {
        [SerializeField] private long _totalMinutes;
        [SerializeField] private int _year;
        [SerializeField] private int _seasonIndex;
        [SerializeField] private long _seasonStartDay;
        [SerializeField] private bool _seasonAdvancePending;

        public CalendarSnapshot(long totalMinutes, int year, int seasonIndex, long seasonStartDay, bool seasonAdvancePending)
        {
            _totalMinutes = totalMinutes;
            _year = year;
            _seasonIndex = seasonIndex;
            _seasonStartDay = seasonStartDay;
            _seasonAdvancePending = seasonAdvancePending;
        }

        /// <summary>Minutes since the start of Day 0; always a multiple of the step.</summary>
        public long TotalMinutes { get => _totalMinutes; internal set => _totalMinutes = value; }
        public int Year { get => _year; internal set => _year = value; }
        public int SeasonIndex { get => _seasonIndex; internal set => _seasonIndex = value; }
        /// <summary>The absolute day the current season started.</summary>
        public long SeasonStartDay { get => _seasonStartDay; internal set => _seasonStartDay = value; }
        /// <summary>AdvanceSeason() was called; the season changes at the next day boundary.</summary>
        public bool SeasonAdvancePending { get => _seasonAdvancePending; internal set => _seasonAdvancePending = value; }

        public bool Equals(CalendarSnapshot other) =>
            _totalMinutes == other._totalMinutes && _year == other._year && _seasonIndex == other._seasonIndex &&
            _seasonStartDay == other._seasonStartDay && _seasonAdvancePending == other._seasonAdvancePending;

        public override bool Equals(object obj) => obj is CalendarSnapshot other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_totalMinutes, _year, _seasonIndex, _seasonStartDay, _seasonAdvancePending);

        public override string ToString() =>
            $"total {_totalMinutes}, Y{_year}, S{_seasonIndex}, season start day {_seasonStartDay}, pending {_seasonAdvancePending}";
    }
}
