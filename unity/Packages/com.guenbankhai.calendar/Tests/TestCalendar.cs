using System.Collections.Generic;

namespace GuenBanKhai.Calendar.Tests
{
    /// <summary>
    /// Builds settings for tests: new GameClock(TestCalendar.Settings(...)). Defaults: 1 real second per game minute,
    /// 1-minute steps, day starts at 00:00, 7-day week, four 28-day seasons, game starts at Year 1, season 0, 00:00.
    /// </summary>
    internal static class TestCalendar
    {
        public static CalendarSettings Settings(
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

            return new CalendarSettings
            {
                SecondsPerGameMinute = 1f,
                MinutesPerStep = minutesPerStep,
                DayStartMinute = dayStartMinute,
                DayNames = dayNames,
                Seasons = seasons ?? Seasons(28, 28, 28, 28),
                StartYear = 1,
                StartSeasonIndex = 0,
                StartMinutesIntoDay = startMinutesIntoDay,
                MaxSpeed = maxSpeed,
                MaxStepsPerFrame = maxStepsPerFrame,
            };
        }

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

        /// <summary>Records every event except Restored and FastForwardCompleted as "Name TotalMinutes".</summary>
        public static List<string> RecordAll(GameClock clock)
        {
            var events = new List<string>();
            clock.Stepped += info => events.Add($"Stepped {info.Date.TotalMinutes}");
            clock.HourChanged += date => events.Add($"Hour {date.TotalMinutes}");
            clock.DayChanged += date => events.Add($"Day {date.TotalMinutes}");
            clock.WeekChanged += date => events.Add($"Week {date.TotalMinutes}");
            clock.SeasonChanged += date => events.Add($"Season {date.TotalMinutes}");
            clock.YearChanged += date => events.Add($"Year {date.TotalMinutes}");
            clock.Skipped += (from, to) => events.Add($"Skipped {from.TotalMinutes}->{to.TotalMinutes}");
            return events;
        }
    }
}
