using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuenBanKhai.Calendar.Tests
{
    public class GameClockSnapshotTests
    {
        [Test]
        public void Restore_BringsBackCapturedState_AndRaisesOnlyRestored()
        {
            var source = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(2, 0)));
            source.Tick(TestCalendar.Minutes(3, 7));
            source.AdvanceSeason();
            var snapshot = source.CaptureSnapshot();

            var target = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(2, 0)));
            var events = TestCalendar.RecordAll(target);
            var restored = new List<CalendarDate>();
            target.Restored += restored.Add;

            target.Restore(snapshot);

            Assert.AreEqual(snapshot, target.CaptureSnapshot());
            Assert.AreEqual(source.Now, target.Now);
            Assert.IsTrue(snapshot.SeasonAdvancePending);
            CollectionAssert.IsEmpty(events);
            CollectionAssert.AreEqual(new[] { source.Now }, restored);
        }

        [Test]
        public void CaptureSnapshot_IsACopy()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var snapshot = clock.CaptureSnapshot();

            clock.Tick(5f);
            clock.AdvanceSeason();

            Assert.AreEqual(0, snapshot.TotalMinutes);
            Assert.IsFalse(snapshot.SeasonAdvancePending);
        }

        [Test]
        public void Snapshot_SurvivesJsonUtilityRoundTrip()
        {
            var snapshot = new CalendarSnapshot(TestCalendar.Minutes(40, 12), 3, 1, 30, true);

            var roundTripped = JsonUtility.FromJson<CalendarSnapshot>(JsonUtility.ToJson(snapshot));

            Assert.AreEqual(snapshot, roundTripped);
        }

        [Test]
        public void Restore_InvalidData_Throws()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10, seasons: TestCalendar.Seasons(28, 28)));

            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(-10, 1, 0, 0, false)), "negative time");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(15, 1, 0, 0, false)), "off step");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot((GameClock.MaxTotalMinutes / 10 + 1) * 10, 1, 0, 0, false)), "too far, on step");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(0, 1, 2, 0, false)), "season index");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(0, 1, -1, 0, false)), "negative season index");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(0, -1, 0, 0, false)), "year");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(TestCalendar.Minutes(3), 1, 0, 4, false)), "season start after today");
            Assert.Catch<ArgumentException>(() => clock.Restore(new CalendarSnapshot(TestCalendar.Minutes(3), 1, 0, -1, false)), "negative season start");
            Assert.AreEqual(0, clock.Now.TotalMinutes, "A rejected snapshot changes nothing.");
        }

        [Test]
        public void Restore_CanGoBack_AndNextTickCountsFromTheRestoredPoint()
        {
            var clock = new GameClock(TestCalendar.Settings(minutesPerStep: 10));
            clock.Tick(20f);
            var snapshot = clock.CaptureSnapshot();
            clock.Tick(35f);
            var steps = new List<StepInfo>();
            clock.Stepped += steps.Add;

            clock.Restore(snapshot);
            Assert.AreEqual(20.0, clock.ExactMinutes, 1e-6);
            clock.Tick(10f);

            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual(30, steps[0].Date.TotalMinutes);
            Assert.AreEqual(10, steps[0].DeltaMinutes);
        }

        [Test]
        public void Restore_CancelsFastForward_WithoutCompleting()
        {
            var clock = new GameClock(TestCalendar.Settings());
            int completed = 0;
            clock.FastForwardCompleted += _ => completed++;
            var snapshot = clock.CaptureSnapshot();

            clock.FastForwardTo(100, 10f);
            clock.Restore(snapshot);

            Assert.IsFalse(clock.IsFastForwarding);
            Assert.AreEqual(0, completed);
        }

        [Test]
        public void Restore_KeepsPauseSourcesAndBaseSpeed()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var snapshot = clock.CaptureSnapshot();
            clock.SetSpeed(3f);
            clock.Pause(this);

            clock.Restore(snapshot);

            Assert.IsTrue(clock.IsPaused);
            Assert.AreEqual(3f, clock.Speed);
        }

        [Test]
        public void Restore_DayInSeasonPastLength_SeasonChangesAtNextDayBoundary()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(28, 28)));

            clock.Restore(new CalendarSnapshot(TestCalendar.Minutes(40, 12), 1, 0, 0, false));
            Assert.AreEqual(0, clock.Now.SeasonIndex);

            clock.Tick(TestCalendar.Minutes(0, 12));
            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(0, clock.Now.DayInSeason);
        }
    }
}
