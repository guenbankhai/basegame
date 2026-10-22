using System;
using UnityEngine;

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// The saved calendar state. Speed, pause sources, sub-step time and fast-forward are not saved.
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

        public long TotalMinutes => _totalMinutes;
        public int Year => _year;
        public int SeasonIndex => _seasonIndex;
        public long SeasonStartDay => _seasonStartDay;
        public bool SeasonAdvancePending => _seasonAdvancePending;

        public bool Equals(CalendarSnapshot other) =>
            _totalMinutes == other._totalMinutes && _year == other._year && _seasonIndex == other._seasonIndex &&
            _seasonStartDay == other._seasonStartDay && _seasonAdvancePending == other._seasonAdvancePending;

        public override bool Equals(object obj) => obj is CalendarSnapshot other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_totalMinutes, _year, _seasonIndex, _seasonStartDay, _seasonAdvancePending);

        public override string ToString() =>
            $"total {_totalMinutes}, Y{_year}, S{_seasonIndex}, season start day {_seasonStartDay}, pending {_seasonAdvancePending}";
    }
}
