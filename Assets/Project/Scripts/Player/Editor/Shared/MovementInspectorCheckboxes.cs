using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Gives all checkbox settings the same readable colors and border.
    public static class MovementInspectorCheckboxes
    {
        public static void Apply(VisualElement scope)
        {
            void Refresh()
            {
                scope.Query<VisualElement>(className: "unity-toggle__checkmark").ForEach(checkmark =>
                {
                    // Unity uses the same style name for arrows and checkboxes; leave the arrows unchanged.
                    Toggle toggle = checkmark.GetFirstAncestorOfType<Toggle>();
                    if (toggle != null && toggle.ClassListContains(Foldout.toggleUssClassName)) return;
                    checkmark.style.backgroundColor = new Color(0.21f, 0.24f, 0.27f);
                    checkmark.style.borderLeftWidth = checkmark.style.borderRightWidth = 1;
                    checkmark.style.borderTopWidth = checkmark.style.borderBottomWidth = 1;
                    checkmark.style.borderLeftColor = checkmark.style.borderRightColor = new Color(0.4f, 0.47f, 0.52f);
                    checkmark.style.borderTopColor = checkmark.style.borderBottomColor = new Color(0.4f, 0.47f, 0.52f);
                    checkmark.style.borderTopLeftRadius = checkmark.style.borderTopRightRadius = 3;
                    checkmark.style.borderBottomLeftRadius = checkmark.style.borderBottomRightRadius = 3;
                });
            }

            // Some settings create their checkboxes later, so apply their appearance again after they appear.
            scope.RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            scope.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            scope.schedule.Execute(Refresh);
            Refresh();
        }
    }
}
