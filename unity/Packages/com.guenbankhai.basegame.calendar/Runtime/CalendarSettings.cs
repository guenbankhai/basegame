using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Calendar
{
    /// <summary>
    /// Starting settings only: GameClock checks and copies them once, so a running clock changes only through its
    /// commands. Plain C# so code and tests can build them without an asset.
    /// </summary>
    [Serializable]
    public class CalendarSettings
    {
        [Tooltip("Real seconds per game minute at speed 1. 1200 / 1440 = a 20-minute real day.")]
        [SerializeField] private float _secondsPerGameMinute = 1200f / 1440f;
        [Tooltip("Game minutes per clock step. Must divide 60.")]
        [SerializeField] private int _minutesPerStep = 10;
        [Tooltip("Clock minute at which a new day starts (0 = 00:00, 360 = 06:00). Must be a multiple of the step.")]
        [SerializeField] private int _dayStartMinute;
        [Tooltip("One name per weekday; the length sets the week length.")]
        [SerializeField] private string[] _dayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        [SerializeField] private SeasonDefinition[] _seasons =
        {
            new SeasonDefinition("Spring", 28),
            new SeasonDefinition("Summer", 28),
            new SeasonDefinition("Fall", 28),
            new SeasonDefinition("Winter", 28),
        };

        [Header("New game")]
        [SerializeField] private int _startYear = 1;
        [SerializeField] private int _startSeasonIndex;
        [Tooltip("Minutes after the start of Day 0. Must be a multiple of the step.")]
        [SerializeField] private int _startMinutesIntoDay = 360;

        [Header("Limits")]
        [SerializeField] private float _maxSpeed = 100f;
        [Tooltip("Steps processed per Tick at most; the rest is dropped.")]
        [SerializeField] private int _maxStepsPerFrame = 120;

        public float SecondsPerGameMinute { get => _secondsPerGameMinute; set => _secondsPerGameMinute = value; }
        public int MinutesPerStep { get => _minutesPerStep; set => _minutesPerStep = value; }
        public int DayStartMinute { get => _dayStartMinute; set => _dayStartMinute = value; }
        public IReadOnlyList<string> DayNames { get => _dayNames; set => _dayNames = ToArray(value); }
        public IReadOnlyList<SeasonDefinition> Seasons { get => _seasons; set => _seasons = ToArray(value); }
        public int StartYear { get => _startYear; set => _startYear = value; }
        public int StartSeasonIndex { get => _startSeasonIndex; set => _startSeasonIndex = value; }
        public int StartMinutesIntoDay { get => _startMinutesIntoDay; set => _startMinutesIntoDay = value; }
        public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = value; }
        public int MaxStepsPerFrame { get => _maxStepsPerFrame; set => _maxStepsPerFrame = value; }

        internal bool TryGetError(out string error)
        {
            error = FindError();
            return error != null;
        }

        private string FindError()
        {
            if (!float.IsFinite(_secondsPerGameMinute) || _secondsPerGameMinute <= 0f)
            {
                return "SecondsPerGameMinute must be a finite number greater than 0.";
            }

            if (_minutesPerStep <= 0 || 60 % _minutesPerStep != 0)
            {
                return "MinutesPerStep must be a positive divisor of 60.";
            }

            if (!IsMinuteOfDayOnStep(_dayStartMinute))
            {
                return "DayStartMinute must be in [0, 1440) and a multiple of MinutesPerStep.";
            }

            if (_dayNames == null || _dayNames.Length == 0)
            {
                return "DayNames needs at least one day.";
            }

            if (_seasons == null || _seasons.Length == 0)
            {
                return "Seasons needs at least one season.";
            }

            foreach (var season in _seasons)
            {
                if (season.LengthInDays < 0)
                {
                    return $"Season '{season.Name}' has a negative length.";
                }
            }

            if (_startYear < 0)
            {
                return "StartYear must not be negative.";
            }

            if (_startSeasonIndex < 0 || _startSeasonIndex >= _seasons.Length)
            {
                return "StartSeasonIndex is outside Seasons.";
            }

            if (!IsMinuteOfDayOnStep(_startMinutesIntoDay))
            {
                return "StartMinutesIntoDay must be in [0, 1440) and a multiple of MinutesPerStep.";
            }

            if (!float.IsFinite(_maxSpeed) || _maxSpeed <= 0f)
            {
                return "MaxSpeed must be a finite number greater than 0.";
            }

            if (_maxStepsPerFrame <= 0)
            {
                return "MaxStepsPerFrame must be greater than 0.";
            }

            return null;
        }

        private bool IsMinuteOfDayOnStep(int minute) =>
            minute >= 0 && minute < CalendarDate.MinutesPerDay && minute % _minutesPerStep == 0;

        private static T[] ToArray<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                return null;
            }

            var result = new T[source.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = source[i];
            }

            return result;
        }
    }
}
