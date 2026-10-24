using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GuenBanKhai.Calendar.Tests
{
    public class GameClockTimeTests
    {
        [Test]
        public void Tick_MovesOnlyInWholeSteps_AndKeepsTheRemainder()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));

            clock.Tick(25f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);
            Assert.AreEqual(25.0, clock.ExactMinutes, 1e-6);

            clock.Tick(5f);
            Assert.AreEqual(30, clock.Now.TotalMinutes);
        }

        [Test]
        public void Tick_RaisesSteppedOncePerStep_WithNowAtThatStep()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            var seen = new List<(long Date, long Now, long Delta)>();
            clock.Stepped += info => seen.Add((info.Date.TotalMinutes, clock.Now.TotalMinutes, info.DeltaMinutes));

            clock.Tick(30f);

            CollectionAssert.AreEqual(new[] { (10L, 10L, 10L), (20L, 20L, 10L), (30L, 30L, 10L) }, seen);
        }

        [TestCase(1)]
        [TestCase(10)]
        public void DayAndHour_FollowDayStartMinute(int step)
        {
            // Day starts at 06:00: the clock shows 06:00 at TotalMinutes 0 and the day changes at the next 06:00.
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: step, dayStartMinute: 360));
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
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(1), weekLength: 1));
            clock.Tick(1439f);
            var order = new List<string>();
            clock.Stepped += info => order.Add($"Step {clock.Now.Year}");
            clock.HourChanged += date => order.Add($"Hour {date.Year}");
            clock.DayChanged += date => order.Add($"Day {date.Year}");
            clock.WeekChanged += date => order.Add($"Week {date.Year}");
            clock.SeasonChanged += date => order.Add($"Season {date.Year}");
            clock.YearChanged += date => order.Add($"Year {date.Year}");

            clock.Tick(1f);

            CollectionAssert.AreEqual(new[] { "Step 2", "Hour 2", "Day 2", "Week 2", "Season 2", "Year 2" }, order);
        }

        [Test]
        public void ManyStepsInOneTick_RaiseEachStepsEventsInOrder_WithNowAtThatStep()
        {
            // 60-minute steps, one-day seasons and weeks: 25 steps cross one day, week, season and year boundary.
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 60, seasons: TestCalendar.Seasons(1), weekLength: 1));
            var events = new List<string>();
            void Record(string name, CalendarDate date) => events.Add($"{name} {date.TotalMinutes} now {clock.Now.TotalMinutes}");
            clock.Stepped += info => Record("Stepped", info.Date);
            clock.HourChanged += date => Record("Hour", date);
            clock.DayChanged += date => Record("Day", date);
            clock.WeekChanged += date => Record("Week", date);
            clock.SeasonChanged += date => Record("Season", date);
            clock.YearChanged += date => Record("Year", date);

            clock.Tick(TestCalendar.Minutes(1, 1));

            var expected = new List<string>();
            for (long minute = 60; minute <= 1500; minute += 60)
            {
                expected.Add($"Stepped {minute} now {minute}");
                expected.Add($"Hour {minute} now {minute}");
                if (minute == 1440)
                {
                    expected.Add("Day 1440 now 1440");
                    expected.Add("Week 1440 now 1440");
                    expected.Add("Season 1440 now 1440");
                    expected.Add("Year 1440 now 1440");
                }
            }

            CollectionAssert.AreEqual(expected, events);
        }

        [Test]
        public void SetSpeed_ScalesTime_UpToMaxSpeed()
        {
            var clock = new GameClock(TestCalendar.Settings(maxSpeed: 5f));

            clock.SetSpeed(2f);
            clock.Tick(10f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);

            clock.SetSpeed(5f);
            Assert.AreEqual(5f, clock.Speed);
            Assert.AreEqual(5f, clock.MaxSpeed);

            clock.SetSpeed(0f);
            clock.Tick(10f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(5.5f)]
        [TestCase(float.PositiveInfinity)]
        public void SetSpeed_OutOfRange_Throws_AndKeepsTheSpeed(float speed)
        {
            var clock = new GameClock(TestCalendar.Settings(maxSpeed: 5f));
            clock.SetSpeed(2f);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed(speed));
            Assert.AreEqual(2f, clock.Speed);
        }

        [Test]
        public void SetSpeed_AboveMaxSpeed_SaysWhatMaxSpeedIs()
        {
            var clock = new GameClock(TestCalendar.Settings(maxSpeed: 5f));

            var error = Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed(50f));

            StringAssert.Contains("MaxSpeed (5)", error.Message);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Tick_NegativeOrNonFiniteDelta_Throws(float delta)
        {
            var clock = new GameClock(TestCalendar.Settings());

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Tick(delta));
            Assert.AreEqual(0.0, clock.ExactMinutes, 1e-6);
        }

        [Test]
        public void Pause_KeepsClockStopped_UntilEverySourceResumes()
        {
            var clock = new GameClock(TestCalendar.Settings());
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
            var clock = new GameClock(TestCalendar.Settings());

            Assert.Throws<ArgumentNullException>(() => clock.Pause(null));
            Assert.Throws<ArgumentNullException>(() => clock.Resume(null));
        }

        [Test]
        public void NextOccurrence_ReturnsTheNextTimeTheClockShowsThatMinute()
        {
            // Day starts at 06:00, so the clock shows 06:00 at TotalMinutes 0.
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10, dayStartMinute: 360));

            Assert.AreEqual(1440, clock.NextOccurrence(360), "Exactly at 06:00 means tomorrow's 06:00.");
            Assert.AreEqual(60, clock.NextOccurrence(420));
            Assert.AreEqual(1080, clock.NextOccurrence(0));
        }

        [Test]
        public void NextOccurrence_InvalidMinute_Throws()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.NextOccurrence(-10));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.NextOccurrence(1440));
            Assert.Throws<ArgumentException>(() => clock.NextOccurrence(365));
        }

        [TestCase(1)]
        [TestCase(10)]
        public void Tick_ProcessesAtMostMaxStepsPerFrame_AndDropsTheRest(int step)
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: step, maxStepsPerFrame: 5));

            clock.Tick(1000f);

            Assert.AreEqual(5 * step, clock.Now.TotalMinutes);
            Assert.AreEqual(5 * step, clock.ExactMinutes, 1e-6);
        }
    }
}
