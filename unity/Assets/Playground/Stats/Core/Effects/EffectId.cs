using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>Key of an effect; compared ordinally. default(EffectId) is invalid and rejected by every command.</summary>
    public readonly struct EffectId : IEquatable<EffectId>
    {
        public EffectId(string value)
        {
            if (!IdFormat.IsValid(value))
            {
                throw new ArgumentException($"Effect ID '{value}' {IdFormat.Rule}.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        internal bool IsValid => Value != null;

        public bool Equals(EffectId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EffectId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "<invalid>";

        public static bool operator ==(EffectId left, EffectId right) => left.Equals(right);
        public static bool operator !=(EffectId left, EffectId right) => !left.Equals(right);
    }
}
