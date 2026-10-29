namespace BaseGame.Playground.Stats
{
    public sealed class StatSettings
    {
        public StatSettings()
        {
        }

        public StatSettings(StatId id, float baseValue)
        {
            Id = id;
            BaseValue = baseValue;
        }

        public StatId Id { get; set; }
        /// <summary>Null, empty or whitespace falls back to Id.Value.</summary>
        public string DisplayName { get; set; }
        public float BaseValue { get; set; }
        public float Min { get; set; } = float.NegativeInfinity;
        public float Max { get; set; } = float.PositiveInfinity;
    }
}
