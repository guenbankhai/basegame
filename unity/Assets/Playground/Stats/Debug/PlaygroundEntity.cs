using System.Collections.Generic;

namespace BaseGame.Playground.Stats
{
    /// <summary>One test entity: collections, the Vitality → MaxHealth link, fake hunger and its Simulate.</summary>
    public sealed class PlaygroundEntity
    {
        private readonly Dictionary<EffectId, EffectSettings> _effectsById = new Dictionary<EffectId, EffectSettings>();

        public PlaygroundEntity(IEnumerable<EffectSettings> effectCatalog)
        {
            foreach (EffectSettings settings in effectCatalog)
            {
                _effectsById.Add(settings.Id, settings);
            }

            Stats = new StatCollection(PlaygroundCatalog.CreateStatSettings());
            Effects = new EffectCollection(Stats);
            Derived = new DerivedStatLink(Stats.GetStat(PlaygroundCatalog.Vitality), Stats.GetStat(PlaygroundCatalog.MaxHealth), 5f);
            Hunger = new FakeHunger(Stats.GetResource(PlaygroundCatalog.Hunger), Stats.GetStat(PlaygroundCatalog.HungerRate));
            Simulator = new StatsSimulator(Effects, Hunger, _effectsById[PlaygroundCatalog.Sick]);

            // Health was created against MaxHealth before the link added Vitality * 5.
            Resource health = Stats.GetResource(PlaygroundCatalog.Health);
            health.Set(health.Max);
        }

        public StatCollection Stats { get; }
        public EffectCollection Effects { get; }
        public DerivedStatLink Derived { get; }
        public FakeHunger Hunger { get; }
        public StatsSimulator Simulator { get; }

        /// <summary>The resolve callback for EffectCollection.Restore.</summary>
        public EffectSettings ResolveEffect(EffectId id) => _effectsById.TryGetValue(id, out EffectSettings settings) ? settings : null;

        /// <summary>Order the README requires: modifier sources first, stats last.</summary>
        public void Restore(StatsSnapshot stats, EffectsSnapshot effects)
        {
            Effects.Restore(effects, ResolveEffect);
            Stats.Restore(stats);
        }
    }
}
