using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuenBanKhai.Calendar.Tests
{
    public class GameClockCommandQueueTests
    {
        [Test]
        public void CommandQueue_CommandInHandler_RunsAfterThatMinutesEvents_AndDropsRestOfTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var events = TestCalendar.RecordAll(clock);
            var nowInHourHandler = new List<long>();
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 60)
                {
                    clock.SkipTo(200);
                }
            };
            clock.HourChanged += _ => nowInHourHandler.Add(clock.Now.TotalMinutes);

            clock.Tick(120f);

            CollectionAssert.AreEqual(new[] { 60L }, nowInHourHandler);
            Assert.AreEqual("Hour 60", events[events.Count - 2]);
            Assert.AreEqual("Skipped 60->200", events[events.Count - 1]);
            Assert.AreEqual(200, clock.Now.TotalMinutes);
            Assert.AreEqual(200.0, clock.ExactMinutes, 1e-6);
        }

        [Test]
        public void PauseInHandler_MidTick_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.Pause(this);
                    Assert.IsFalse(clock.IsPaused, "Queued until the minute's events are done.");
                }
            };

            clock.Tick(10f);

            Assert.AreEqual(3, clock.Now.TotalMinutes);
            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void RestoreInHandler_MidTick_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var snapshot = clock.CaptureSnapshot();
            var restored = new List<long>();
            clock.Restored += date => restored.Add(date.TotalMinutes);
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.Restore(snapshot);
                }
            };

            clock.Tick(10f);

            CollectionAssert.AreEqual(new[] { 0L }, restored);
            Assert.AreEqual(0, clock.Now.TotalMinutes);
            Assert.AreEqual(0.0, clock.ExactMinutes, 1e-6);
        }

        [Test]
        public void SetSpeedInHandler_MidTick_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.SetSpeed(2f);
                }
            };

            clock.Tick(10f);
            Assert.AreEqual(3, clock.Now.TotalMinutes);
            Assert.AreEqual(3.0, clock.ExactMinutes, 1e-6);

            clock.Tick(1f);
            Assert.AreEqual(5, clock.Now.TotalMinutes, "The next Tick runs at the new speed.");
        }

        [Test]
        public void StopFastForwardInHandler_MidTick_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.FastForwardTo(TestCalendar.Minutes(1), 10f);
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.StopFastForward();
                }
            };

            clock.Tick(10f);

            Assert.AreEqual(3, clock.Now.TotalMinutes);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void FastForwardToInHandler_Now_CompletesAndStopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var completed = new List<long>();
            clock.FastForwardCompleted += date => completed.Add(date.TotalMinutes);
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.FastForwardTo(3, 1f);
                }
            };

            clock.Tick(10f);

            CollectionAssert.AreEqual(new[] { 3L }, completed);
            Assert.AreEqual(3, clock.Now.TotalMinutes);
        }

        [Test]
        public void SetSpeedInHandler_DuringFastForward_DoesNotStopTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.FastForwardTo(TestCalendar.Minutes(1), 10f);
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.SetSpeed(2f);
                }
            };

            clock.Tick(1f);

            Assert.AreEqual(10, clock.Now.TotalMinutes, "The running speed stays the fast-forward speed.");
            clock.StopFastForward();
            Assert.AreEqual(2f, clock.Speed);
        }

        [Test]
        public void SetSpeedInHandler_SameSpeed_DoesNotStopTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Stepped += date => clock.SetSpeed(1f);

            clock.Tick(10f);

            Assert.AreEqual(10, clock.Now.TotalMinutes);
        }

        [Test]
        public void CommandQueue_SeveralCommandsInHandler_RunInOrder()
        {
            var clock = new GameClock(TestCalendar.Settings());
            object a = new object();
            object b = new object();
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 1)
                {
                    clock.Pause(a);
                    clock.Resume(a);
                    clock.Resume(b);
                    clock.Pause(b);
                }
            };

            clock.Tick(5f);

            Assert.IsTrue(clock.IsPaused);
            Assert.AreEqual(1, clock.Now.TotalMinutes);
            clock.Resume(b);
            Assert.IsFalse(clock.IsPaused);
        }

        [Test]
        public void CommandQueue_CommandsQueuedWhileDraining_RunAfterTheEarlierOnes()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var order = new List<string>();
            object a = new object();
            clock.Skipped += (from, to) =>
            {
                order.Add($"Skipped {to.TotalMinutes}");
                clock.Pause(a);
            };
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 1)
                {
                    clock.SkipTo(10);
                    clock.Resume(a);
                }
            };

            clock.Tick(5f);

            CollectionAssert.AreEqual(new[] { "Skipped 10" }, order);
            Assert.IsTrue(clock.IsPaused, "Pause from the Skipped handler runs after the Resume queued before it.");
        }

        [Test]
        public void CommandQueue_HandlersKeepQueueing_AreCutOffAndLogged()
        {
            var clock = new GameClock(TestCalendar.Settings());
            int completed = 0;
            clock.FastForwardCompleted += date =>
            {
                completed++;
                clock.FastForwardTo(date.TotalMinutes, 1f);
            };
            LogAssert.Expect(LogType.Error, new Regex("kept queueing commands"));

            clock.FastForwardTo(0, 1f);

            Assert.Greater(completed, 1);
            clock.Tick(1f);
            Assert.AreEqual(1, clock.Now.TotalMinutes, "The clock still runs afterwards.");
        }

        [Test]
        public void CommandQueue_CutOff_DropsEveryCommandStillQueuedIncludingAResume()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var source = new object();
            clock.Pause(source);
            int completed = 0;
            clock.FastForwardCompleted += date =>
            {
                completed++;
                // Two commands per run grow the queue, so a command queued this late is still waiting at the cut-off.
                clock.FastForwardTo(date.TotalMinutes, 1f);
                clock.FastForwardTo(date.TotalMinutes, 1f);
                if (completed == 600)
                {
                    clock.Resume(source);
                }
            };
            LogAssert.Expect(LogType.Error, new Regex("kept queueing commands"));

            clock.FastForwardTo(0, 1f);

            Assert.GreaterOrEqual(completed, 600, "The Resume was queued before the cut-off.");
            Assert.IsTrue(clock.IsPaused, "The cut-off has no exceptions: the queued Resume is dropped too.");
        }

        [Test]
        public void CommandQueue_InvalidQueuedCommand_IsCheckedWhenItRunsAndLoggedNotThrown()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 1)
                {
                    clock.SkipTo(10);
                    clock.SkipTo(5);
                }
            };
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentOutOfRangeException"));

            Assert.DoesNotThrow(() => clock.Tick(3f));

            Assert.AreEqual(10, clock.Now.TotalMinutes);
        }

        [Test]
        public void AdvanceSeasonInHandler_IsQueued_AndDoesNotStopTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            bool pendingInsideHandler = true;
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 1)
                {
                    clock.AdvanceSeason();
                    pendingInsideHandler = clock.CaptureSnapshot().SeasonAdvancePending;
                }
            };

            clock.Tick(5f);

            Assert.IsFalse(pendingInsideHandler, "Queued until the minute's events are done.");
            Assert.IsTrue(clock.CaptureSnapshot().SeasonAdvancePending);
            Assert.AreEqual(5, clock.Now.TotalMinutes);
        }

        [Test]
        public void AdvanceSeasonInHandler_AfterRestore_AppliesToTheRestoredState()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var snapshot = clock.CaptureSnapshot();
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 3)
                {
                    clock.Restore(snapshot);
                    clock.AdvanceSeason();
                }
            };

            clock.Tick(10f);

            Assert.AreEqual(0, clock.Now.TotalMinutes);
            Assert.IsTrue(clock.CaptureSnapshot().SeasonAdvancePending);
        }

        [Test]
        public void AdvanceSeasonInHandler_AfterSkipTo_ChangesTheSeasonAfterTheSkip()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(0, 0)));
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 1)
                {
                    clock.SkipTo(TestCalendar.Minutes(2, 12));
                    clock.AdvanceSeason();
                }
            };

            clock.Tick(1f);
            Assert.AreEqual(0, clock.Now.SeasonIndex, "The skip crossed day boundaries before the flag was set.");

            clock.Tick(TestCalendar.Minutes(0, 12));
            Assert.AreEqual(1, clock.Now.SeasonIndex);
            Assert.AreEqual(0, clock.Now.DayInSeason);
        }

        [Test]
        public void RestoreInHandler_InvalidSnapshot_IsLoggedAndChangesNothing()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.AdvanceSeason();
            int restored = 0;
            clock.Restored += _ => restored++;
            clock.Stepped += date =>
            {
                if (date.TotalMinutes == 1)
                {
                    clock.Restore(new CalendarSnapshot(0, -1, 0, 0, false));
                }
            };
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));

            clock.Tick(3f);

            Assert.AreEqual(0, restored);
            Assert.AreEqual(3, clock.Now.TotalMinutes, "A rejected command does not stop the rest of the Tick.");
            Assert.AreEqual(1, clock.Now.Year);
            Assert.IsTrue(clock.CaptureSnapshot().SeasonAdvancePending);
        }

        [Test]
        public void CommandQueue_CommandInSkippedHandler_RunsBeforeSkipToReturns()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Skipped += (from, to) => clock.Pause(this);

            clock.SkipTo(100);

            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void Event_ThrowingHandler_IsLoggedAndOtherHandlersStillRun()
        {
            var clock = new GameClock(TestCalendar.Settings());
            int secondCalls = 0;
            clock.Stepped += _ => throw new InvalidOperationException("boom");
            clock.Stepped += _ => secondCalls++;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            clock.Tick(2f);

            Assert.AreEqual(2, secondCalls);
            Assert.AreEqual(2, clock.Now.TotalMinutes);
        }

        [Test]
        public void TickInHandler_Always_Throws()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Stepped += _ => clock.Tick(1f);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));

            clock.Tick(1f);

            Assert.AreEqual(1, clock.Now.TotalMinutes);
        }
    }
}
