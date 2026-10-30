using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BaseGame.Playground.Stats.Tests
{
    public class EffectCollectionTests
    {
        private static readonly StatId MaxHealth = new StatId("MaxHealth");
        private static readonly StatId Speed = new StatId("Speed");
        private static readonly ResourceId Health = new ResourceId("Health");

        private StatCollection _stats;
        private EffectCollection _effects;
        private List<string> _events;

        [SetUp]
        public void SetUp()
        {
            _stats = new StatCollection(new StatCollectionSettings
            {
                Stats = { new StatSettings(MaxHealth, 100f), new StatSettings(Speed, 5f) },
                Resources = { new ResourceSettings(Health, MaxHealth) },
            });
            _effects = new EffectCollection(_stats);
            _events = new List<string>();
            _effects.Applied += e => _events.Add($"Applied {e.Id}");
            _effects.Stacked += e => _events.Add($"Stacked {e.Id}");
            _effects.Refreshed += e => _events.Add($"Refreshed {e.Id}");
            _effects.Suppressed += e => _events.Add($"Suppressed {e.Id}");
            _effects.Resumed += e => _events.Add($"Resumed {e.Id}");
            _effects.Removed += (e, reason) => _events.Add($"Removed {e.Id} {reason}");
        }

        [Test]
        public void Apply_StackOnMaxHealth_KeepsCurrentWithoutMidwayClamp()
        {
            EffectSettings fortify = Effect("Fortify", StackRule.Stack, 30, null, 0, MaxHealth, ModifierType.Flat, 20f);
            fortify.MaxStacks = 2;
            Resource health = _stats.GetResource(Health);
            _effects.Apply(fortify);
            health.Set(115f);

            _effects.Apply(fortify);

            Assert.That(health.Current, Is.EqualTo(115f));
            Assert.That(health.Max, Is.EqualTo(140f));
        }

        [Test]
        public void Apply_RefreshWithStaleMaxDurationAndMaxStacks_IgnoresThem()
        {
            EffectSettings slow = Effect("Slow", StackRule.Refresh, 180, null, 0, Speed, ModifierType.PercentMult, -0.5f);
            slow.MaxDuration = 120;
            slow.MaxStacks = 3;

            _effects.Apply(slow);

            Assert.That(_stats.GetStat(Speed).Value, Is.EqualTo(2.5f));
        }

        [Test]
        public void Apply_ExtendWithMaxDurationBelowDuration_Throws()
        {
            EffectSettings poison = Effect("Poison", StackRule.Extend, 90, null, 0, Speed, ModifierType.Flat, -1f);
            poison.MaxDuration = 60;

            Assert.Throws<System.ArgumentException>(() => _effects.Apply(poison));
            Assert.That(_effects.Instances, Is.Empty);
        }

        [Test]
        public void Apply_SuppressedSameRankReapplied_LoserFirstThenWinner()
        {
            var food = new EffectGroupSettings("Food", GroupPolicy.Suppress);
            EffectSettings bread = Effect("Bread", StackRule.Refresh, 60, food, 1, Speed, ModifierType.Flat, 1f);
            EffectSettings rice = Effect("Rice", StackRule.Refresh, 60, food, 1, Speed, ModifierType.Flat, 2f);
            _effects.Apply(bread);
            _effects.Apply(rice);
            _events.Clear();

            _effects.Apply(bread);

            Assert.That(_events, Is.EqualTo(new[] { "Suppressed Rice", "Refreshed Bread", "Resumed Bread" }));
            Assert.That(_stats.GetStat(Speed).Value, Is.EqualTo(6f));
        }

        [Test]
        public void Apply_LowerRankInReplaceGroup_ReturnsNullAndChangesNothing()
        {
            var tonic = new EffectGroupSettings("Tonic", GroupPolicy.Replace);
            _effects.Apply(Effect("TonicII", StackRule.Refresh, 5, tonic, 2, Speed, ModifierType.PercentAdd, 0.3f));
            _events.Clear();

            EffectInstance result = _effects.Apply(Effect("TonicI", StackRule.Refresh, 10, tonic, 1, Speed, ModifierType.PercentAdd, 0.2f));

            Assert.That(result, Is.Null);
            Assert.That(_effects.Instances.Count, Is.EqualTo(1));
            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void Advance_SuppressedResumesAndExpiresInOneCall_OnlyTwoRemoved()
        {
            var potion = new EffectGroupSettings("Potion", GroupPolicy.Suppress);
            _effects.Apply(Effect("PotionII", StackRule.Refresh, 5, potion, 2, Speed, ModifierType.PercentAdd, 0.3f));
            _effects.Apply(Effect("PotionI", StackRule.Refresh, 10, potion, 1, Speed, ModifierType.PercentAdd, 0.2f));
            _events.Clear();

            _effects.Advance(10);

            Assert.That(_events, Is.EqualTo(new[] { "Removed PotionII Expired", "Removed PotionI Expired" }));
            Assert.That(_stats.GetStat(Speed).Value, Is.EqualTo(5f));
        }

        [Test]
        public void Advance_TenOnceOrOneTenTimes_SameSnapshotAndStats()
        {
            var potion = new EffectGroupSettings("Potion", GroupPolicy.Suppress);
            var settings = new[]
            {
                Effect("PotionII", StackRule.Refresh, 5, potion, 2, Speed, ModifierType.PercentAdd, 0.3f),
                Effect("PotionI", StackRule.Refresh, 12, potion, 1, Speed, ModifierType.PercentAdd, 0.2f),
                Effect("Slow", StackRule.Refresh, 7, null, 0, Speed, ModifierType.PercentMult, -0.5f),
            };
            var other = new EffectCollection(new StatCollection(new StatCollectionSettings
            {
                Stats = { new StatSettings(MaxHealth, 100f), new StatSettings(Speed, 5f) },
                Resources = { new ResourceSettings(Health, MaxHealth) },
            }));
            foreach (EffectSettings s in settings)
            {
                _effects.Apply(s);
                other.Apply(s);
            }

            _effects.Advance(10);
            for (int i = 0; i < 10; i++)
            {
                other.Advance(1);
            }

            Assert.That(JsonUtility.ToJson(other.CaptureSnapshot()), Is.EqualTo(JsonUtility.ToJson(_effects.CaptureSnapshot())));
            Assert.That(_stats.GetStat(Speed).Value, Is.EqualTo(6f).Within(1e-5f));
        }

        [Test]
        public void Restore_ResolveReturnsOtherId_RejectedAndStateUnchanged()
        {
            EffectSettings a = Effect("A", StackRule.Refresh, 10, null, 0, Speed, ModifierType.Flat, 1f);
            EffectSettings b = Effect("B", StackRule.Refresh, 10, null, 0, Speed, ModifierType.Flat, 2f);
            _effects.Apply(a);
            _effects.Apply(b);
            EffectsSnapshot snapshot = _effects.CaptureSnapshot();
            _effects.Remove(a.Id);

            Assert.Throws<System.ArgumentException>(() => _effects.Restore(snapshot, _ => b));
            Assert.That(_effects.Instances.Count, Is.EqualTo(1));
            Assert.That(_stats.GetStat(Speed).Value, Is.EqualTo(7f));
        }

        [Test]
        public void RestoreThenStatsRestore_EffectOnMaxHealth_KeepsCurrent()
        {
            EffectSettings fortify = Effect("Fortify", StackRule.Refresh, 30, null, 0, MaxHealth, ModifierType.Flat, 20f);
            _effects.Apply(fortify);
            _stats.GetResource(Health).Set(115f);
            EffectsSnapshot effectsSnapshot = _effects.CaptureSnapshot();
            StatsSnapshot statsSnapshot = _stats.CaptureSnapshot();

            var stats = new StatCollection(new StatCollectionSettings
            {
                Stats = { new StatSettings(MaxHealth, 100f), new StatSettings(Speed, 5f) },
                Resources = { new ResourceSettings(Health, MaxHealth) },
            });
            var effects = new EffectCollection(stats);
            effects.Restore(effectsSnapshot, _ => fortify);
            stats.Restore(statsSnapshot);

            Assert.That(stats.GetResource(Health).Current, Is.EqualTo(115f));
            Assert.That(stats.GetResource(Health).Max, Is.EqualTo(120f));
            Assert.That(effects.TryGet(fortify.Id, out EffectInstance instance) && instance.Remaining == 30, Is.True);
        }

        private static EffectSettings Effect(
            string id, StackRule rule, double duration, EffectGroupSettings group, int rank, StatId stat, ModifierType type, float value) =>
            new EffectSettings(new EffectId(id), rule)
            {
                Duration = duration,
                MaxDuration = duration,
                Group = group,
                Rank = rank,
                Modifiers = new List<EffectModifierSettings> { new EffectModifierSettings(stat, type, value) },
            };
    }
}
