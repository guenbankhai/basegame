#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Turns PlaygroundCatalog into definition assets under Assets/Configs/Stats. Assets already at their path are kept
    /// untouched, so hand edits survive a second run; new assets reference whatever sits at the expected paths.
    /// </summary>
    public static class PlaygroundAssetGenerator
    {
        private const string Root = "Assets/Configs/Stats";

        [MenuItem("GuenBanKhai/Stats/Playground/Generate Assets")]
        private static void Generate()
        {
            var created = new List<string>();
            StatCollectionSettings statSettings = PlaygroundCatalog.CreateStatSettings();

            var stats = new Dictionary<StatId, StatDefinition>();
            foreach (StatSettings stat in statSettings.Stats)
            {
                stats[stat.Id] = LoadOrCreate<StatDefinition>($"{Root}/Stats/{stat.Id.Value}.asset", created, so =>
                {
                    so.FindProperty("_displayName").stringValue = stat.DisplayName;
                    so.FindProperty("_baseValue").floatValue = stat.BaseValue;
                    so.FindProperty("_min").floatValue = stat.Min;
                    so.FindProperty("_max").floatValue = stat.Max;
                });
            }

            // "Pools", not "Resources": Unity ships every Resources folder in the build.
            var resources = new List<ResourceDefinition>();
            foreach (ResourceSettings resource in statSettings.Resources)
            {
                resources.Add(LoadOrCreate<ResourceDefinition>($"{Root}/Pools/{resource.Id.Value}.asset", created, so =>
                {
                    so.FindProperty("_displayName").stringValue = resource.DisplayName;
                    so.FindProperty("_maxStat").objectReferenceValue = stats[resource.MaxStat];
                    so.FindProperty("_min").floatValue = resource.Min;
                    so.FindProperty("_initialFraction").floatValue = resource.InitialFraction;
                }));
            }

            LoadOrCreate<StatProfile>($"{Root}/TestEntity.asset", created, so =>
            {
                SerializedProperty statList = so.FindProperty("_stats");
                statList.arraySize = statSettings.Stats.Count;
                for (int i = 0; i < statSettings.Stats.Count; i++)
                {
                    statList.GetArrayElementAtIndex(i).FindPropertyRelative("_stat").objectReferenceValue = stats[statSettings.Stats[i].Id];
                }

                SerializedProperty resourceList = so.FindProperty("_resources");
                resourceList.arraySize = resources.Count;
                for (int i = 0; i < resources.Count; i++)
                {
                    resourceList.GetArrayElementAtIndex(i).objectReferenceValue = resources[i];
                }
            });

            var groups = new Dictionary<string, EffectGroupDefinition>(StringComparer.Ordinal);
            foreach (EffectSettings effect in PlaygroundCatalog.CreateEffects())
            {
                EffectGroupSettings group = effect.Group;
                if (group != null && !groups.ContainsKey(group.Id))
                {
                    groups[group.Id] = LoadOrCreate<EffectGroupDefinition>($"{Root}/Groups/{group.Id}.asset", created, so =>
                        so.FindProperty("_policy").intValue = (int)group.Policy);
                }

                LoadOrCreate<EffectDefinition>($"{Root}/Effects/{effect.Id.Value}.asset", created, so =>
                {
                    bool infinite = double.IsPositiveInfinity(effect.Duration);
                    so.FindProperty("_displayName").stringValue = effect.DisplayName;
                    so.FindProperty("_infinite").boolValue = infinite;
                    if (!infinite)
                    {
                        so.FindProperty("_duration").doubleValue = effect.Duration;
                        so.FindProperty("_maxDuration").doubleValue = effect.MaxDuration;
                    }

                    so.FindProperty("_stackRule").intValue = (int)effect.StackRule;
                    so.FindProperty("_maxStacks").intValue = effect.MaxStacks;
                    so.FindProperty("_group").objectReferenceValue = group != null ? groups[group.Id] : null;
                    so.FindProperty("_rank").intValue = effect.Rank;

                    SerializedProperty modifierList = so.FindProperty("_modifiers");
                    modifierList.arraySize = effect.Modifiers.Count;
                    for (int i = 0; i < effect.Modifiers.Count; i++)
                    {
                        EffectModifierSettings modifier = effect.Modifiers[i];
                        SerializedProperty element = modifierList.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("_stat").objectReferenceValue = stats[modifier.Stat];
                        element.FindPropertyRelative("_type").intValue = (int)modifier.Type;
                        element.FindPropertyRelative("_value").floatValue = modifier.Value;
                    }
                });
            }

            AssetDatabase.SaveAssets();
            Debug.Log(created.Count == 0
                ? $"Stats playground: every asset already exists under {Root}, nothing created."
                : $"Stats playground: created {created.Count} asset(s) under {Root}:\n{string.Join("\n", created)}");
        }

        /// <summary>Overwrites only DisplayName, found by asset name anywhere under Root, so GUIDs and scene links stay.</summary>
        [MenuItem("GuenBanKhai/Stats/Playground/Sync Display Names")]
        private static void SyncDisplayNames()
        {
            StatCollectionSettings statSettings = PlaygroundCatalog.CreateStatSettings();
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (StatSettings stat in statSettings.Stats)
            {
                names[stat.Id.Value] = stat.DisplayName;
            }

            foreach (ResourceSettings resource in statSettings.Resources)
            {
                names[resource.Id.Value] = resource.DisplayName;
            }

            foreach (EffectSettings effect in PlaygroundCatalog.CreateEffects())
            {
                names[effect.Id.Value] = effect.DisplayName;
            }

            int updated = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { Root }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = asset != null ? new SerializedObject(asset) : null;
                SerializedProperty displayName = serialized?.FindProperty("_displayName");
                if (displayName != null && names.TryGetValue(asset.name, out string value) && displayName.stringValue != value)
                {
                    displayName.stringValue = value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Stats playground: updated DisplayName on {updated} asset(s) under {Root}.");
        }

        private static T LoadOrCreate<T>(string path, List<string> created, Action<SerializedObject> fill) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var serialized = new SerializedObject(asset);
            fill(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }
    }
}
#endif
