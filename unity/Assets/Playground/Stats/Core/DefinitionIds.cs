using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Shared OnValidate check of a definition's _id.</summary>
    internal static class DefinitionIds
    {
        /// <returns>True if the ID is valid, so the definition can run its other checks.</returns>
        public static bool Check(ScriptableObject asset, string id)
        {
            // Empty is normal on a new asset; the Inspector fills it or reports it, and the core rejects it in Play.
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (!IdFormat.IsValid(id))
            {
                Debug.LogWarning($"{asset.GetType().Name} '{asset.name}': ID '{id}' {IdFormat.Rule}.", asset);
                return false;
            }

            return true;
        }
    }
}
