using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GuenBanKhai.Calendar.Tests
{
    public class GameClockSeasonTests
    {
        [TestCase(1)]
        [TestCase(10)]
        public void FixedLengthSeason_ChangesAfterItsDays_AndWrapsIntoNextYear(int step)
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: step, seasons: TestCalendar.Seasons(2, 3)));
            var seasons = new List<long>();
            var years = new List<long>();
            clock.SeasonChanged += date => seasons.Add(date.Day);
            clock.YearChanged += date => years.Add(date.Day);

            clock.Tick(TestCalendar.Minutes(2));
            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(0, clock.Now.DayInSeason);

            clock.Tick(TestCalendar.Minutes(3));
            Assert.AreEqual(0, clock.Now.SeasonIndex);
            Assert.AreEqual(2, clock.Now.Year);

            CollectionAssert.AreEqual(new[] { 2L, 5L }, seasons);
            CollectionAssert.AreEqual(new[] { 5L }, years);
        }

        [Test]
        public void AdvanceSeason_ChangesOnlyAtNextDayBoundary_AndOnlyOnce()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(0, 0, 0)));
            clock.Tick(TestCalendar.Minutes(0, 12));

            clock.AdvanceSeason();
            clock.AdvanceSeason();
            clock.AdvanceSeason();
            Assert.AreEqual(0, clock.Now.SeasonIndex);

            clock.Tick(TestCalendar.Minutes(0, 12));
            Assert.AreEqual(1, clock.Now.SeasonIndex);

            clock.Tick(TestCalendar.Minutes(5));
            Assert.AreEqual(1, clock.Now.SeasonIndex, "A command-only season never ends by itself.");
            Assert.AreEqual(5, clock.Now.DayInSeason);
        }

        [Test]
        public void AdvanceSeason_EndsFixedLengthSeasonEarly_AndNextSeasonCountsFromThatDay()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(28, 10)));
            clock.Tick(TestCalendar.Minutes(3, 5));

            clock.AdvanceSeason();
            clock.Tick(TestCalendar.Minutes(0, 19));
            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(4, clock.Now.Day);
            Assert.AreEqual(0, clock.Now.DayInSeason);

            clock.Tick(TestCalendar.Minutes(10));
            Assert.AreEqual(0, clock.Now.SeasonIndex);
            Assert.AreEqual(14, clock.Now.Day);
        }

        [Test]
        public void Week_RunsAcrossSeasons_WithoutReset()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(3, 3)));
            int weeks = 0;
            clock.WeekChanged += _ => weeks++;

            clock.Tick(TestCalendar.Minutes(10));

            Assert.AreEqual(3, clock.Now.DayOfWeek);
            Assert.AreEqual(1, weeks);
        }
    }
}
