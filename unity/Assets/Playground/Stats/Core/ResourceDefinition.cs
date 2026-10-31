using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Authoring wrapper for ResourceSettings. The ID lives in _id; renaming the asset does not change it.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Resource Definition", fileName = "NewResource")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "";
        [Tooltip("Empty falls back to the ID.")]
        [SerializeField] private string _displayName = "";
        [Tooltip("Added to the profile automatically when not listed; list it there only to override its base.")]
        [SerializeField] private StatDefinition _maxStat = null;
        [Tooltip("Floor of Current. Separate from the max stat's own Min, which clamps that stat's value.")]
        [SerializeField] private float _min = 0f;
        [Tooltip("Current at creation: Min + fraction * (Max - Min).")]
        [Range(0f, 1f)]
        [SerializeField] private float _initialFraction = 1f;

        internal ResourceId Id => IdFormat.IsValid(_id) ? new ResourceId(_id) : default;
        internal StatDefinition MaxStat => _maxStat;

        /// <summary>A new copy on every call; not validated here.</summary>
        public ResourceSettings CreateSettings() => new ResourceSettings(Id, _maxStat != null ? _maxStat.Id : default)
        {
            DisplayName = _displayName,
            Min = _min,
            InitialFraction = _initialFraction,
        };

        private void OnValidate() => DefinitionIds.Check(this, _id);
    }
}
