using NUnit.Framework;
using UnityEngine;

namespace BaseGame.Calendar.Tests
{
    public class CalendarConfigTests
    {
        private CalendarConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CalendarConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void CreateSettings_ReturnsTheDefaults()
        {
            var settings = _config.CreateSettings();

            Assert.AreEqual(10, settings.MinutesPerStep);
            Assert.AreEqual(7, settings.DayNames.Count);
            Assert.AreEqual(4, settings.Seasons.Count);
            Assert.AreEqual("Spring", settings.Seasons[0].Name);
            Assert.DoesNotThrow(() => new GameClock(settings));
        }

        [Test]
        public void CreateSettings_ReturnsACopy_SoChangesNeverReachTheAsset()
        {
            var first = _config.CreateSettings();
            first.MaxSpeed = 1000f;
            first.DayNames = new[] { "Only" };

            var second = _config.CreateSettings();

            Assert.AreEqual(100f, second.MaxSpeed);
            Assert.AreEqual(7, second.DayNames.Count);
        }
    }
}
