using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BaseGame.Playground.Stats.Tests
{
    public class EffectDefinitionEditorTests
    {
        // A renamed field would make the editor silently stop hiding it.
        [TestCase(EffectDefinitionEditor.InfiniteField)]
        [TestCase(EffectDefinitionEditor.DurationField)]
        [TestCase(EffectDefinitionEditor.StackRuleField)]
        [TestCase(EffectDefinitionEditor.MaxDurationField)]
        [TestCase(EffectDefinitionEditor.MaxStacksField)]
        [TestCase(EffectDefinitionEditor.GroupField)]
        [TestCase(EffectDefinitionEditor.RankField)]
        public void FieldName_ExistsOnEffectDefinition(string field)
        {
            var definition = ScriptableObject.CreateInstance<EffectDefinition>();
            try
            {
                Assert.That(new SerializedObject(definition).FindProperty(field), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
