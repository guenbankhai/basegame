namespace BaseGame.Playground.Stats
{
    public sealed class ResourceSettings
    {
        public ResourceSettings()
        {
        }

        public ResourceSettings(ResourceId id, StatId maxStat)
        {
            Id = id;
            MaxStat = maxStat;
        }

        public ResourceId Id { get; set; }
        /// <summary>Null, empty or whitespace falls back to Id.Value.</summary>
        public string DisplayName { get; set; }
        public StatId MaxStat { get; set; }
        public float Min { get; set; }
        /// <summary>Current at creation: Min + InitialFraction * (Max - Min).</summary>
        public float InitialFraction { get; set; } = 1f;
    }
}
