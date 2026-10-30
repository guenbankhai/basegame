using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BaseGame.Playground.Stats.Tests
{
    public class StatProfileTests
    {
        private static readonly StatId MaxHealth = new StatId("MaxHealth");

        private readonly List<Object> _assets = new List<Object>();
        private StatDefinition _maxHealth;
        private ResourceDefinition _health;

        [SetUp]
        public void SetUp()
        {
            _maxHealth = Create<StatDefinition>("MaxHealth");
            var stat = new SerializedObject(_maxHealth);
            stat.FindProperty("_baseValue").floatValue = 100f;
            stat.ApplyModifiedPropertiesWithoutUndo();

            _health = Create<ResourceDefinition>("Health");
            var resource = new SerializedObject(_health);
            resource.FindProperty("_maxStat").objectReferenceValue = _maxHealth;
            resource.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void CreateSettings_MaxStatNotListed_AddsItWithDefinitionBase()
        {
            StatProfile profile = Profile(overrideMaxHealth: null);

            var stats = new StatCollection(profile.CreateSettings());

            Assert.That(stats.Stats, Has.Count.EqualTo(1));
            Assert.That(stats.GetStat(MaxHealth).BaseValue, Is.EqualTo(100f));
            Assert.That(stats.GetResource(new ResourceId("Health")).Max, Is.EqualTo(100f));
        }

        [Test]
        public void CreateSettings_MaxStatListedWithOverride_ListedEntryWins()
        {
            StatProfile profile = Profile(overrideMaxHealth: 150f);

            var stats = new StatCollection(profile.CreateSettings());

            Assert.That(stats.Stats, Has.Count.EqualTo(1));
            Assert.That(stats.GetStat(MaxHealth).BaseValue, Is.EqualTo(150f));
        }

        [Test]
        public void CreateSettings_EmptyMaxStat_CollectionRejects()
        {
            var resource = new SerializedObject(_health);
            resource.FindProperty("_maxStat").objectReferenceValue = null;
            resource.ApplyModifiedPropertiesWithoutUndo();
            StatProfile profile = Profile(overrideMaxHealth: null);

            Assert.Throws<System.ArgumentException>(() => new StatCollection(profile.CreateSettings()));
        }

        /// <summary>Profile with Health; MaxHealth is listed with an override only when one is given.</summary>
        private StatProfile Profile(float? overrideMaxHealth)
        {
            StatProfile profile = Create<StatProfile>("TestProfile");
            var serialized = new SerializedObject(profile);
            SerializedProperty resources = serialized.FindProperty("_resources");
            resources.arraySize = 1;
            resources.GetArrayElementAtIndex(0).objectReferenceValue = _health;
            if (overrideMaxHealth.HasValue)
            {
                SerializedProperty stats = serialized.FindProperty("_stats");
                stats.arraySize = 1;
                SerializedProperty entry = stats.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("_stat").objectReferenceValue = _maxHealth;
                entry.FindPropertyRelative("_overrideBase").boolValue = true;
                entry.FindPropertyRelative("_baseValue").floatValue = overrideMaxHealth.Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private T Create<T>(string assetName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            _assets.Add(asset);
            return asset;
        }
    }
}
