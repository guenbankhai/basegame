using System.Collections.Generic;
using NUnit.Framework;

namespace BaseGame.Playground.Calendar.Tests
{
    public class GameClockTimeTests
    {
        [Test]
        public void NewClock_StartsAtConfiguredPoint()
        {
            var config = TestCalendar.Config(dayStartMinute: 360, startMinutesIntoDay: 120);
            config.StartYear = 3;
            config.StartSeasonIndex = 2;

            var now = new GameClock(config).Now;

            Assert.AreEqual(120, now.TotalMinutes);
            Assert.AreEqual(0, now.Day);
            Assert.AreEqual(8 * 60, now.ClockMinute);
            Assert.AreEqual(3, now.Year);
            Assert.AreEqual(2, now.SeasonIndex);
            Assert.AreEqual(0, now.DayInSeason);
            Assert.AreEqual(0, now.DayOfWeek);
        }

        [TestCase(7, 0, 0, 0)]
        [TestCase(10, 5, 0, 0)]
        [TestCase(10, 0, 15, 0)]
        [TestCase(1, 1440, 0, 0)]
        [TestCase(1, 0, 0, 4)]
        public void NewClock_InvalidConfig_Throws(int step, int dayStartMinute, int startMinutesIntoDay, int startSeasonIndex)
        {
            var config = TestCalendar.Config(minutesPerStep: step, dayStartMinute: dayStartMinute, startMinutesIntoDay: startMinutesIntoDay);
            config.StartSeasonIndex = startSeasonIndex;

            Assert.Throws<System.ArgumentException>(() => new GameClock(config));
        }

        [Test]
        public void NewClock_NoSeasonsOrNoDays_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => new GameClock(TestCalendar.Config(seasons: new SeasonDefinition[0])));
            Assert.Throws<System.ArgumentException>(() => new GameClock(TestCalendar.Config(weekLength: 0)));
        }

        [Test]
        public void Tick_MovesOnlyInWholeSteps_AndKeepsTheRemainder()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);

            clock.Tick(25f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);
            Assert.AreEqual(25.0, clock.ExactMinutes, 1e-6);

            clock.Tick(5f);
            Assert.AreEqual(30, clock.Now.TotalMinutes);
        }

        [Test]
        public void Tick_RaisesTickedOncePerStep_WithNowAtThatStep()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);
            var seen = new List<(long Date, long Now, long Delta)>();
            clock.Ticked += info => seen.Add((info.Date.TotalMinutes, clock.Now.TotalMinutes, info.DeltaMinutes));

            clock.Tick(30f);

            CollectionAssert.AreEqual(new[] { (10L, 10L, 10L), (20L, 20L, 10L), (30L, 30L, 10L) }, seen);
        }

        [TestCase(1)]
        [TestCase(10)]
        public void DayAndHour_FollowDayStartMinute(int step)
        {
            // Day starts at 06:00: the clock shows 06:00 at TotalMinutes 0 and the day changes at the next 06:00.
            var clock = TestCalendar.Clock(minutesPerStep: step, dayStartMinute: 360);
            var days = new List<CalendarDate>();
            int hours = 0;
            clock.DayChanged += days.Add;
            clock.HourChanged += _ => hours++;

            clock.Tick(TestCalendar.Minutes(1));

            Assert.AreEqual(1, days.Count);
            Assert.AreEqual(1440, days[0].TotalMinutes);
            Assert.AreEqual(1, days[0].Day);
            Assert.AreEqual(360, days[0].ClockMinute);
            Assert.AreEqual(1, days[0].DayOfWeek);
            Assert.AreEqual(24, hours);
        }

        [Test]
        public void SameMinute_EventsFireInOrder_AfterStateIsUpdated()
        {
            // One-day season, one-day week: every boundary falls on minute 1440.
            var config = TestCalendar.Config(seasons: TestCalendar.Seasons(1), weekLength: 1);
            var clock = new GameClock(config);
            clock.Tick(1439f);
            var order = new List<string>();
            clock.Ticked += info => order.Add($"Tick {clock.Now.Year}");
            clock.HourChanged += date => order.Add($"Hour {date.Year}");
            clock.DayChanged += date => order.Add($"Day {date.Year}");
            clock.WeekChanged += date => order.Add($"Week {date.Year}");
            clock.SeasonChanged += date => order.Add($"Season {date.Year}");
            clock.YearChanged += date => order.Add($"Year {date.Year}");

            clock.Tick(1f);

            CollectionAssert.AreEqual(new[] { "Tick 2", "Hour 2", "Day 2", "Week 2", "Season 2", "Year 2" }, order);
        }

        [Test]
        public void SetSpeed_ScalesTime_AndIsClampedToMaxSpeed()
        {
            var clock = new GameClock(TestCalendar.Config(maxSpeed: 5f));

            clock.SetSpeed(2f);
            clock.Tick(10f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);

            clock.SetSpeed(50f);
            Assert.AreEqual(5f, clock.Speed);

            clock.SetSpeed(0f);
            clock.Tick(10f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);
        }

        [Test]
        public void SetSpeed_Negative_Throws()
        {
            var clock = TestCalendar.Clock();

            Assert.Throws<System.ArgumentOutOfRangeException>(() => clock.SetSpeed(-1f));
        }

        [Test]
        public void Tick_NegativeDelta_Throws()
        {
            var clock = TestCalendar.Clock();

            Assert.Throws<System.ArgumentOutOfRangeException>(() => clock.Tick(-1f));
        }

        [Test]
        public void Pause_KeepsClockStopped_UntilEverySourceResumes()
        {
            var clock = TestCalendar.Clock();
            object menu = new object();
            object dialog = new object();

            clock.Pause(menu);
            clock.Pause(menu);
            clock.Pause(dialog);
            clock.Tick(10f);
            Assert.AreEqual(0, clock.Now.TotalMinutes);
            Assert.AreEqual(0.0, clock.ExactMinutes, 1e-6, "Paused time does not build up either.");

            clock.Resume(menu);
            clock.Resume(new object());
            Assert.IsTrue(clock.IsPaused);

            clock.Resume(dialog);
            Assert.IsFalse(clock.IsPaused);
            clock.Tick(10f);
            Assert.AreEqual(10, clock.Now.TotalMinutes);
        }

        [Test]
        public void Pause_NullSource_Throws()
        {
            var clock = TestCalendar.Clock();

            Assert.Throws<System.ArgumentNullException>(() => clock.Pause(null));
            Assert.Throws<System.ArgumentNullException>(() => clock.Resume(null));
        }

        [Test]
        public void NextOccurrence_ReturnsTheNextTimeTheClockShowsThatMinute()
        {
            // Day starts at 06:00, so the clock shows 06:00 at TotalMinutes 0.
            var clock = TestCalendar.Clock(minutesPerStep: 10, dayStartMinute: 360);

            Assert.AreEqual(1440, clock.NextOccurrence(360), "Exactly at 06:00 means tomorrow's 06:00.");
            Assert.AreEqual(60, clock.NextOccurrence(420));
            Assert.AreEqual(1080, clock.NextOccurrence(0));
        }

        [Test]
        public void NextOccurrence_InvalidMinute_Throws()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => clock.NextOccurrence(-10));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => clock.NextOccurrence(1440));
            Assert.Throws<System.ArgumentException>(() => clock.NextOccurrence(365));
        }

        [TestCase(1)]
        [TestCase(10)]
        public void Tick_ProcessesAtMostMaxStepsPerFrame_AndDropsTheRest(int step)
        {
            var clock = new GameClock(TestCalendar.Config(minutesPerStep: step, maxStepsPerFrame: 5));

            clock.Tick(1000f);

            Assert.AreEqual(5 * step, clock.Now.TotalMinutes);
            Assert.AreEqual(5 * step, clock.ExactMinutes, 1e-6);
        }
    }
}
