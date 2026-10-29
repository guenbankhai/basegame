using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>The stats and resources of one kind of entity, with optional per-profile base values.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Stat Profile", fileName = "NewStatProfile")]
    public sealed class StatProfile : ScriptableObject
    {
        [SerializeField] private List<StatEntry> _stats = new List<StatEntry>();
        [SerializeField] private List<ResourceDefinition> _resources = new List<ResourceDefinition>();

        /// <summary>A new copy on every call, overrides applied; not validated here.</summary>
        public StatCollectionSettings CreateSettings()
        {
            var settings = new StatCollectionSettings();
            foreach (StatEntry entry in _stats)
            {
                // Empty slots become null elements so the collection rejects them instead of silently dropping them.
                StatSettings stat = entry != null && entry.Stat != null ? entry.Stat.CreateSettings() : null;
                if (stat != null && entry.OverrideBase)
                {
                    stat.BaseValue = entry.BaseValue;
                }

                settings.Stats.Add(stat);
            }

            foreach (ResourceDefinition resource in _resources)
            {
                settings.Resources.Add(resource != null ? resource.CreateSettings() : null);
            }

            return settings;
        }

        private void OnValidate()
        {
            // Usual cause: two assets with the same name in different folders.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (StatEntry entry in _stats)
            {
                if (entry != null && entry.Stat != null && !seen.Add(entry.Stat.name))
                {
                    Debug.LogWarning($"{nameof(StatProfile)} '{name}': stat ID '{entry.Stat.name}' appears more than once.", this);
                }
            }

            seen.Clear();
            foreach (ResourceDefinition resource in _resources)
            {
                if (resource != null && !seen.Add(resource.name))
                {
                    Debug.LogWarning($"{nameof(StatProfile)} '{name}': resource ID '{resource.name}' appears more than once.", this);
                }
            }
        }

        [Serializable]
        private sealed class StatEntry
        {
            [SerializeField] private StatDefinition _stat = null;
            [SerializeField] private bool _overrideBase = false;
            [Tooltip("Used only when Override Base is on.")]
            [SerializeField] private float _baseValue = 0f;

            public StatDefinition Stat => _stat;
            public bool OverrideBase => _overrideBase;
            public float BaseValue => _baseValue;
        }
    }
}
