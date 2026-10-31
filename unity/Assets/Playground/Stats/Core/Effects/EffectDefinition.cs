using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Authoring wrapper for EffectSettings. The ID lives in _id; renaming the asset does not change it. Time unit is
    /// the game's own.
    /// </summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Effect Definition", fileName = "NewEffect")]
    public sealed class EffectDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "";
        [Tooltip("Empty falls back to the ID.")]
        [SerializeField] private string _displayName = "";
        [Tooltip("Lasts until removed; Duration and Max Duration are ignored.")]
        [SerializeField] private bool _infinite = false;
        [Tooltip("Same unit as the delta the game passes to Advance.")]
        [SerializeField] private double _duration = 60d;
        // Declared right after the rule so the Inspector shows each rule's own fields under it.
        [SerializeField] private StackRule _stackRule = StackRule.Refresh;
        [Tooltip("Cap for Extend; must be >= Duration.")]
        [SerializeField] private double _maxDuration = double.PositiveInfinity;
        [Tooltip("Cap for Stack.")]
        [SerializeField] private int _maxStacks = 1;
        [Tooltip("Empty stacks freely with every other effect.")]
        [SerializeField] private EffectGroupDefinition _group = null;
        [Tooltip("Highest rank in the group takes effect; ties go to the latest apply.")]
        [SerializeField] private int _rank = 0;
        [SerializeField] private List<ModifierEntry> _modifiers = new List<ModifierEntry>();

        internal EffectId Id => IdFormat.IsValid(_id) ? new EffectId(_id) : default;

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

        // Runs the core's check on this asset alone; a Policy conflict with a running effect only shows on Apply.
        private void OnValidate()
        {
            if (DefinitionIds.Check(this, _id) && CreateSettings().TryGetError(out string error))
            {
                Debug.LogWarning($"{nameof(EffectDefinition)} '{name}': {error}", this);
            }
        }

        [Serializable]
        private sealed class ModifierEntry
        {
            [SerializeField] private StatDefinition _stat = null;
            [SerializeField] private ModifierType _type = ModifierType.Flat;
            [Tooltip("Flat: added as is. PercentAdd 0.1 = +10%. PercentMult -0.2 = x0.8.")]
            [SerializeField] private float _value = 0f;

            public StatDefinition Stat => _stat;
            public ModifierType Type => _type;
            public float Value => _value;
        }
    }
}
