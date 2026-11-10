using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Gives both coin Inspectors the same gold panel and existing project field styles.
    internal static class CoinInspectorPanel
    {
        private static readonly Color Accent = new Color(1f, 0.84f, 0.36f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);

        internal static VisualElement Create(string title, string description, out VisualElement settings)
        {
            var root = Panel(10, new Color(0.08f, 0.08f, 0.08f));
            root.style.marginTop = root.style.marginBottom = 4;
            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;
            var heading = Label(title, 14, Accent);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(heading);
            var summary = Label(description, 10, new Color(0.62f, 0.65f, 0.7f));
            summary.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(summary);
            root.Add(header);
            settings = Panel(8, new Color(0.12f, 0.12f, 0.12f));
            var section = Label("Settings", 11, Accent);
            section.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.style.marginBottom = 6;
            settings.Add(section);
            root.Add(settings);
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        // Unity bindings preserve Undo, prefab overrides and multiple-object editing.
        internal static void AddField(VisualElement card, SerializedObject source,
            string propertyName, string label, string tooltip)
        {
            SerializedProperty property = source.FindProperty(propertyName);
            var row = new VisualElement();
            row.style.marginTop = row.style.marginBottom = 2;
            var field = new PropertyField(property, label) { name = propertyName + "-field" };
            field.style.minWidth = 0;
            row.Add(field);
            card.Add(row);
            field.BindProperty(property);
            MovementInspectorLayout.FixedLabel(field);
            MovementInspectorTooltips.LabelOnly(row, field, tooltip);
        }

        private static Label Label(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static VisualElement Panel(int padding, Color background)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = background;
            panel.style.color = new Color(0.92f, 0.93f, 0.95f);
            panel.style.paddingTop = panel.style.paddingBottom = padding;
            panel.style.paddingLeft = panel.style.paddingRight = padding;
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = 5;
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 5;
            panel.style.borderLeftWidth = panel.style.borderRightWidth = 1;
            panel.style.borderTopWidth = panel.style.borderBottomWidth = 1;
            panel.style.borderLeftColor = panel.style.borderRightColor = Line;
            panel.style.borderTopColor = panel.style.borderBottomColor = Line;
            return panel;
        }
    }
}
