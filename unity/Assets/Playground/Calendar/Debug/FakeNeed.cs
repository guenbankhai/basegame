using System;

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// Stand-in for an accumulating module (hunger and the like): drops by 1 per game minute.
    /// Written once as Advance so ticking and skipping give the same result.
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

        /// <param name="deltaMinutes">Game minutes since the last update.</param>
        /// <param name="endTotalMinutes">TotalMinutes at the end of that span.</param>
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
