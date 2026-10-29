using System;
using NUnit.Framework;

namespace GuenBanKhai.Calendar.Tests
{
    public class GameClockSettingsTests
    {
        [Test]
        public void NewClock_StartsAtConfiguredPoint()
        {
            var settings = TestCalendar.Settings(dayStartMinute: 360, startMinutesIntoDay: 120);
            settings.StartYear = 3;
            settings.StartSeasonIndex = 2;

            var now = new GameClock(settings).Now;

            Assert.AreEqual(120, now.TotalMinutes);
            Assert.AreEqual(0, now.Day);
            Assert.AreEqual(8 * 60, now.ClockMinute);
            Assert.AreEqual(3, now.Year);
            Assert.AreEqual(2, now.SeasonIndex);
            Assert.AreEqual(0, now.DayInSeason);
            Assert.AreEqual(0, now.DayOfWeek);
        }

        [TestCase(7, 0, 0, 0)]
        [TestCase(0, 0, 0, 0)]
        [TestCase(-10, 0, 0, 0)]
        [TestCase(10, 5, 0, 0)]
        [TestCase(10, 0, 15, 0)]
        [TestCase(1, 1440, 0, 0)]
        [TestCase(1, -1, 0, 0)]
        [TestCase(1, 0, 1440, 0)]
        [TestCase(1, 0, -1, 0)]
        [TestCase(1, 0, 0, 4)]
        [TestCase(1, 0, 0, -1)]
        public void NewClock_InvalidSettings_Throws(int step, int dayStartMinute, int startMinutesIntoDay, int startSeasonIndex)
        {
            var settings = TestCalendar.Settings(minutesPerStep: step, dayStartMinute: dayStartMinute, startMinutesIntoDay: startMinutesIntoDay);
            settings.StartSeasonIndex = startSeasonIndex;

            Assert.Throws<ArgumentException>(() => new GameClock(settings));
        }

        [Test]
        public void NewClock_NoSeasonsOrNoDays_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameClock(TestCalendar.Settings(seasons: new SeasonDefinition[0])));
            Assert.Throws<ArgumentException>(() => new GameClock(TestCalendar.Settings(weekLength: 0)));
        }

        [Test]
        public void NewClock_NegativeSeasonLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(28, -1))));
        }

        [Test]
        public void NewClock_NegativeStartYear_Throws()
        {
            var settings = TestCalendar.Settings();
            settings.StartYear = -1;

            Assert.Throws<ArgumentException>(() => new GameClock(settings));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NewClock_MaxStepsPerFrameNotPositive_Throws(int maxStepsPerFrame)
        {
            Assert.Throws<ArgumentException>(() => new GameClock(TestCalendar.Settings(maxStepsPerFrame: maxStepsPerFrame)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NewClock_MaxSkipDaysNotPositive_Throws(int maxSkipDays)
        {
            var settings = TestCalendar.Settings();
            settings.MaxSkipDays = maxSkipDays;

            Assert.Throws<ArgumentException>(() => new GameClock(settings));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void NewClock_NonFiniteOrNotPositiveRates_Throws(float value)
        {
            var secondsPerMinute = TestCalendar.Settings();
            secondsPerMinute.SecondsPerGameMinute = value;
            var maxSpeed = TestCalendar.Settings();
            maxSpeed.MaxSpeed = value;

            Assert.Throws<ArgumentException>(() => new GameClock(secondsPerMinute));
            Assert.Throws<ArgumentException>(() => new GameClock(maxSpeed));
        }

        [Test]
        public void NewClock_MaxSpeedBelowStartingSpeed_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameClock(TestCalendar.Settings(maxSpeed: 0.5f)));
            Assert.DoesNotThrow(() => new GameClock(TestCalendar.Settings(maxSpeed: 1f)));
        }

        [TestCase(null)]
        [TestCase("")]
        public void NewClock_EmptyDayOrSeasonName_Throws(string name)
        {
            var dayName = TestCalendar.Settings();
            dayName.DayNames = new[] { "D0", name };
            var seasonName = TestCalendar.Settings(seasons: new[] { new SeasonDefinition("S0", 28), new SeasonDefinition(name, 28) });

            Assert.Throws<ArgumentException>(() => new GameClock(dayName));
            Assert.Throws<ArgumentException>(() => new GameClock(seasonName));
        }

        [Test]
        public void NewClock_SeasonsBuiltWithSetters_AreUsed()
        {
            var season = new SeasonDefinition { Name = "Monsoon", LengthInDays = 5 };

            var clock = new GameClock(TestCalendar.Settings(seasons: new[] { season }));

            Assert.AreEqual("Monsoon", clock.GetSeasonName(0));
            clock.SkipTo(TestCalendar.Minutes(5));
            Assert.AreEqual(2, clock.Now.Year, "The only season wraps into the next year after its 5 days.");
        }

        [Test]
        public void NewClock_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameClock(null));
        }

        [Test]
        public void GetDayNameOrGetSeasonName_InRange_ReturnTheConfiguredName()
        {
            var clock = new GameClock(TestCalendar.Settings(seasons: TestCalendar.Seasons(28, 0, 5)));

            Assert.AreEqual(7, clock.WeekLength);
            Assert.AreEqual(3, clock.SeasonCount);
            Assert.AreEqual("D3", clock.GetDayName(3));
            Assert.AreEqual("S2", clock.GetSeasonName(2));
        }

        [Test]
        public void GetDayNameOrGetSeasonName_OutOfRange_Throw()
        {
            var clock = new GameClock(TestCalendar.Settings());

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.GetDayName(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.GetDayName(7));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.GetSeasonName(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.GetSeasonName(4));
        }

        [Test]
        public void NewClock_SettingsChangedAfterwards_DoNotReachTheClock()
        {
            var settings = TestCalendar.Settings();
            var clock = new GameClock(settings);

            settings.DayNames = new[] { "Only" };
            settings.Seasons = TestCalendar.Seasons(1);
            settings.MinutesPerStep = 60;

            Assert.AreEqual(7, clock.WeekLength);
            Assert.AreEqual(4, clock.SeasonCount);
            clock.Tick(1f);
            Assert.AreEqual(1, clock.Now.TotalMinutes);
        }
    }
}
