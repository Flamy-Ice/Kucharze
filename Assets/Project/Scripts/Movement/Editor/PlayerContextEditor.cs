using UnityEditor;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Replaces this component's Inspector. It does not change how the player moves.
    [CustomEditor(typeof(PlayerContext)), CanEditMultipleObjects]
    public sealed class PlayerContextEditor : UnityEditor.Editor
    {
        // Unity calls this to build the panel shown when the component is selected.
        public override VisualElement CreateInspectorGUI()
        {
            // The context has no settings. Keep its inspector body empty.
            // Only the panel body is empty; Unity still shows the component header.
            return new VisualElement();
        }
    }
}
