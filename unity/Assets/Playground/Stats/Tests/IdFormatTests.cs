using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BaseGame.Playground.Stats.Tests
{
    public class IdFormatTests
    {
        [TestCase("MoveSpeed")]
        [TestCase("TonicII")]
        [TestCase("A1")]
        public void Constructors_PascalCase_Accepted(string id)
        {
            Assert.That(new StatId(id).Value, Is.EqualTo(id));
            Assert.That(new ResourceId(id).Value, Is.EqualTo(id));
            Assert.That(new EffectId(id).Value, Is.EqualTo(id));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("moveSpeed")]
        [TestCase("Move Speed")]
        [TestCase("Move_Speed")]
        [TestCase("1Move")]
        [TestCase("MoveSpeed(Clone)")]
        [TestCase("Máu")]
        public void Constructors_BadFormat_Throw(string id)
        {
            Assert.Throws<ArgumentException>(() => new StatId(id));
            Assert.Throws<ArgumentException>(() => new ResourceId(id));
            Assert.Throws<ArgumentException>(() => new EffectId(id));
        }

        [Test]
        public void Apply_GroupIdBadFormat_Throws()
        {
            var effects = new EffectCollection(new StatCollection(new StatCollectionSettings()));
            var settings = new EffectSettings(new EffectId("Bread"), StackRule.Refresh)
            {
                Group = new EffectGroupSettings("food group", GroupPolicy.Suppress),
            };

            Assert.Throws<ArgumentException>(() => effects.Apply(settings));
            Assert.That(effects.Instances, Is.Empty);
        }

        [Test]
        public void Restore_SnapshotIdBadFormat_Rejected()
        {
            var stats = new StatCollection(new StatCollectionSettings());
            var snapshot = new StatsSnapshot
            {
                Stats = new[] { new StatsSnapshot.StatEntry("move speed", 1f) },
                Resources = new StatsSnapshot.ResourceEntry[0],
            };

            Assert.Throws<ArgumentException>(() => stats.Restore(snapshot));
        }

        // A definition without the field would silently never get an ID.
        [TestCase(typeof(StatDefinition))]
        [TestCase(typeof(ResourceDefinition))]
        [TestCase(typeof(EffectDefinition))]
        [TestCase(typeof(EffectGroupDefinition))]
        public void IdField_ExistsOnDefinition(Type type)
        {
            ScriptableObject definition = ScriptableObject.CreateInstance(type);
            try
            {
                Assert.That(new UnityEditor.SerializedObject(definition).FindProperty(DefinitionEditor.IdField), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
