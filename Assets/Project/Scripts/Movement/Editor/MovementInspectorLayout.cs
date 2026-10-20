using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Keeps setting labels from changing the space left for their editable values.
    public static class MovementInspectorLayout
    {
        public static void FixedLabel(VisualElement field, float width = 145f)
        {
            void AlignLabel()
            {
                Label label = field.Q<Label>(className: "unity-base-field__label");
                if (label == null)
                    return;
                label.style.width = label.style.minWidth = label.style.maxWidth = width;
                label.style.flexBasis = width;
                label.style.flexGrow = 0;
                label.style.flexShrink = 0;
            }

            // PropertyFields may build their labels after the Inspector is first attached.
            field.RegisterCallback<AttachToPanelEvent>(_ => AlignLabel());
            field.RegisterCallback<GeometryChangedEvent>(_ => AlignLabel());
            field.schedule.Execute(AlignLabel);
            AlignLabel();
        }
    }
}
