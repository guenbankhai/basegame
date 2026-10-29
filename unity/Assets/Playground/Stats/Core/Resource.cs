using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// A value spent and refilled directly, always in [Min, Max] with Max = max(MaxStat.Value, Min). When max changes,
    /// Current keeps its absolute amount and is only clamped if it no longer fits.
    /// </summary>
    public sealed class Resource
    {
        private readonly StatCollection _owner;
        private float _reportedCurrent;
        private int _reportCount;

        internal Resource(StatCollection owner, int index, ResourceSettings settings, Stat maxStat)
        {
            _owner = owner;
            Index = index;
            Id = settings.Id;
            DisplayName = string.IsNullOrWhiteSpace(settings.DisplayName) ? settings.Id.Value : settings.DisplayName;
            Min = settings.Min;
            MaxStat = maxStat;
            InitialFraction = settings.InitialFraction;
            Current = InitialValue;
            _reportedCurrent = Current;
        }

        public ResourceId Id { get; }
        public string DisplayName { get; }
        public float Current { get; private set; }
        public float Min { get; }
        public float Max => Math.Max(MaxStat.Value, Min);
        /// <summary>(Current - Min) / (Max - Min); 1 when Max == Min.</summary>
        public float Normalized => Max == Min ? 1f : (Current - Min) / (Max - Min);
        public Stat MaxStat { get; }

        internal int Index { get; }
        internal float InitialFraction { get; }
        internal float InitialValue => Min + InitialFraction * (Max - Min);

        /// <summary>(old, new); also when a shrinking max clamps Current.</summary>
        public event Action<float, float> ValueChanged;
        /// <summary>Current just reached Min from above.</summary>
        public event Action Depleted;
        /// <summary>Current just rose to Max; a max shrinking onto Current does not count.</summary>
        public event Action Filled;

        /// <returns>The amount actually applied after clamping (at 5 with Min 0, Add(-20) returns -5).</returns>
        public float Add(float delta)
        {
            _owner.ThrowIfTooDeep();
            if (!float.IsFinite(delta))
            {
                throw new ArgumentException("Delta must be finite.", nameof(delta));
            }

            if (delta == 0f)
            {
                return 0f;
            }

            float before = Current;
            var change = new StatChangeSet();
            SetCurrentRaw(Current + delta, change);
            float applied = Current - before;
            _owner.Commit(change, null);
            return applied;
        }

        /// <summary>Clamped into [Min, Max].</summary>
        public void Set(float value)
        {
            _owner.ThrowIfTooDeep();
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Value must be finite.", nameof(value));
            }

            var change = new StatChangeSet();
            SetCurrentRaw(value, change);
            _owner.Commit(change, null);
        }

        public override string ToString() => $"{Id} {Current}/{Max}";

        internal void SetCurrentRaw(float value, StatChangeSet change)
        {
            float clamped = Math.Clamp(value, Min, Max);
            if (clamped != Current)
            {
                Current = clamped;
                change.Add(this);
            }
        }

        internal void ClampToRange(StatChangeSet change) => SetCurrentRaw(Current, change);

        /// <summary>After a silent restore: the restored value becomes the baseline for the next event.</summary>
        internal void ResetReported() => _reportedCurrent = Current;

        internal void RaiseIfChanged()
        {
            if (Current == _reportedCurrent)
            {
                return;
            }

            float old = _reportedCurrent;
            float current = Current;
            _reportedCurrent = current;
            int report = ++_reportCount;
            EventInvoker.Invoke(ValueChanged, old, current);

            // A ValueChanged handler moved Current: its nested event already reported the threshold, if any.
            if (report != _reportCount)
            {
                return;
            }

            if (current == Min && old > Min)
            {
                EventInvoker.Invoke(Depleted);
            }
            else if (current == Max && old < current)
            {
                EventInvoker.Invoke(Filled);
            }
        }
    }
}
