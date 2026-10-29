using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>Shared OnValidate check: a definition's asset name is its ID.</summary>
    internal static class DefinitionNames
    {
        public static void WarnIfWhitespace(ScriptableObject asset)
        {
            foreach (char c in asset.name)
            {
                if (char.IsWhiteSpace(c))
                {
                    Debug.LogWarning($"{asset.GetType().Name} '{asset.name}': the asset name is the ID and must not contain whitespace.", asset);
                    return;
                }
            }
        }
    }
}
