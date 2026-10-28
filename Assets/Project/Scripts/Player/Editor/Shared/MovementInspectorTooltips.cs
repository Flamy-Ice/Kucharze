using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Shows short help messages over setting names, not over editable controls.
    public static class MovementInspectorTooltips
    {
        private static readonly Color LabelTextColor = new Color(0.92f, 0.93f, 0.95f);

        public static void LabelOnly(VisualElement scope, VisualElement field, string tooltip, float labelMinWidth = 145f)
        {
            field.tooltip = string.Empty;
            scope.RegisterCallback<TooltipEvent>(evt =>
            {
                Label label = field.Q<Label>(className: "unity-base-field__label");
                VisualElement hovered = evt.target as VisualElement;
                bool isLabel = label != null && hovered != null &&
                    (hovered == label || label.Contains(hovered));
                evt.tooltip = isLabel ? tooltip : string.Empty;
                if (isLabel)
                    evt.rect = label.worldBound;
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            void RefreshLabels()
            {
                Label mainLabel = field.Q<Label>(className: "unity-base-field__label");
                if (mainLabel != null)
                {
                    // Compact checkboxes can opt out of the width used by ordinary setting labels.
                    mainLabel.style.color = LabelTextColor;
                    mainLabel.style.fontSize = 12;
                    mainLabel.style.minWidth = labelMinWidth;
                }

                field.Query<Label>().ForEach(label =>
                {
                    // Shortened names and reference values must not create their own tooltips.
                    label.displayTooltipWhenElided = false;
                    label.tooltip = label == mainLabel ? tooltip : string.Empty;
                });
            }

            field.RegisterCallback<AttachToPanelEvent>(_ => RefreshLabels());
            field.RegisterCallback<GeometryChangedEvent>(_ => RefreshLabels());
            RefreshLabels();
        }
    }
}
