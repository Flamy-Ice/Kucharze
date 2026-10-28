using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Draws this component's settings in Unity. It does not control the player.
    [CustomEditor(typeof(PlayerRun)), CanEditMultipleObjects]
    public sealed class PlayerRunEditor : UnityEditor.Editor
    {
        // Give this settings panel its own color, so it is easy to recognize.
        private static readonly Color Accent = new Color(0.95f, 0.47f, 0.57f);
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

            var title = CreateLabel("Player Run", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);

            var description = CreateLabel("Sets the target running speed and activation mode.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            container.Add(header);

            // The settings card groups editable values below the title and description.
            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = "run-settings-card";
            var sectionTitle = CreateLabel("Run Settings", 11, Accent);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.marginBottom = 6;
            card.Add(sectionTitle);

            AddField(card, "alwaysRun", "Always Run", null,
                "Runs without pressing Run. Movement input is still required.");
            AddField(card, "alwaysFullRunSpeed", "Always Full Run Speed", null,
                "Uses Run Speed for input above Input Deadzone. Acceleration and braking still apply.");
            MovementInspectorEnumDropdown.Add(card, root, this, "activationMode", "Activation Mode",
                "Hold runs while pressed. Toggle switches running on or off. Always Run overrides both.", Accent);
            AddField(card, "runSpeed", "Run Speed", "m/s",
                "Maximum running speed in meters per second.");
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
                var unitBadge = CreatePanel(3, 0, new Color(0.22f, 0.11f, 0.14f));
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
                if (!(selected is PlayerRun run) || run == null)
                    continue;
                PlayerContext context = run.GetComponent<PlayerContext>();
                if (context != null && context.ValidateDependencies())
                {
                    if (run.AlwaysRun) continue;
                    var asset = context.Input.InputActionAsset;
                    var action = asset != null ? asset.FindAction("Player/Run", false) : null;
                    if (asset == null || (action != null && action.type == UnityEngine.InputSystem.InputActionType.Button))
                        continue;
                    dependencyMessage.text = "Add Player/Run as a Button action to the assigned Input Action Asset.";
                    dependencyMessage.style.display = DisplayStyle.Flex;
                    return;
                }

                // Show the first incomplete setup without adding or repairing components here.
                dependencyMessage.text = run.name + ": Movement foundation is incomplete. "
                    + "Restore required components or re-enable PlayerRun in Play Mode to add missing dependencies.";
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
