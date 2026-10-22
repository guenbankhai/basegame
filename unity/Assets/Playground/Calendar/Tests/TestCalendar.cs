using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Playground.Calendar.Tests
{
    /// <summary>
    /// Builds configs and clocks for tests. Defaults: 1 real second per game minute, day starts at 00:00,
    /// 7-day week, four 28-day seasons, game starts at Year 1, season 0, 00:00.
    /// </summary>
    internal static class TestCalendar
    {
        public static CalendarConfig Config(
            int minutesPerStep = 1,
            int dayStartMinute = 0,
            int startMinutesIntoDay = 0,
            SeasonDefinition[] seasons = null,
            int weekLength = 7,
            float maxSpeed = 1000f,
            int maxStepsPerFrame = 100000)
        {
            var dayNames = new List<string>();
            for (int i = 0; i < weekLength; i++)
            {
                dayNames.Add("D" + i);
            }

            var config = ScriptableObject.CreateInstance<CalendarConfig>();
            config.SecondsPerGameMinute = 1f;
            config.MinutesPerStep = minutesPerStep;
            config.DayStartMinute = dayStartMinute;
            config.DayNames = dayNames;
            config.Seasons = seasons ?? Seasons(28, 28, 28, 28);
            config.StartYear = 1;
            config.StartSeasonIndex = 0;
            config.StartMinutesIntoDay = startMinutesIntoDay;
            config.MaxSpeed = maxSpeed;
            config.MaxStepsPerFrame = maxStepsPerFrame;
            return config;
        }

        public static GameClock Clock(int minutesPerStep = 1, int dayStartMinute = 0, int startMinutesIntoDay = 0, SeasonDefinition[] seasons = null) =>
            new GameClock(Config(minutesPerStep, dayStartMinute, startMinutesIntoDay, seasons));

        public static SeasonDefinition[] Seasons(params int[] lengths)
        {
            var result = new SeasonDefinition[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
            {
                result[i] = new SeasonDefinition("S" + i, lengths[i]);
            }

            return result;
        }

        public static long Minutes(long days, int hours = 0, int minutes = 0) =>
            days * CalendarDate.MinutesPerDay + hours * 60 + minutes;
    }
}
