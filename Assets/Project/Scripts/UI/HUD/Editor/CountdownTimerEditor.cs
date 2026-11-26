using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Draws the countdown settings in the same style as the other project Inspectors.
    [CustomEditor(typeof(CountdownTimer)), CanEditMultipleObjects]
    public sealed class CountdownTimerEditor : UnityEditor.Editor
    {
        private static readonly Color Accent = new Color(0.96f, 0.78f, 0.48f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);

        public override VisualElement CreateInspectorGUI()
        {
            var root = CreatePanel(6, 10, new Color(0.08f, 0.08f, 0.08f));
            root.style.marginTop = root.style.marginBottom = 4;

            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;

            var title = CreateLabel("Countdown Timer", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);

            var description = CreateLabel("Counts down to zero in m:ss format.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            root.Add(header);

            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = "countdown-settings-card";
            var sectionTitle = CreateLabel("Timer Settings", 11, Accent);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.marginBottom = 6;
            card.Add(sectionTitle);

            AddField(card, "timerText", "Timer Text", null,
                "Text that shows the countdown. If empty, uses TMP text on this object.");
            AddField(card, "durationSeconds", "Duration", "s",
                "Starting time in seconds. For example, 120 is two minutes. Zero keeps the timer stopped.");
            AddField(card, "startAutomatically", "Start Automatically", null,
                "Start counting when this object first loads. Otherwise, call ResumeTimer to begin.");
            root.Add(card);

            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        // Binding keeps Undo, prefab overrides and editing several timers working through Unity.
        private void AddField(VisualElement card, string propertyName, string label, string unit, string tooltip)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = row.style.marginBottom = 2;

            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var field = new PropertyField(property, label) { name = propertyName + "-field" };
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);

            if (!string.IsNullOrEmpty(unit))
            {
                var badge = CreatePanel(3, 0, new Color(0.23f, 0.18f, 0.10f));
                badge.name = propertyName + "-unit";
                badge.style.width = 44;
                badge.style.height = 20;
                badge.style.flexShrink = 0;
                badge.style.marginLeft = 6;
                badge.style.justifyContent = Justify.Center;
                var unitLabel = CreateLabel(unit, 10, Accent);
                unitLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                unitLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.Add(unitLabel);
                row.Add(badge);
            }

            card.Add(row);
            field.BindProperty(property);
            // Reuse the shared layout so hovering a label does not move the editable input.
            MovementInspectorTooltips.LabelOnly(row, field, tooltip);
            MovementInspectorLayout.FixedLabel(field);
        }

        private static Label CreateLabel(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static VisualElement CreatePanel(int radius, int padding, Color background)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = background;
            panel.style.color = new Color(0.92f, 0.93f, 0.95f);
            panel.style.paddingTop = panel.style.paddingBottom = padding;
            panel.style.paddingLeft = panel.style.paddingRight = padding;
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = radius;
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = radius;
            panel.style.borderLeftWidth = panel.style.borderRightWidth = 1;
            panel.style.borderTopWidth = panel.style.borderBottomWidth = 1;
            panel.style.borderLeftColor = panel.style.borderRightColor = Line;
            panel.style.borderTopColor = panel.style.borderBottomColor = Line;
            return panel;
        }
    }
}
