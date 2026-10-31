using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Authoring wrapper for StatSettings. The ID lives in _id; renaming the asset does not change it.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Stat Definition", fileName = "NewStat")]
    public sealed class StatDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "";
        [Tooltip("Empty falls back to the ID.")]
        [SerializeField] private string _displayName = "";
        [SerializeField] private float _baseValue = 0f;
        [Tooltip("Clamps the final value, after modifiers. A resource's floor is the resource's own Min.")]
        [SerializeField] private float _min = float.NegativeInfinity;
        [Tooltip("Clamps the final value, after modifiers.")]
        [SerializeField] private float _max = float.PositiveInfinity;

        // A bad _id gives the invalid default ID, so CreateSettings never throws and the collection reports it.
        internal StatId Id => IdFormat.IsValid(_id) ? new StatId(_id) : default;

        /// <summary>A new copy on every call; not validated here.</summary>
        public StatSettings CreateSettings() => new StatSettings(Id, _baseValue)
        {
            DisplayName = _displayName,
            Min = _min,
            Max = _max,
        };

        // Runs the core's own check on this stat alone; profile-level checks run in StatProfile.
        private void OnValidate()
        {
            if (!DefinitionIds.Check(this, _id))
            {
                return;
            }

            var settings = new StatCollectionSettings { Stats = { CreateSettings() } };
            if (StatCollection.TryGetError(settings, out string error))
            {
                Debug.LogWarning($"{nameof(StatDefinition)} '{name}': {error}", this);
            }
        }
    }
}
