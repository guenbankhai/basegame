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

        /// <summary>
        /// A new copy on every call, overrides applied; not validated here. A resource's MaxStat missing from the
        /// stat list is added after the listed stats with its definition's base; listing it is only for overriding.
        /// </summary>
        public StatCollectionSettings CreateSettings()
        {
            var settings = new StatCollectionSettings();
            var statIds = new HashSet<StatId>();
            foreach (StatEntry entry in _stats)
            {
                // Empty slots become null elements so the collection rejects them instead of silently dropping them.
                StatSettings stat = entry != null && entry.Stat != null ? entry.Stat.CreateSettings() : null;
                if (stat != null && entry.OverrideBase)
                {
                    stat.BaseValue = entry.BaseValue;
                }

                settings.Stats.Add(stat);
                if (stat != null)
                {
                    statIds.Add(stat.Id);
                }
            }

            foreach (ResourceDefinition resource in _resources)
            {
                settings.Resources.Add(resource != null ? resource.CreateSettings() : null);
                // An empty MaxStat stays missing, so the collection rejects the resource.
                StatDefinition maxStat = resource != null ? resource.MaxStat : null;
                if (maxStat != null && maxStat.Id.IsValid && statIds.Add(maxStat.Id))
                {
                    settings.Stats.Add(maxStat.CreateSettings());
                }
            }

            return settings;
        }

        // Runs the core's own check, so the Inspector warns about exactly what StatsBehaviour would reject in Play.
        private void OnValidate()
        {
            if (StatCollection.TryGetError(CreateSettings(), out string error))
            {
                Debug.LogWarning($"{nameof(StatProfile)} '{name}': {error}", this);
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
