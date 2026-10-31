using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Authoring wrapper for EffectGroupSettings. The ID lives in _id; the policy lives only here.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Effect Group Definition", fileName = "NewEffectGroup")]
    public sealed class EffectGroupDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "";
        [Tooltip("Replace: the loser is removed. Suppress: the loser pauses and resumes once it is the highest rank again.")]
        [SerializeField] private GroupPolicy _policy = GroupPolicy.Replace;

        /// <summary>A new copy on every call; not validated here.</summary>
        public EffectGroupSettings CreateSettings() => new EffectGroupSettings(_id, _policy);

        private void OnValidate() => DefinitionIds.Check(this, _id);
    }
}
