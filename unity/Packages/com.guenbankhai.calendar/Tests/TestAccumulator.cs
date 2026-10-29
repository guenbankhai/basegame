using System;

namespace GuenBanKhai.Calendar.Tests
{
    /// <summary>
    /// Stand-in accumulating module: drops 1 per game minute and records when it hit 0. Fed from both Stepped and
    /// Skipped, as the module convention asks, so fast-forward and skip must end with the same value.
    /// </summary>
    internal class TestAccumulator
    {
        public TestAccumulator(GameClock clock, int start)
        {
            Value = start;
            clock.Stepped += date => Advance(clock.MinutesPerStep, date.TotalMinutes);
            clock.Skipped += (from, to) => Advance(to.TotalMinutes - from.TotalMinutes, to.TotalMinutes);
        }

        public int Value { get; private set; }
        /// <summary>The TotalMinutes at which Value first reached 0, or null while above 0.</summary>
        public long? ZeroSinceMinute { get; private set; }

        private void Advance(long deltaMinutes, long endTotalMinutes)
        {
            if (Value > 0 && deltaMinutes >= Value)
            {
                ZeroSinceMinute = endTotalMinutes - deltaMinutes + Value;
            }

            Value = (int)Math.Max(0, Value - deltaMinutes);
        }
    }
}
