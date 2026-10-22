using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BaseGame.Playground.Calendar.Tests
{
    public class GameClockCommandQueueTests
    {
        [Test]
        public void CommandInHandler_RunsAfterThatMinutesEvents_AndDropsRestOfTick()
        {
            var clock = TestCalendar.Clock();
            var events = GameClockSkipFastForwardTests.RecordAll(clock);
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
            var clock = TestCalendar.Clock();
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
        public void QueuedCommands_RunInOrder()
        {
            var clock = TestCalendar.Clock();
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
        public void QueuedCommand_IsCheckedWhenItRuns_AndAFailureIsLoggedNotThrown()
        {
            var clock = TestCalendar.Clock();
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
            var clock = TestCalendar.Clock();
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
            var clock = TestCalendar.Clock();
            clock.Skipped += (from, to) => clock.Pause(this);

            clock.SkipTo(100);

            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void ThrowingHandler_IsLogged_AndOtherHandlersStillRun()
        {
            var clock = TestCalendar.Clock();
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
            var clock = TestCalendar.Clock();
            clock.Ticked += _ => clock.Tick(1f);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));

            clock.Tick(1f);

            Assert.AreEqual(1, clock.Now.TotalMinutes);
        }
    }
}
