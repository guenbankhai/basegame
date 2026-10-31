#if UNITY_EDITOR
using UnityEditor;

namespace BaseGame.Playground.Stats
{
    [CustomEditor(typeof(ResourceDefinition))]
    [CanEditMultipleObjects]
    public sealed class ResourceDefinitionEditor : DefinitionEditor
    {
    }
}
#endif
