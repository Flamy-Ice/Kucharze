using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Draws this component's settings in Unity. It does not control the player.
    [CustomEditor(typeof(PlayerWalk)), CanEditMultipleObjects]
    public sealed class PlayerWalkEditor : UnityEditor.Editor
    {
        // Give this settings panel its own color, so it is easy to recognize.
        private static readonly Color Accent = new Color(0.42f, 0.84f, 0.6f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);
        private static readonly Color Text = new Color(0.92f, 0.93f, 0.95f);
        private HelpBox dependencyMessage;

        // Unity calls this to build the panel shown when the component is selected.
        public override VisualElement CreateInspectorGUI()
        {
            // VisualElement is a box that holds other parts of the panel.
            var root = new VisualElement();
            var container = CreatePanel(6, 10, new Color(0.08f, 0.08f, 0.08f));
            container.style.marginTop = 4;
            container.style.marginBottom = 4;
            root.Add(container);

            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;

            var title = CreateLabel("Player Walk", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);

            var description = CreateLabel("Sets the target walking speed.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            container.Add(header);

            // The settings card groups editable values below the title and description.
            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = "walk-settings-card";
            var sectionTitle = CreateLabel("Walk Settings", 11, Accent);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.marginBottom = 6;
            card.Add(sectionTitle);

            AddField(card, "alwaysFullWalkSpeed", "Always Full Walk Speed", null,
                serializedObject.FindProperty("alwaysFullWalkSpeed").tooltip);
            AddField(card, "walkSpeed", "Walk Speed", "m/s",
                "Maximum walking speed in meters per second.");
            container.Add(card);

            dependencyMessage = new HelpBox("", HelpBoxMessageType.Error);
            dependencyMessage.style.marginTop = 6;
            container.Add(dependencyMessage);
            UpdateDependencyMessage();
            // Check twice a second whether the player is missing any required components.
            root.schedule.Execute(UpdateDependencyMessage).Every(500);
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        private void AddField(VisualElement card, string propertyName, string label, string unit, string tooltip)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;
            // Finds the saved component setting that this row will edit.
            var property = serializedObject.FindProperty(propertyName);
            var field = new PropertyField(property, label)
            {
                name = propertyName + "-field"
            };
            // Leave room beside the input for its unit, such as m/s.
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);
            if (!string.IsNullOrEmpty(unit))
            {
                // Only numeric settings need a unit badge.
                var unitBadge = CreatePanel(3, 0, new Color(0.1f, 0.2f, 0.14f));
                unitBadge.name = propertyName + "-unit";
                unitBadge.style.width = 44;
                unitBadge.style.height = 20;
                unitBadge.style.flexShrink = 0;
                unitBadge.style.marginLeft = 6;
                unitBadge.style.justifyContent = Justify.Center;
                var unitLabel = CreateLabel(unit, 10, Accent);
                unitLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                unitLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                unitBadge.Add(unitLabel);
                row.Add(unitBadge);
            }
            card.Add(row);
            // Save edits on the selected player and let Unity undo them.
            field.BindProperty(property);
            MovementInspectorTooltips.LabelOnly(row, field, tooltip);
            MovementInspectorLayout.FixedLabel(field);
        }

        private void UpdateDependencyMessage()
        {
            dependencyMessage.style.display = DisplayStyle.None;
            // Check each selected player, since one may be missing a required component.
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerWalk walk) || walk == null)
                    continue;
                PlayerContext context = walk.GetComponent<PlayerContext>();
                if (context != null && context.ValidateDependencies())
                    continue;

                // Show the first incomplete setup without adding or repairing components here.
                dependencyMessage.text = walk.name + ": Movement foundation is incomplete. "
                    + "Restore required components or re-enable PlayerWalk in Play Mode to add missing dependencies.";
                dependencyMessage.style.display = DisplayStyle.Flex;
                return;
            }
        }

        // Wrapped text stays readable when the Inspector becomes narrow.
        private static Label CreateLabel(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        // Give settings groups and unit labels matching borders and spacing.
        private static VisualElement CreatePanel(int radius, int padding, Color background)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = background;
            panel.style.color = Text;
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
