using System.Collections.Generic;
using NUnit.Framework;

namespace BaseGame.Playground.Stats.Tests
{
    public class StatCollectionTests
    {
        private static readonly StatId Speed = new StatId("Speed");
        private static readonly StatId Vitality = new StatId("Vitality");
        private static readonly StatId MaxHealth = new StatId("MaxHealth");
        private static readonly ResourceId Health = new ResourceId("Health");

        [Test]
        public void StatFormula_ThreeTiers_MatchesReadmeExample()
        {
            var stats = new StatCollection(new StatCollectionSettings { Stats = { new StatSettings(Speed, 5f) } });
            Stat speed = stats.GetStat(Speed);
            var source = new object();

            speed.AddModifier(new Modifier(ModifierType.Flat, 1f, source));
            speed.AddModifier(new Modifier(ModifierType.PercentAdd, -0.3f, source));
            speed.AddModifier(new Modifier(ModifierType.PercentMult, -0.5f, source));

            Assert.That(speed.Value, Is.EqualTo(2.1f).Within(1e-5f));
        }

        [Test]
        public void StatFormula_ClampOnlyFinalValue_RemovingModifierRestoresBase()
        {
            var stats = new StatCollection(new StatCollectionSettings { Stats = { new StatSettings(Speed, 5f) { Min = 0f } } });
            Stat speed = stats.GetStat(Speed);
            var modifier = new Modifier(ModifierType.PercentAdd, -1.5f, new object());

            speed.AddModifier(modifier);
            Assert.That(speed.Value, Is.EqualTo(0f));

            speed.RemoveModifier(modifier);
            Assert.That(speed.Value, Is.EqualTo(5f));
        }

        [Test]
        public void ReplaceModifier_DerivedStat_KeepsHealthWithoutMidwayClamp()
        {
            StatCollection stats = CreateDerivedEntity(out _);
            Stat vitality = stats.GetStat(Vitality);
            Resource health = stats.GetResource(Health);
            vitality.SetBase(20f);
            health.Set(140f);

            vitality.SetBase(21f);

            Assert.That(health.Current, Is.EqualTo(140f));
            Assert.That(health.Max, Is.EqualTo(155f));
        }

        [Test]
        public void RemoveAllFromSourceInHandler_NestedChange_ListenerGetsLastReportedOldAndFinalValue()
        {
            // Vitality is registered before MaxHealth, so its derived handler runs while the outer events still wait.
            StatCollection stats = CreateDerivedEntity(out _);
            Stat vitality = stats.GetStat(Vitality);
            Stat maxHealth = stats.GetStat(MaxHealth);
            Resource health = stats.GetResource(Health);
            vitality.SetBase(20f);
            var armor = new object();
            vitality.AddModifier(new Modifier(ModifierType.Flat, 4f, armor));
            maxHealth.AddModifier(new Modifier(ModifierType.Flat, 10f, armor));
            health.Set(health.Max);
            Assert.That(health.Current, Is.EqualTo(180f));

            var maxEvents = new List<(float, float)>();
            var healthEvents = new List<(float, float)>();
            maxHealth.ValueChanged += (o, n) => maxEvents.Add((o, n));
            health.ValueChanged += (o, n) => healthEvents.Add((o, n));

            stats.RemoveAllFromSource(armor);

            Assert.That(maxEvents, Is.EqualTo(new[] { (180f, 150f) }));
            Assert.That(healthEvents, Is.EqualTo(new[] { (180f, 150f) }));
            Assert.That(health.Current, Is.EqualTo(150f));
        }

        [Test]
        public void SetInHandler_NestedChangeBackToReportedValue_RaisesNothingMore()
        {
            var stats = new StatCollection(new StatCollectionSettings
            {
                Stats = { new StatSettings(Speed, 1f), new StatSettings(MaxHealth, 90f) },
                Resources = { new ResourceSettings(Health, MaxHealth) },
            });
            Stat speed = stats.GetStat(Speed);
            Stat maxHealth = stats.GetStat(MaxHealth);
            Resource health = stats.GetResource(Health);
            var gear = new object();
            speed.AddModifier(new Modifier(ModifierType.Flat, 1f, gear));
            maxHealth.AddModifier(new Modifier(ModifierType.Flat, 10f, gear));
            health.Set(100f);

            // Speed raises first and undoes both the max drop and the clamp that are still waiting to be reported.
            speed.ValueChanged += (_, __) =>
            {
                maxHealth.AddModifier(new Modifier(ModifierType.Flat, 10f, new object()));
                health.Set(100f);
            };
            var maxEvents = new List<(float, float)>();
            var healthEvents = new List<(float, float)>();
            maxHealth.ValueChanged += (o, n) => maxEvents.Add((o, n));
            health.ValueChanged += (o, n) => healthEvents.Add((o, n));

            stats.RemoveAllFromSource(gear);

            Assert.That(maxHealth.Value, Is.EqualTo(100f));
            Assert.That(health.Current, Is.EqualTo(100f));
            Assert.That(maxEvents, Is.Empty);
            Assert.That(healthEvents, Is.Empty);
        }

        [Test]
        public void Restore_DerivedStatIntoNewEntity_KeepsHealth140Of150()
        {
            StatCollection saved = CreateDerivedEntity(out _);
            saved.GetStat(Vitality).SetBase(20f);
            saved.GetResource(Health).Set(140f);
            StatsSnapshot snapshot = saved.CaptureSnapshot();

            StatCollection loaded = CreateDerivedEntity(out _);
            int healthEvents = 0;
            loaded.GetResource(Health).ValueChanged += (_, __) => healthEvents++;
            loaded.Restore(snapshot);

            Assert.That(loaded.GetResource(Health).Current, Is.EqualTo(140f));
            Assert.That(loaded.GetResource(Health).Max, Is.EqualTo(150f));
            Assert.That(healthEvents, Is.Zero);
        }

        /// <summary>MaxHealth = 50 + Vitality * 5, Vitality default 10, health starts full.</summary>
        private static StatCollection CreateDerivedEntity(out DerivedStatLink link)
        {
            var stats = new StatCollection(new StatCollectionSettings
            {
                Stats = { new StatSettings(Vitality, 10f), new StatSettings(MaxHealth, 50f) },
                Resources = { new ResourceSettings(Health, MaxHealth) },
            });
            link = new DerivedStatLink(stats.GetStat(Vitality), stats.GetStat(MaxHealth), 5f);
            Resource health = stats.GetResource(Health);
            health.Set(health.Max);
            return stats;
        }
    }
}
