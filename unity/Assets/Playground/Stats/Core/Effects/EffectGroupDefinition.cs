using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Authoring wrapper for EffectGroupSettings; the group ID is the asset name, the policy lives only here.</summary>
    [CreateAssetMenu(menuName = "GuenBanKhai/Stats/Effect Group Definition", fileName = "NewEffectGroup")]
    public sealed class EffectGroupDefinition : ScriptableObject
    {
        [SerializeField] private GroupPolicy _policy = GroupPolicy.Replace;

        /// <summary>A new copy on every call; not validated here.</summary>
        public EffectGroupSettings CreateSettings() => new EffectGroupSettings(name, _policy);

        private void OnValidate() => DefinitionNames.WarnIfWhitespace(this);
    }
}
