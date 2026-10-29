using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>Immutable; compared by reference, so the same values from two sources are two modifiers.</summary>
    public sealed class Modifier
    {
        public Modifier(ModifierType type, float value, object source)
        {
            if (!Enum.IsDefined(typeof(ModifierType), type))
            {
                throw new ArgumentException($"Unknown modifier type {type}.", nameof(type));
            }

            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Modifier value must be finite.", nameof(value));
            }

            // Below -1 would flip the sign of the stat.
            if (type == ModifierType.PercentMult && value < -1f)
            {
                throw new ArgumentException($"PercentMult must be >= -1, got {value}.", nameof(value));
            }

            Type = type;
            Value = value;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public ModifierType Type { get; }
        public float Value { get; }
        public object Source { get; }

        public override string ToString() => $"{Type} {Value:+0.###;-0.###}";
    }
}
