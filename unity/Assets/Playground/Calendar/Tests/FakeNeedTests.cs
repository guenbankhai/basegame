using System;
using NUnit.Framework;

namespace BaseGame.Playground.Calendar.Tests
{
    public class FakeNeedTests
    {
        [Test]
        public void Advance_DropsOnePerMinute()
        {
            var need = new FakeNeed(100);

            need.Advance(30, 30);

            Assert.AreEqual(70, need.Value);
            Assert.IsNull(need.ZeroSinceMinute);
        }

        [Test]
        public void Advance_PastZero_RecordsTheMinuteItHitZero()
        {
            // Started at minute 100 with 100 left: zero at minute 200, even though the update arrives at 260.
            var need = new FakeNeed(100);

            need.Advance(160, 260);

            Assert.AreEqual(0, need.Value);
            Assert.AreEqual(200, need.ZeroSinceMinute);

            need.Advance(60, 320);
            Assert.AreEqual(200, need.ZeroSinceMinute, "Stays at the first time it hit zero.");
        }

        [Test]
        public void Refill_RestoresMax_AndClearsZeroTime()
        {
            var need = new FakeNeed(100);
            need.Advance(150, 150);

            need.Refill();

            Assert.AreEqual(100, need.Value);
            Assert.IsNull(need.ZeroSinceMinute);
        }

        [Test]
        public void Advance_NegativeDelta_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FakeNeed(100).Advance(-1, 0));
        }
    }
}
