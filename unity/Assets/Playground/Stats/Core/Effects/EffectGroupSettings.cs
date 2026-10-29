namespace BaseGame.Playground.Stats
{
    /// <summary>Effects with the same group Id exclude each other; only the highest rank has an effect.</summary>
    public sealed class EffectGroupSettings
    {
        public EffectGroupSettings()
        {
        }

        public EffectGroupSettings(string id, GroupPolicy policy)
        {
            Id = id;
            Policy = policy;
        }

        public string Id { get; set; }
        public GroupPolicy Policy { get; set; } = GroupPolicy.Replace;

        internal EffectGroupSettings Clone() => new EffectGroupSettings(Id, Policy);
    }
}
