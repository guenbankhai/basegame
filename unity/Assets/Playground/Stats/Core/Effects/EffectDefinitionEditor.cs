#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Inspector that shows only the fields the chosen StackRule, Infinite and Group use. Hidden values stay in the
    /// asset and the core ignores them, so switching back shows them again. Mixed values in a multi-selection show all.
    /// </summary>
    [CustomEditor(typeof(EffectDefinition))]
    [CanEditMultipleObjects]
    public sealed class EffectDefinitionEditor : Editor
    {
        // Field names of EffectDefinition; a test checks they still exist, since a rename would silently stop hiding.
        public const string InfiniteField = "_infinite";
        public const string DurationField = "_duration";
        public const string StackRuleField = "_stackRule";
        public const string MaxDurationField = "_maxDuration";
        public const string MaxStacksField = "_maxStacks";
        public const string GroupField = "_group";
        public const string RankField = "_rank";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty rule = serializedObject.FindProperty(StackRuleField);
            SerializedProperty infinite = serializedObject.FindProperty(InfiniteField);
            HashSet<string> hidden = GetHiddenFields(rule, infinite, serializedObject.FindProperty(GroupField));

            // Same walk DrawPropertiesExcluding does, so the rule's help box can sit right under the rule.
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (hidden.Contains(property.name))
                {
                    continue;
                }

                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                if (property.name == StackRuleField && !rule.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox(Describe((StackRule)rule.intValue, infinite), MessageType.None);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static HashSet<string> GetHiddenFields(SerializedProperty rule, SerializedProperty infinite, SerializedProperty group)
        {
            var hidden = new HashSet<string>();
            if (!rule.hasMultipleDifferentValues)
            {
                var value = (StackRule)rule.intValue;
                if (value != StackRule.Extend)
                {
                    hidden.Add(MaxDurationField);
                }

                if (value != StackRule.Stack)
                {
                    hidden.Add(MaxStacksField);
                }
            }

            if (!infinite.hasMultipleDifferentValues && infinite.boolValue)
            {
                hidden.Add(DurationField);
                hidden.Add(MaxDurationField);
            }

            if (!group.hasMultipleDifferentValues && group.objectReferenceValue == null)
            {
                hidden.Add(RankField);
            }

            return hidden;
        }

        private static string Describe(StackRule rule, SerializedProperty infinite)
        {
            string text = rule switch
            {
                StackRule.Refresh => "Re-apply: time resets to Duration.",
                StackRule.Extend => "Re-apply: adds Duration, capped at Max Duration.",
                StackRule.Ignore => "Re-apply: does nothing.",
                StackRule.Stack => "Re-apply: +1 stack up to Max Stacks, time resets to Duration. All stacks share one timer.",
                _ => "Unknown rule.",
            };

            return !infinite.hasMultipleDifferentValues && infinite.boolValue
                ? text + "\nInfinite: re-applying never changes the time."
                : text;
        }
    }
}
#endif
