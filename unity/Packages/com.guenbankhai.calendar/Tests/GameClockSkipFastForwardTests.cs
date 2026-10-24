using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GuenBanKhai.Calendar.Tests
{
    public class GameClockSkipFastForwardTests
    {
        [TestCase(1)]
        [TestCase(10)]
        public void SkipTo_MovesAtOnce_AndRaisesOnlySkipped(int step)
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: step, startMinutesIntoDay: 360));
            var events = TestCalendar.RecordAll(clock);
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
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(2, 3)));

            clock.SkipTo(TestCalendar.Minutes(23, 5));

            Assert.AreEqual(5, clock.Now.Year);
            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(1, clock.Now.DayInSeason);
        }

        [Test]
        public void SkipTo_AppliesPendingAdvanceAtFirstBoundary_AndCommandSeasonsThenStay()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(0, 0, 0)));
            clock.AdvanceSeason();

            clock.SkipTo(TestCalendar.Minutes(10));

            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(9, clock.Now.DayInSeason);
        }

        [Test]
        public void SkipTo_Now_DoesNothing()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var events = TestCalendar.RecordAll(clock);

            clock.SkipTo(0);

            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void SkipTo_PastOrOffStep_Throws()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            clock.Tick(20f);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SkipTo(10));
            Assert.Throws<ArgumentException>(() => clock.SkipTo(25));
        }

        [Test]
        public void SkipTo_UpToMaxSkipDays_Works_AndFurtherThrows()
        {
            var clock = new GameClock(TestCalendar.Settings());
            long limit = TestCalendar.Minutes(GameClock.MaxSkipDays);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SkipTo(limit + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SkipTo(long.MaxValue));
            Assert.AreEqual(0, clock.Now.TotalMinutes);

            clock.SkipTo(limit);
            Assert.AreEqual(limit, clock.Now.TotalMinutes);
        }

        [Test]
        public void SkipToOrFastForwardTo_PastMaxTotalMinutes_Throws_SoTheClockStaysRestorable()
        {
            var clock = new GameClock(TestCalendar.Settings());
            long start = GameClock.MaxTotalMinutes - CalendarDate.MinutesPerDay;
            clock.Restore(new CalendarSnapshot(start, 1, 0, 0, false));

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SkipTo(GameClock.MaxTotalMinutes + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(GameClock.MaxTotalMinutes + 1, 10f));
            Assert.AreEqual(start, clock.Now.TotalMinutes);
            Assert.IsFalse(clock.IsFastForwarding);

            clock.SkipTo(GameClock.MaxTotalMinutes);
            Assert.DoesNotThrow(() => clock.Restore(clock.CaptureSnapshot()));
        }

        [Test]
        public void SkipTo_DropsGatheredTime_AndNextTickCountsFromTheSkip()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            clock.Tick(15f);
            var deltas = new List<long>();
            clock.Stepped += info => deltas.Add(info.DeltaMinutes);

            clock.SkipTo(100);
            Assert.AreEqual(100.0, clock.ExactMinutes, 1e-6);

            clock.Tick(10f);
            CollectionAssert.AreEqual(new[] { 10L }, deltas);
        }

        [Test]
        public void SkipTo_RunsWhilePaused()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Pause(this);

            clock.SkipTo(100);

            Assert.AreEqual(100, clock.Now.TotalMinutes);
            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void FastForwardTo_RaisesNormalEvents_AndStopsExactlyAtTarget()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            var ticks = new List<StepInfo>();
            var completed = new List<long>();
            clock.Stepped += ticks.Add;
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
        public void FastForwardTo_FinalStepHandlers_StillSeeFastForwarding_UntilCompleted()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            var seen = new List<string>();
            clock.Stepped += info => seen.Add($"Stepped {info.Date.TotalMinutes} {info.IsFastForwarding} {clock.IsFastForwarding} {clock.Speed}");
            clock.HourChanged += date => seen.Add($"Hour {date.TotalMinutes} {clock.IsFastForwarding} {clock.Speed}");
            clock.FastForwardCompleted += date => seen.Add($"Completed {date.TotalMinutes} {clock.IsFastForwarding} {clock.Speed}");

            clock.FastForwardTo(60, 20f);
            clock.Tick(10f);

            Assert.AreEqual("Stepped 60 True True 20", seen[seen.Count - 3]);
            Assert.AreEqual("Hour 60 True 20", seen[seen.Count - 2]);
            Assert.AreEqual("Completed 60 False 1", seen[seen.Count - 1]);
        }

        [Test]
        public void FastForwardTo_Now_CompletesImmediately()
        {
            var clock = new GameClock(TestCalendar.Settings());
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;

            clock.FastForwardTo(0, 10f);

            Assert.AreEqual(1, completed);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void FastForward_InvalidArguments_Throw()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            clock.Tick(20f);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(10, 5f));
            Assert.Throws<ArgumentException>(() => clock.FastForwardTo(25, 5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(30, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(30, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForward(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForward(float.NaN));
        }

        [Test]
        public void FastForward_AtMaxSpeed_Runs()
        {
            var clock = new GameClock(TestCalendar.Settings(maxSpeed: 5f));

            clock.FastForward(5f);
            clock.Tick(1f);

            Assert.AreEqual(5f, clock.Speed);
            Assert.AreEqual(5, clock.Now.TotalMinutes);
        }

        [TestCase(5.5f)]
        [TestCase(float.PositiveInfinity)]
        public void FastForward_AboveMaxSpeed_Throws_AndDoesNotStart(float speed)
        {
            var clock = new GameClock(TestCalendar.Settings(maxSpeed: 5f));

            var error = Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForward(speed));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.FastForwardTo(10, speed));

            StringAssert.Contains("MaxSpeed (5)", error.Message);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void FastForwardTo_StoppedByAHandlerAtTheTarget_DoesNotComplete()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;
            clock.HourChanged += _ => clock.StopFastForward();

            clock.FastForwardTo(60, 20f);
            clock.Tick(10f);

            Assert.AreEqual(0, completed);
            Assert.AreEqual(60, clock.Now.TotalMinutes);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void FastForwardTo_ReplacedByAHandlerAtTheTarget_CompletesOnlyTheNewTarget()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            var completed = new List<long>();
            clock.FastForwardCompleted += date => completed.Add(date.TotalMinutes);
            clock.HourChanged += date =>
            {
                if (date.TotalMinutes == 60)
                {
                    clock.FastForwardTo(120, 20f);
                }
            };

            clock.FastForwardTo(60, 20f);
            clock.Tick(10f);

            CollectionAssert.AreEqual(new[] { 120L }, completed);
            Assert.AreEqual(120, clock.Now.TotalMinutes);
        }

        [Test]
        public void FastForwardTo_OtherCommandsAtTheTarget_StillComplete_AndRunBeforeCompleted()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            var seen = new List<string>();
            clock.HourChanged += _ =>
            {
                clock.SetSpeed(3f);
                clock.Pause(this);
            };
            clock.FastForwardCompleted += date => seen.Add($"Completed {date.TotalMinutes} paused {clock.IsPaused}");

            clock.FastForwardTo(60, 20f);
            clock.Tick(10f);

            CollectionAssert.AreEqual(new[] { "Completed 60 paused True" }, seen);
            Assert.AreEqual(3f, clock.Speed);
        }

        [Test]
        public void FastForwardAgain_ReplacesTarget_WithoutCompletingTheOldOne()
        {
            var clock = new GameClock(TestCalendar.Settings());
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
            var clock = new GameClock(TestCalendar.Settings());
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
            var clock = new GameClock(TestCalendar.Settings());
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;

            clock.FastForwardTo(100, 10f);
            clock.SkipTo(50);

            Assert.IsFalse(clock.IsFastForwarding);
            Assert.AreEqual(0, completed);
        }

        [Test]
        public void SkipToNow_CancelsFastForward_WithoutCompletingOrSkipping()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var events = TestCalendar.RecordAll(clock);
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;

            clock.FastForwardTo(100, 10f);
            clock.SkipTo(0);

            Assert.IsFalse(clock.IsFastForwarding);
            Assert.AreEqual(0, completed);
            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void FastForwardTo_SkipToNowByAHandlerAtTheTarget_DoesNotComplete()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            int completed = 0;
            int skipped = 0;
            clock.FastForwardCompleted += _ => completed++;
            clock.Skipped += (from, to) => skipped++;
            clock.HourChanged += date => clock.SkipTo(date.TotalMinutes);

            clock.FastForwardTo(60, 20f);
            clock.Tick(10f);

            Assert.AreEqual(0, completed);
            Assert.AreEqual(0, skipped);
            Assert.AreEqual(60, clock.Now.TotalMinutes);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void Pause_HoldsFastForward_AndResumeContinuesIt()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.FastForwardTo(100, 10f);

            clock.Pause(this);
            clock.Tick(5f);
            Assert.AreEqual(0, clock.Now.TotalMinutes);
            Assert.IsTrue(clock.IsFastForwarding);

            clock.Resume(this);
            clock.Tick(5f);
            Assert.AreEqual(50, clock.Now.TotalMinutes);
        }

        [TestCase(1, false)]
        [TestCase(10, false)]
        [TestCase(10, true)]
        public void FastForwardAndSkip_OverTheSameSpan_EndInTheSameState(int step, bool setSpeedEveryHour)
        {
            // Crosses fixed-length seasons, a year, a pending season command and the accumulator hitting zero.
            // setSpeedEveryHour checks that base-speed changes from a handler do not disturb the fast-forward.
            long target = TestCalendar.Minutes(9, 13, 20);
            var (fastClock, fastValue) = StartScenario(step);
            var (skipClock, skipValue) = StartScenario(step);
            if (setSpeedEveryHour)
            {
                fastClock.HourChanged += date => fastClock.SetSpeed(date.Hour % 3);
            }

            fastClock.FastForwardTo(target, 1000f);
            for (int frame = 0; fastClock.IsFastForwarding && frame < 100000; frame++)
            {
                fastClock.Tick(0.37f);
            }

            skipClock.SkipTo(target);

            Assert.AreEqual(target, fastClock.Now.TotalMinutes);
            Assert.AreEqual(skipClock.CaptureSnapshot(), fastClock.CaptureSnapshot());
            Assert.AreEqual(skipValue.Value, fastValue.Value);
            Assert.AreEqual(skipValue.ZeroSinceMinute, fastValue.ZeroSinceMinute);
            Assert.IsNotNull(skipValue.ZeroSinceMinute);
        }

        private static (GameClock Clock, TestAccumulator Value) StartScenario(int step)
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: step, startMinutesIntoDay: 480, seasons: TestCalendar.Seasons(2, 3)));
            clock.Tick(4f * step);
            clock.AdvanceSeason();
            return (clock, new TestAccumulator(clock, 3000));
        }
    }
}
