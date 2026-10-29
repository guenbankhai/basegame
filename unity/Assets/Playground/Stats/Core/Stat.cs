using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Value = clamp((Base + sum Flat) * (1 + sum PercentAdd) * product (1 + PercentMult), Min, Max), recomputed from
    /// scratch on every change so float error never builds up.
    /// </summary>
    public sealed class Stat
    {
        private readonly StatCollection _owner;
        private readonly List<Modifier> _modifiers = new List<Modifier>();
        // Last value listeners were told; a pending event whose value a nested command already reported is dropped.
        private float _reportedValue;

        internal Stat(StatCollection owner, int index, StatSettings settings)
        {
            _owner = owner;
            Index = index;
            Id = settings.Id;
            DisplayName = string.IsNullOrWhiteSpace(settings.DisplayName) ? settings.Id.Value : settings.DisplayName;
            DefaultBaseValue = settings.BaseValue;
            BaseValue = settings.BaseValue;
            Min = settings.Min;
            Max = settings.Max;
            Modifiers = new ReadOnlyCollection<Modifier>(_modifiers);
            Value = Calculate();
            _reportedValue = Value;
        }

        public StatId Id { get; }
        public string DisplayName { get; }
        public float BaseValue { get; private set; }
        public float Value { get; private set; }
        public float Min { get; }
        public float Max { get; }
        public IReadOnlyList<Modifier> Modifiers { get; }

        internal int Index { get; }
        internal float DefaultBaseValue { get; }

        /// <summary>(old, new); only when the final value changes (!=).</summary>
        public event Action<float, float> ValueChanged;

        /// <summary>Any finite value, even outside [Min, Max]; only the final value is clamped.</summary>
        public void SetBase(float value)
        {
            _owner.ThrowIfTooDeep();
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Base value must be finite.", nameof(value));
            }

            var change = new StatChangeSet();
            SetBaseRaw(value, change);
            _owner.Commit(change, null);
        }

        /// <returns>False if this exact modifier is already on the stat.</returns>
        public bool AddModifier(Modifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            _owner.ThrowIfTooDeep();
            if (_modifiers.Contains(modifier))
            {
                return false;
            }

            var change = new StatChangeSet();
            AddModifierRaw(modifier, change);
            _owner.Commit(change, null);
            return true;
        }

        public bool RemoveModifier(Modifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            _owner.ThrowIfTooDeep();
            if (!_modifiers.Contains(modifier))
            {
                return false;
            }

            var change = new StatChangeSet();
            RemoveModifierRaw(modifier, change);
            _owner.Commit(change, null);
            return true;
        }

        /// <summary>Swaps in place with one recalculation, so a resource using this stat as max is not clamped midway.</summary>
        /// <returns>False if oldModifier is not on the stat or newModifier already is (including old == new).</returns>
        public bool ReplaceModifier(Modifier oldModifier, Modifier newModifier)
        {
            if (oldModifier == null)
            {
                throw new ArgumentNullException(nameof(oldModifier));
            }

            if (newModifier == null)
            {
                throw new ArgumentNullException(nameof(newModifier));
            }

            _owner.ThrowIfTooDeep();
            if (!_modifiers.Contains(oldModifier) || _modifiers.Contains(newModifier))
            {
                return false;
            }

            var change = new StatChangeSet();
            ReplaceModifierRaw(oldModifier, newModifier, change);
            _owner.Commit(change, null);
            return true;
        }

        public override string ToString() => $"{Id} {Value} (base {BaseValue})";

        internal void SetBaseRaw(float value, StatChangeSet change)
        {
            BaseValue = value;
            Recalculate(change);
        }

        internal void AddModifierRaw(Modifier modifier, StatChangeSet change)
        {
            _modifiers.Add(modifier);
            Recalculate(change);
        }

        internal void RemoveModifierRaw(Modifier modifier, StatChangeSet change)
        {
            if (_modifiers.Remove(modifier))
            {
                Recalculate(change);
            }
        }

        // Tolerates a missing old modifier (outside code removed it by source), so effect bookkeeping self-heals.
        internal void ReplaceModifierRaw(Modifier oldModifier, Modifier newModifier, StatChangeSet change)
        {
            int index = _modifiers.IndexOf(oldModifier);
            if (index < 0)
            {
                _modifiers.Add(newModifier);
            }
            else
            {
                _modifiers[index] = newModifier;
            }

            Recalculate(change);
        }

        internal int RemoveAllFromSourceRaw(object source, StatChangeSet change)
        {
            int removed = _modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
            if (removed > 0)
            {
                Recalculate(change);
            }

            return removed;
        }

        internal void RaiseIfChanged()
        {
            if (Value == _reportedValue)
            {
                return;
            }

            float old = _reportedValue;
            _reportedValue = Value;
            EventInvoker.Invoke(ValueChanged, old, Value);
        }

        private void Recalculate(StatChangeSet change)
        {
            float value = Calculate();
            if (value != Value)
            {
                Value = value;
                change.Add(this);
            }
        }

        private float Calculate()
        {
            float flat = 0f;
            float percentAdd = 0f;
            float percentMult = 1f;
            foreach (Modifier modifier in _modifiers)
            {
                switch (modifier.Type)
                {
                    case ModifierType.Flat:
                        flat += modifier.Value;
                        break;
                    case ModifierType.PercentAdd:
                        percentAdd += modifier.Value;
                        break;
                    case ModifierType.PercentMult:
                        percentMult *= 1f + modifier.Value;
                        break;
                }
            }

            float unclamped = (BaseValue + flat) * (1f + percentAdd) * percentMult;
            return Math.Clamp(unclamped, Min, Max);
        }
    }
}
