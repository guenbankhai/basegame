using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Stand-in for a Needs module: drains a Resource by a rate Stat per minute. Its only threshold is reaching Min;
    /// it reports that instead of applying an effect itself, so Simulate can apply it at the end of the span.
    /// </summary>
    public sealed class FakeHunger
    {
        private readonly Resource _hunger;
        private readonly Stat _rate;

        public FakeHunger(Resource hunger, Stat rate)
        {
            _hunger = hunger ?? throw new ArgumentNullException(nameof(hunger));
            _rate = rate ?? throw new ArgumentNullException(nameof(rate));
        }

        /// <summary>Always > 0: already at Min means there is no next threshold (+infinity).</summary>
        public double TimeToNextThreshold
        {
            get
            {
                float rate = _rate.Value;
                if (rate <= 0f || _hunger.Current <= _hunger.Min)
                {
                    return double.PositiveInfinity;
                }

                return (_hunger.Current - _hunger.Min) / (double)rate;
            }
        }

        /// <summary>The rate stays fixed for the whole span; Simulate cuts spans where it may change.</summary>
        /// <returns>True if hunger reached Min at the end of this span.</returns>
        public bool Advance(double minutes)
        {
            double toThreshold = TimeToNextThreshold;
            if (double.IsPositiveInfinity(toThreshold))
            {
                return false;
            }

            // Snap at the threshold so float error cannot leave a sliver above Min.
            if (minutes >= toThreshold)
            {
                _hunger.Set(_hunger.Min);
                return true;
            }

            _hunger.Add(-(float)(_rate.Value * minutes));
            return false;
        }
    }
}
