using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>Key of a resource; compared ordinally. default(ResourceId) is invalid and rejected by every command.</summary>
    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        public ResourceId(string value)
        {
            if (!IdFormat.IsValid(value))
            {
                throw new ArgumentException($"Resource ID '{value}' {IdFormat.Rule}.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        internal bool IsValid => Value != null;

        public bool Equals(ResourceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ResourceId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "<invalid>";

        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
    }
}
