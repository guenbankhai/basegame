using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// The single Simulate(delta) the README asks for, used by both stepping and skipping. Each span ends at the next
    /// effect expiry or hunger threshold, so the rate is constant inside a span and both paths give the same result.
    /// </summary>
    public sealed class StatsSimulator
    {
        /// <summary>Spans per call above this mean a threshold that never moves; stop instead of hanging.</summary>
        private const int MaxSpansPerCall = 10000;

        private readonly EffectCollection _effects;
        private readonly FakeHunger _hunger;
        private readonly EffectSettings _onStarved;

        public StatsSimulator(EffectCollection effects, FakeHunger hunger, EffectSettings onStarved)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _hunger = hunger ?? throw new ArgumentNullException(nameof(hunger));
            _onStarved = onStarved ?? throw new ArgumentNullException(nameof(onStarved));
        }

        public double TotalMinutes { get; private set; }
        /// <summary>Spans the last call was cut into; shows where expiries and thresholds split a step.</summary>
        public int LastSpanCount { get; private set; }

        public void Simulate(double delta)
        {
            if (!(delta >= 0) || double.IsInfinity(delta))
            {
                throw new ArgumentException($"Delta must be finite and >= 0, got {delta}.", nameof(delta));
            }

            double left = delta;
            int spans = 0;
            while (left > 0)
            {
                if (++spans > MaxSpansPerCall)
                {
                    throw new InvalidOperationException($"Simulate cut one call into more than {MaxSpansPerCall} spans.");
                }

                double span = Math.Min(left, Math.Min(_effects.TimeUntilNextExpiry, _hunger.TimeToNextThreshold));
                bool starved = _hunger.Advance(span);
                _effects.Advance(span);
                // Applied after Advance: it starts at the end of the span and gets its full duration.
                if (starved)
                {
                    _effects.Apply(_onStarved);
                }

                left -= span;
                TotalMinutes += span;
            }

            LastSpanCount = spans;
        }
    }
}
