#if UNITY_EDITOR
using UnityEditor;

namespace BaseGame.Playground.Stats
{
    [CustomEditor(typeof(StatDefinition))]
    [CanEditMultipleObjects]
    public sealed class StatDefinitionEditor : DefinitionEditor
    {
    }
}
#endif
