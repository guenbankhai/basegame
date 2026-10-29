namespace BaseGame.Playground.Stats
{
    /// <summary>One modifier of an effect; the instance applies it with Value * Stacks.</summary>
    public sealed class EffectModifierSettings
    {
        public EffectModifierSettings()
        {
        }

        public EffectModifierSettings(StatId stat, ModifierType type, float value)
        {
            Stat = stat;
            Type = type;
            Value = value;
        }

        public StatId Stat { get; set; }
        public ModifierType Type { get; set; }
        public float Value { get; set; }

        internal EffectModifierSettings Clone() => new EffectModifierSettings(Stat, Type, Value);
    }
}
