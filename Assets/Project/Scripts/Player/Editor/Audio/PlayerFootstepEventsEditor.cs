using UnityEditor;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Keeps the event receiver's Inspector empty because it has no settings.
    [CustomEditor(typeof(PlayerFootstepEvents)), CanEditMultipleObjects]
    public sealed class PlayerFootstepEventsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() => new VisualElement();
    }
}
