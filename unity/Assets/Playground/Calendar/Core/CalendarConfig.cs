using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("BaseGame.Playground.Calendar.Tests")]

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// Calendar settings only. Holds no runtime state; GameClock copies and validates these values once.
    /// </summary>
    [CreateAssetMenu(fileName = "CalendarConfig", menuName = "BaseGame/Playground/Calendar Config")]
    public class CalendarConfig : ScriptableObject
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

        public float SecondsPerGameMinute { get => _secondsPerGameMinute; internal set => _secondsPerGameMinute = value; }
        public int MinutesPerStep { get => _minutesPerStep; internal set => _minutesPerStep = value; }
        public int DayStartMinute { get => _dayStartMinute; internal set => _dayStartMinute = value; }
        public IReadOnlyList<string> DayNames { get => _dayNames; internal set => _dayNames = ToArray(value); }
        public IReadOnlyList<SeasonDefinition> Seasons { get => _seasons; internal set => _seasons = ToArray(value); }
        public int StartYear { get => _startYear; internal set => _startYear = value; }
        public int StartSeasonIndex { get => _startSeasonIndex; internal set => _startSeasonIndex = value; }
        public int StartMinutesIntoDay { get => _startMinutesIntoDay; internal set => _startMinutesIntoDay = value; }
        public float MaxSpeed { get => _maxSpeed; internal set => _maxSpeed = value; }
        public int MaxStepsPerFrame { get => _maxStepsPerFrame; internal set => _maxStepsPerFrame = value; }

        /// <summary>Finds the first invalid setting, if any.</summary>
        internal bool TryGetError(out string error)
        {
            error = FindError();
            return error != null;
        }

        private void OnValidate()
        {
            if (TryGetError(out string error))
            {
                Debug.LogWarning($"{name}: {error}", this);
            }
        }

        private string FindError()
        {
            if (_secondsPerGameMinute <= 0f)
            {
                return "SecondsPerGameMinute must be greater than 0.";
            }

            if (_minutesPerStep <= 0 || 60 % _minutesPerStep != 0)
            {
                return "MinutesPerStep must be a positive divisor of 60.";
            }

            if (_dayStartMinute < 0 || _dayStartMinute >= CalendarDate.MinutesPerDay || _dayStartMinute % _minutesPerStep != 0)
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

            if (_startMinutesIntoDay < 0 || _startMinutesIntoDay >= CalendarDate.MinutesPerDay || _startMinutesIntoDay % _minutesPerStep != 0)
            {
                return "StartMinutesIntoDay must be in [0, 1440) and a multiple of MinutesPerStep.";
            }

            if (_maxSpeed <= 0f)
            {
                return "MaxSpeed must be greater than 0.";
            }

            if (_maxStepsPerFrame <= 0)
            {
                return "MaxStepsPerFrame must be greater than 0.";
            }

            return null;
        }

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
