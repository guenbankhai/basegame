using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Authoring wrapper for EffectSettings; the ID is the asset name. Time unit is the game's own.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Effect Definition", fileName = "NewEffect")]
    public sealed class EffectDefinition : ScriptableObject
    {
        [Tooltip("Empty falls back to the ID (asset name).")]
        [SerializeField] private string _displayName = "";
        [Tooltip("Lasts until removed; Duration and Max Duration are ignored.")]
        [SerializeField] private bool _infinite = false;
        [SerializeField] private double _duration = 60d;
        [Tooltip("Cap for Extend; must be >= Duration.")]
        [SerializeField] private double _maxDuration = double.PositiveInfinity;
        [SerializeField] private StackRule _stackRule = StackRule.Refresh;
        [Tooltip("Used by Stack.")]
        [SerializeField] private int _maxStacks = 1;
        [Tooltip("Empty stacks freely with every other effect.")]
        [SerializeField] private EffectGroupDefinition _group = null;
        [SerializeField] private int _rank = 0;
        [SerializeField] private List<ModifierEntry> _modifiers = new List<ModifierEntry>();

        internal EffectId Id => string.IsNullOrWhiteSpace(name) ? default : new EffectId(name);

        /// <summary>A new copy on every call; not validated here.</summary>
        public EffectSettings CreateSettings()
        {
            var settings = new EffectSettings(Id, _stackRule)
            {
                DisplayName = _displayName,
                Duration = _infinite ? double.PositiveInfinity : _duration,
                MaxDuration = _infinite ? double.PositiveInfinity : _maxDuration,
                MaxStacks = _maxStacks,
                Group = _group != null ? _group.CreateSettings() : null,
                Rank = _rank,
            };

            foreach (ModifierEntry entry in _modifiers)
            {
                // Empty stat slot gives the invalid default ID, which the collection rejects on Apply.
                StatId stat = entry.Stat != null ? entry.Stat.Id : default;
                settings.Modifiers.Add(new EffectModifierSettings(stat, entry.Type, entry.Value));
            }

            return settings;
        }

        private void OnValidate()
        {
            DefinitionNames.WarnIfWhitespace(this);
            foreach (ModifierEntry entry in _modifiers)
            {
                // Same float multiply EffectSettings checks with.
                if (entry.Type == ModifierType.PercentMult && (entry.Value < -1f || entry.Value * _maxStacks < -1f))
                {
                    Debug.LogWarning($"{nameof(EffectDefinition)} '{name}': PercentMult {entry.Value} x MaxStacks {_maxStacks} must be >= -1.", this);
                }
            }
        }

        [Serializable]
        private sealed class ModifierEntry
        {
            [SerializeField] private StatDefinition _stat = null;
            [SerializeField] private ModifierType _type = ModifierType.Flat;
            [SerializeField] private float _value = 0f;

            public StatDefinition Stat => _stat;
            public ModifierType Type => _type;
            public float Value => _value;
        }
    }
}
