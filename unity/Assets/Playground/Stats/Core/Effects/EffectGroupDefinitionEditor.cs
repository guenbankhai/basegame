#if UNITY_EDITOR
using UnityEditor;

namespace BaseGame.Playground.Stats
{
    [CustomEditor(typeof(EffectGroupDefinition))]
    [CanEditMultipleObjects]
    public sealed class EffectGroupDefinitionEditor : DefinitionEditor
    {
    }
}
#endif
