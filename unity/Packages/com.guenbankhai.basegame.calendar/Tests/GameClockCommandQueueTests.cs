using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BaseGame.Calendar.Tests
{
    public class GameClockCommandQueueTests
    {
        [Test]
        public void CommandInHandler_RunsAfterThatMinutesEvents_AndDropsRestOfTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var events = TestCalendar.RecordAll(clock);
            var nowInHourHandler = new List<long>();
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 60)
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
        public void PauseInHandler_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 3)
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
        public void RestoreInHandler_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var snapshot = clock.CaptureSnapshot();
            var restored = new List<long>();
            clock.Restored += date => restored.Add(date.TotalMinutes);
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 3)
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
        public void SetSpeedInHandler_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 3)
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
        public void StopFastForwardInHandler_StopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.FastForward(10f);
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 3)
                {
                    clock.StopFastForward();
                }
            };

            clock.Tick(10f);

            Assert.AreEqual(3, clock.Now.TotalMinutes);
            Assert.IsFalse(clock.IsFastForwarding);
        }

        [Test]
        public void FastForwardToNowInHandler_CompletesAndStopsTheRestOfTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var completed = new List<long>();
            clock.FastForwardCompleted += date => completed.Add(date.TotalMinutes);
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 3)
                {
                    clock.FastForwardTo(3, 1f);
                }
            };

            clock.Tick(10f);

            CollectionAssert.AreEqual(new[] { 3L }, completed);
            Assert.AreEqual(3, clock.Now.TotalMinutes);
        }

        [Test]
        public void SetSpeedInHandlerDuringFastForward_DoesNotStopTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.FastForward(10f);
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 3)
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
        public void SameSpeedCommandsInHandler_DoNotStopTheTick()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Ticked += info => clock.SetSpeed(1f);

            clock.Tick(10f);

            Assert.AreEqual(10, clock.Now.TotalMinutes);
        }

        [Test]
        public void QueuedCommands_RunInOrder()
        {
            var clock = new GameClock(TestCalendar.Settings());
            object a = new object();
            object b = new object();
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 1)
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
        public void CommandsQueuedByQueuedCommands_RunAfterTheEarlierOnes()
        {
            var clock = new GameClock(TestCalendar.Settings());
            var order = new List<string>();
            object a = new object();
            clock.Skipped += (from, to) =>
            {
                order.Add($"Skipped {to.TotalMinutes}");
                clock.Pause(a);
            };
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 1)
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
        public void HandlersThatKeepQueueingCommands_AreCutOff_AndLogged()
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
        public void QueuedCommand_IsCheckedWhenItRuns_AndAFailureIsLoggedNotThrown()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Ticked += info =>
            {
                if (info.Date.TotalMinutes == 1)
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
        public void AdvanceSeasonInHandler_AppliesImmediately()
        {
            var clock = new GameClock(TestCalendar.Settings());
            bool pendingInsideHandler = false;
            clock.Ticked += _ =>
            {
                clock.AdvanceSeason();
                pendingInsideHandler = clock.CaptureSnapshot().SeasonAdvancePending;
            };

            clock.Tick(1f);

            Assert.IsTrue(pendingInsideHandler);
        }

        [Test]
        public void CommandInSkippedHandler_RunsBeforeSkipToReturns()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Skipped += (from, to) => clock.Pause(this);

            clock.SkipTo(100);

            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void ThrowingHandler_IsLogged_AndOtherHandlersStillRun()
        {
            var clock = new GameClock(TestCalendar.Settings());
            int secondCalls = 0;
            clock.Ticked += _ => throw new InvalidOperationException("boom");
            clock.Ticked += _ => secondCalls++;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            clock.Tick(2f);

            Assert.AreEqual(2, secondCalls);
            Assert.AreEqual(2, clock.Now.TotalMinutes);
        }

        [Test]
        public void TickInsideHandler_IsRejected()
        {
            var clock = new GameClock(TestCalendar.Settings());
            clock.Ticked += _ => clock.Tick(1f);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));

            clock.Tick(1f);

            Assert.AreEqual(1, clock.Now.TotalMinutes);
        }
    }
}
