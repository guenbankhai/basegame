using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BaseGame.Playground.Calendar.Tests
{
    public class GameClockSkipFastForwardTests
    {
        [TestCase(1)]
        [TestCase(10)]
        public void SkipTo_JumpsAndRaisesOnlySkipped(int step)
        {
            var clock = TestCalendar.Clock(minutesPerStep: step, startMinutesIntoDay: 360);
            var events = RecordAll(clock);
            long target = TestCalendar.Minutes(3, 6);

            clock.SkipTo(target);

            CollectionAssert.AreEqual(new[] { $"Skipped 360->{target}" }, events);
            Assert.AreEqual(target, clock.Now.TotalMinutes);
            Assert.AreEqual(3, clock.Now.DayOfWeek);
        }

        [Test]
        public void SkipTo_AppliesFixedLengthSeasonsAcrossYears()
        {
            // Years are 5 days long (2 + 3). Day 23 is day 1 of season 1 in Year 1 + 4.
            var clock = TestCalendar.Clock(seasons: TestCalendar.Seasons(2, 3));

            clock.SkipTo(TestCalendar.Minutes(23, 5));

            Assert.AreEqual(5, clock.Now.Year);
            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(1, clock.Now.DayInSeason);
        }

        [Test]
        public void SkipTo_AppliesPendingAdvanceAtFirstBoundary_AndCommandSeasonsThenStay()
        {
            var clock = TestCalendar.Clock(seasons: TestCalendar.Seasons(0, 0, 0));
            clock.AdvanceSeason();

            clock.SkipTo(TestCalendar.Minutes(10));

            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(9, clock.Now.DayInSeason);
        }

        [Test]
        public void SkipTo_Now_DoesNothing()
        {
            var clock = TestCalendar.Clock();
            var events = RecordAll(clock);

            clock.SkipTo(0);

            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void SkipTo_PastOrOffStep_Throws()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);
            clock.Tick(20f);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SkipTo(10));
            Assert.Throws<ArgumentException>(() => clock.SkipTo(25));
        }

        [Test]
        public void SkipTo_DropsGatheredTime_AndNextTickCountsFromTheSkip()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);
            clock.Tick(15f);
            var deltas = new List<long>();
            clock.Ticked += info => deltas.Add(info.DeltaMinutes);

            clock.SkipTo(100);
            Assert.AreEqual(100.0, clock.ExactMinutes, 1e-6);

            clock.Tick(10f);
            CollectionAssert.AreEqual(new[] { 10L }, deltas);
        }

        [Test]
        public void SkipTo_RunsWhilePaused()
        {
            var clock = TestCalendar.Clock();
            clock.Pause(this);

            clock.SkipTo(100);

            Assert.AreEqual(100, clock.Now.TotalMinutes);
            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void FastForwardTo_RaisesNormalEvents_AndStopsExactlyAtTarget()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);
            var ticks = new List<TickInfo>();
            var completed = new List<long>();
            clock.Ticked += ticks.Add;
            clock.FastForwardCompleted += date => completed.Add(date.TotalMinutes);

            clock.FastForwardTo(60, 20f);
            clock.Tick(1f);
            Assert.AreEqual(20, clock.Now.TotalMinutes);
            Assert.AreEqual(20f, ticks[0].Speed);
            Assert.IsTrue(ticks[0].IsFastForwarding);
            Assert.IsTrue(clock.IsFastForwarding);

            clock.Tick(10f);
            Assert.AreEqual(60, clock.Now.TotalMinutes);
            Assert.AreEqual(60.0, clock.ExactMinutes, 1e-6, "Time left in the frame after the target is dropped.");
            CollectionAssert.AreEqual(new[] { 60L }, completed);
            Assert.IsFalse(clock.IsFastForwarding);
            Assert.AreEqual(1f, clock.Speed);
            Assert.AreEqual(6, ticks.Count);
        }

        [Test]
        public void FastForwardTo_Now_CompletesImmediately()
        {
            var clock = TestCalendar.Clock();
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;

            clock.FastForwardTo(0, 10f);

            Assert.AreEqual(1, completed);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void FastForward_InvalidArguments_Throw()
        {
            var clock = TestCalendar.Clock(minutesPerStep: 10);
            clock.Tick(20f);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(10, 5f));
            Assert.Throws<ArgumentException>(() => clock.FastForwardTo(25, 5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(30, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForward(-1f));
        }

        [Test]
        public void FastForward_IsClampedToMaxSpeed()
        {
            var clock = new GameClock(TestCalendar.Config(maxSpeed: 5f));

            clock.FastForward(50f);
            clock.Tick(1f);

            Assert.AreEqual(5f, clock.Speed);
            Assert.AreEqual(5, clock.Now.TotalMinutes);
        }

        [Test]
        public void FastForwardAgain_ReplacesTarget_WithoutCompletingTheOldOne()
        {
            var clock = TestCalendar.Clock();
            var completed = new List<long>();
            clock.FastForwardCompleted += date => completed.Add(date.TotalMinutes);

            clock.FastForwardTo(10, 10f);
            clock.FastForwardTo(30, 10f);
            clock.Tick(5f);

            CollectionAssert.AreEqual(new[] { 30L }, completed);
        }

        [Test]
        public void StopFastForward_ReturnsToBaseSpeed_WithoutCompleting_AndKeepsSpeedSetDuringFastForward()
        {
            var clock = TestCalendar.Clock();
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;

            clock.FastForward(10f);
            clock.SetSpeed(3f);
            Assert.AreEqual(10f, clock.Speed);

            clock.StopFastForward();

            Assert.AreEqual(3f, clock.Speed);
            Assert.IsFalse(clock.IsFastForwarding);
            Assert.AreEqual(0, completed);
        }

        [Test]
        public void SkipTo_CancelsFastForward_WithoutCompleting()
        {
            var clock = TestCalendar.Clock();
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;

            clock.FastForwardTo(100, 10f);
            clock.SkipTo(50);

            Assert.IsFalse(clock.IsFastForwarding);
            Assert.AreEqual(0, completed);
        }

        [Test]
        public void Pause_HoldsFastForward_AndResumeContinuesIt()
        {
            var clock = TestCalendar.Clock();
            clock.FastForwardTo(100, 10f);

            clock.Pause(this);
            clock.Tick(5f);
            Assert.AreEqual(0, clock.Now.TotalMinutes);
            Assert.IsTrue(clock.IsFastForwarding);

            clock.Resume(this);
            clock.Tick(5f);
            Assert.AreEqual(50, clock.Now.TotalMinutes);
        }

        [TestCase(1)]
        [TestCase(10)]
        public void FastForwardAndSkip_OverTheSameSpan_EndInTheSameState(int step)
        {
            // Spans several fixed-length seasons, a year change, a pending season command, and the need hitting zero.
            long target = TestCalendar.Minutes(9, 13, 20);
            var (fastClock, fastNeed) = StartScenario(step);
            var (skipClock, skipNeed) = StartScenario(step);

            fastClock.FastForwardTo(target, 1000f);
            for (int frame = 0; fastClock.IsFastForwarding && frame < 100000; frame++)
            {
                fastClock.Tick(0.37f);
            }

            skipClock.SkipTo(target);

            Assert.AreEqual(target, fastClock.Now.TotalMinutes);
            Assert.AreEqual(skipClock.CaptureSnapshot(), fastClock.CaptureSnapshot());
            Assert.AreEqual(skipNeed.Value, fastNeed.Value);
            Assert.AreEqual(skipNeed.ZeroSinceMinute, fastNeed.ZeroSinceMinute);
            Assert.IsNotNull(skipNeed.ZeroSinceMinute);
        }

        private static (GameClock Clock, FakeNeed Need) StartScenario(int step)
        {
            var clock = new GameClock(TestCalendar.Config(minutesPerStep: step, startMinutesIntoDay: 480, seasons: TestCalendar.Seasons(2, 3)));
            clock.Tick(4f * step);
            clock.AdvanceSeason();

            var need = new FakeNeed(3000);
            clock.Ticked += info => need.Advance(info.DeltaMinutes, info.Date.TotalMinutes);
            clock.Skipped += (from, to) => need.Advance(to.TotalMinutes - from.TotalMinutes, to.TotalMinutes);
            return (clock, need);
        }

        internal static List<string> RecordAll(GameClock clock)
        {
            var events = new List<string>();
            clock.Ticked += info => events.Add($"Ticked {info.Date.TotalMinutes}");
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
