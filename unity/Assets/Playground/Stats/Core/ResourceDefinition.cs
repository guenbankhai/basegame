using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Authoring wrapper for ResourceSettings; the ID is the asset name.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Resource Definition", fileName = "NewResource")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [Tooltip("Empty falls back to the ID (asset name).")]
        [SerializeField] private string _displayName = "";
        [Tooltip("Added to the profile automatically when not listed; list it there only to override its base.")]
        [SerializeField] private StatDefinition _maxStat = null;
        [Tooltip("Floor of Current. Separate from the max stat's own Min, which clamps that stat's value.")]
        [SerializeField] private float _min = 0f;
        [Tooltip("Current at creation: Min + fraction * (Max - Min).")]
        [Range(0f, 1f)]
        [SerializeField] private float _initialFraction = 1f;

        internal ResourceId Id => string.IsNullOrWhiteSpace(name) ? default : new ResourceId(name);
        internal StatDefinition MaxStat => _maxStat;

        /// <summary>A new copy on every call; not validated here.</summary>
        public ResourceSettings CreateSettings() => new ResourceSettings(Id, _maxStat != null ? _maxStat.Id : default)
        {
            DisplayName = _displayName,
            Min = _min,
            InitialFraction = _initialFraction,
        };

        private void OnValidate() => DefinitionNames.WarnIfWhitespace(this);
    }
}
