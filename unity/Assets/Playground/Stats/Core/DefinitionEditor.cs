#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Inspector of every definition: an empty _id takes the asset name if that name is valid, and a missing, malformed
    /// or duplicate ID shows an error. Only the Inspector writes the ID; the game never falls back to the asset name.
    /// </summary>
    public abstract class DefinitionEditor : Editor
    {
        public const string IdField = "_id";

        private static readonly GUIContent IdLabel = new GUIContent(
            "ID", "Key used by code and saves. Renaming the asset does not change it; clear it to take the asset name again.");

        private string _checkedId;
        private string _duplicatePath;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty id = serializedObject.FindProperty(IdField);
            FillEmptyId(id);
            // Read before drawing: a value committed during this event must not change the layout until the next one.
            string idError = GetIdError(id);
            HashSet<string> hidden = GetHiddenFields();

            // Same walk DrawPropertiesExcluding does, so subclasses can draw right under a field.
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == IdField)
                {
                    EditorGUILayout.DelayedTextField(property, IdLabel);
                    if (idError != null)
                    {
                        EditorGUILayout.HelpBox(idError, MessageType.Error);
                    }

                    continue;
                }

                if (hidden.Contains(property.name))
                {
                    continue;
                }

                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                DrawAfterField(property);
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual HashSet<string> GetHiddenFields() => new HashSet<string>();

        protected virtual void DrawAfterField(SerializedProperty property)
        {
        }

        private void FillEmptyId(SerializedProperty id)
        {
            if (!serializedObject.isEditingMultipleObjects && id.stringValue.Length == 0 &&
                AssetDatabase.Contains(target) && IdFormat.IsValid(target.name))
            {
                id.stringValue = target.name;
            }
        }

        private string GetIdError(SerializedProperty id)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return null;
            }

            string value = id.stringValue;
            if (value.Length == 0)
            {
                return $"ID is required. The asset name '{target.name}' cannot be used: an ID {IdFormat.Rule}.";
            }

            if (!IdFormat.IsValid(value))
            {
                return $"This ID {IdFormat.Rule}.";
            }

            // Scans only when the value changes, so an open Inspector costs nothing per frame.
            if (_checkedId != value)
            {
                _checkedId = value;
                _duplicatePath = FindDuplicate(value);
            }

            return _duplicatePath != null ? $"Another {target.GetType().Name} already uses this ID: {_duplicatePath}" : null;
        }

        private string FindDuplicate(string value)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{target.GetType().Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath(path, target.GetType());
                if (asset == null || asset == target)
                {
                    continue;
                }

                using (var other = new SerializedObject(asset))
                {
                    if (other.FindProperty(IdField).stringValue == value)
                    {
                        return path;
                    }
                }
            }

            return null;
        }
    }
}
#endif
