using System.Collections.Generic;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Stats, resources and effects of the test entity, built in code until definitions exist. Time unit: game minutes.
    /// </summary>
    public static class PlaygroundCatalog
    {
        public static readonly StatId Vitality = new StatId("Vitality");
        public static readonly StatId MaxHealth = new StatId("MaxHealth");
        public static readonly StatId MaxStamina = new StatId("MaxStamina");
        public static readonly StatId MoveSpeed = new StatId("MoveSpeed");
        public static readonly StatId HungerRate = new StatId("HungerRate");
        public static readonly StatId MaxHunger = new StatId("MaxHunger");

        public static readonly ResourceId Health = new ResourceId("Health");
        public static readonly ResourceId Stamina = new ResourceId("Stamina");
        public static readonly ResourceId Hunger = new ResourceId("Hunger");

        public static readonly EffectId Sick = new EffectId("Sick");

        /// <summary>MaxHealth base 50; DerivedStatLink adds Vitality * 5 on top.</summary>
        public static StatCollectionSettings CreateStatSettings() => new StatCollectionSettings
        {
            Stats = new List<StatSettings>
            {
                new StatSettings(Vitality, 10f) { DisplayName = "Thể lực", Min = 0f },
                new StatSettings(MaxHealth, 50f) { DisplayName = "Máu tối đa", Min = 0f },
                new StatSettings(MaxStamina, 100f) { DisplayName = "Sức bền tối đa", Min = 0f },
                new StatSettings(MoveSpeed, 5f) { DisplayName = "Tốc độ", Min = 0f },
                new StatSettings(HungerRate, 1f) { DisplayName = "Tốc độ đói (/phút)", Min = 0f },
                new StatSettings(MaxHunger, 100f) { DisplayName = "No tối đa", Min = 0f },
            },
            Resources = new List<ResourceSettings>
            {
                new ResourceSettings(Health, MaxHealth) { DisplayName = "Máu" },
                new ResourceSettings(Stamina, MaxStamina) { DisplayName = "Sức bền" },
                new ResourceSettings(Hunger, MaxHunger) { DisplayName = "No" },
            },
        };

        public static List<EffectSettings> CreateEffects()
        {
            var food = new EffectGroupSettings("Food", GroupPolicy.Suppress);
            var tonic = new EffectGroupSettings("Tonic", GroupPolicy.Replace);
            var potion = new EffectGroupSettings("Potion", GroupPolicy.Suppress);
            var body = new EffectGroupSettings("Body", GroupPolicy.Suppress);

            return new List<EffectSettings>
            {
                Effect("Bread", "Bánh mì", StackRule.Refresh, 120, food, 1, Mod(MaxStamina, ModifierType.Flat, 10f)),
                Effect("Rice", "Cơm", StackRule.Refresh, 180, food, 1, Mod(MaxStamina, ModifierType.Flat, 20f)),
                Effect("Feast", "Tiệc (Food rank 2)", StackRule.Refresh, 60, food, 2, Mod(MaxStamina, ModifierType.Flat, 40f)),
                Effect("TonicI", "Thuốc I (Replace, r1)", StackRule.Refresh, 10, tonic, 1, Mod(MoveSpeed, ModifierType.PercentAdd, 0.2f)),
                Effect("TonicII", "Thuốc II (Replace, r2)", StackRule.Refresh, 5, tonic, 2, Mod(MoveSpeed, ModifierType.PercentAdd, 0.3f)),
                Effect("PotionI", "Thuốc I (Suppress, r1)", StackRule.Refresh, 10, potion, 1, Mod(MoveSpeed, ModifierType.PercentAdd, 0.2f)),
                Effect("PotionII", "Thuốc II (Suppress, r2)", StackRule.Refresh, 5, potion, 2, Mod(MoveSpeed, ModifierType.PercentAdd, 0.3f)),
                Effect(Sick.Value, "Ốm (đói x2)", StackRule.Refresh, 45, null, 0, Mod(HungerRate, ModifierType.PercentAdd, 1f)),
                Effect("Exhausted", "Kiệt sức (stack 3)", StackRule.Stack, 30, null, 0, Mod(MoveSpeed, ModifierType.PercentMult, -0.2f), maxStacks: 3),
                Effect("Fortify", "Gia cố +20 máu (stack 2)", StackRule.Stack, 30, null, 0, Mod(MaxHealth, ModifierType.Flat, 20f), maxStacks: 2),
                Effect("Poison", "Độc -6 thể lực (Extend 60)", StackRule.Extend, 20, null, 0, Mod(Vitality, ModifierType.Flat, -6f), maxDuration: 60),
                Effect("Blessing", "Phước lành (vô hạn, Ignore)", StackRule.Ignore, double.PositiveInfinity, null, 0, Mod(Vitality, ModifierType.Flat, 2f)),
                // Forbidden by the derived-stat rule on purpose: shows the mid-command clamp the README warns about.
                Effect("Vigor", "Sinh lực +4 thể lực (Body r1, vi phạm)", StackRule.Refresh, 10, body, 1, Mod(Vitality, ModifierType.Flat, 4f)),
                Effect("Bulwark", "Thành lũy +20 máu (Body r2, vi phạm)", StackRule.Refresh, 5, body, 2, Mod(MaxHealth, ModifierType.Flat, 20f)),
            };
        }

        private static EffectModifierSettings Mod(StatId stat, ModifierType type, float value) =>
            new EffectModifierSettings(stat, type, value);

        private static EffectSettings Effect(
            string id, string displayName, StackRule rule, double duration, EffectGroupSettings group, int rank,
            EffectModifierSettings modifier, int maxStacks = 1, double maxDuration = double.PositiveInfinity) =>
            new EffectSettings(new EffectId(id), rule)
            {
                DisplayName = displayName,
                Duration = duration,
                MaxDuration = double.IsPositiveInfinity(maxDuration) ? duration : maxDuration,
                MaxStacks = maxStacks,
                Group = group,
                Rank = rank,
                Modifiers = new List<EffectModifierSettings> { modifier },
            };
    }
}
