using UnityEditor;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Replaces this component's Inspector. It does not change how the player moves.
    [CustomEditor(typeof(PlayerMovementResolver)), CanEditMultipleObjects]
    public sealed class PlayerMovementResolverEditor : UnityEditor.Editor
    {
        // Unity calls this to build the panel shown when the component is selected.
        public override VisualElement CreateInspectorGUI()
        {
            // The resolver has no settings. Keep its inspector body empty.
            // Only the panel body is empty; Unity still shows the component header.
            return new VisualElement();
        }
    }
}
