using System;

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// Stand-in accumulating module: drops 1 per game minute. One Advance serves both stepping and skipping, so both
    /// give the same result.
    /// </summary>
    public class FakeNeed
    {
        public FakeNeed(int max)
        {
            Max = max;
            Value = max;
        }

        public int Max { get; }
        public int Value { get; private set; }
        /// <summary>The TotalMinutes at which Value first reached 0, or null while above 0.</summary>
        public long? ZeroSinceMinute { get; private set; }

        /// <param name="endTotalMinutes">End of the span; used to place the moment it hit 0 inside a long skip.</param>
        public void Advance(long deltaMinutes, long endTotalMinutes)
        {
            if (deltaMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaMinutes), deltaMinutes, "Must not be negative.");
            }

            if (Value > 0 && deltaMinutes >= Value)
            {
                ZeroSinceMinute = endTotalMinutes - deltaMinutes + Value;
            }

            Value = (int)Math.Max(0, Value - deltaMinutes);
        }

        public void Refill()
        {
            Value = Max;
            ZeroSinceMinute = null;
        }
    }
}
