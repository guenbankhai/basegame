using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>Key of a stat; compared ordinally. default(StatId) is invalid and rejected by every command.</summary>
    public readonly struct StatId : IEquatable<StatId>
    {
        public StatId(string value)
        {
            if (!IdFormat.IsValid(value))
            {
                throw new ArgumentException($"Stat ID '{value}' {IdFormat.Rule}.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        internal bool IsValid => Value != null;

        public bool Equals(StatId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StatId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "<invalid>";

        public static bool operator ==(StatId left, StatId right) => left.Equals(right);
        public static bool operator !=(StatId left, StatId right) => !left.Equals(right);
    }
}
