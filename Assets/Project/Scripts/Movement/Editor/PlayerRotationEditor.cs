using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Replaces this component's Inspector. It does not change how the player moves.
    [CustomEditor(typeof(PlayerRotation)), CanEditMultipleObjects]
    public sealed class PlayerRotationEditor : UnityEditor.Editor
    {
        // These colors give this component its own accent within the shared dark theme.
        private static readonly Color Accent = new Color(168f / 255f, 181f / 255f, 1f);
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

            var title = CreateLabel("Player Rotation", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);

            var description = CreateLabel("Turns the visual model toward actual movement.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            container.Add(header);

            // The settings card groups editable values below the title and description.
            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = "rotation-settings-card";
            var sectionTitle = CreateLabel("Rotation Settings", 11, Accent);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.marginBottom = 6;
            card.Add(sectionTitle);

            AddField(card, "modelTransform", "Model Transform", null,
                "Leave empty to find PlayerModel or the only visual child branch.");
            AddField(card, "rotationSpeed", "Rotation Speed", "deg/s",
                "Turning speed. Zero turns instantly.");
            AddField(card, "finishRotationWhenStopped", "Finish Rotation When Stopped", null,
                "Finishes turning toward the last movement direction after stopping.");
            container.Add(card);

            dependencyMessage = new HelpBox("", HelpBoxMessageType.Error);
            dependencyMessage.style.marginTop = 6;
            dependencyMessage.style.alignItems = Align.Center;
            Label messageLabel = dependencyMessage.Q<Label>();
            messageLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            messageLabel.style.flexGrow = 1;
            dependencyMessage.Q(className: "unity-help-box__icon").style.alignSelf = Align.Center;
            dependencyMessage.style.borderLeftWidth = dependencyMessage.style.borderRightWidth = 1;
            dependencyMessage.style.borderTopWidth = dependencyMessage.style.borderBottomWidth = 1;
            dependencyMessage.style.borderTopLeftRadius = dependencyMessage.style.borderTopRightRadius = 4;
            dependencyMessage.style.borderBottomLeftRadius = dependencyMessage.style.borderBottomRightRadius = 4;
            Color errorBorder = new Color(0.85f, 0.35f, 0.35f);
            dependencyMessage.style.borderLeftColor = dependencyMessage.style.borderRightColor = errorBorder;
            dependencyMessage.style.borderTopColor = dependencyMessage.style.borderBottomColor = errorBorder;
            container.Add(dependencyMessage);
            UpdateDependencyMessage();
            // Checks for removed dependencies every half second while this panel is attached.
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
            // The editable field takes the remaining space beside the fixed-width unit badge.
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);
            if (!string.IsNullOrEmpty(unit))
            {
                // Only numeric settings need a unit badge.
                var unitBadge = CreatePanel(3, 0, new Color(0.13f, 0.14f, 0.24f));
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
            // Connects the field to saved settings, including Undo and changes to prefab instances.
            field.BindProperty(property);
            MovementInspectorTooltips.LabelOnly(row, field, tooltip);
            MovementInspectorLayout.FixedLabel(field);
        }

        private void UpdateDependencyMessage()
        {
            dependencyMessage.style.display = DisplayStyle.None;
            PlayerRotation missingModel = null;
            // Check each selected player, since one may be missing a required component.
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerRotation rotation) || rotation == null)
                    continue;
                Transform model = rotation.ResolvedModel;
                if (model != null)
                {
                    // Save models added after this Inspector opened, with Undo and prefab support.
                    if (!Application.isPlaying && rotation.ModelTransform == null)
                    {
                        using (var selectedObject = new SerializedObject(rotation))
                        {
                            selectedObject.FindProperty("modelTransform").objectReferenceValue = model;
                            selectedObject.ApplyModifiedProperties();
                        }
                    }
                    continue;
                }
                if (missingModel == null) missingModel = rotation;
            }
            if (missingModel == null) return;
            dependencyMessage.text = "No valid player model could be selected automatically. "
                + "Assign a visual child to Model Transform, or name it PlayerModel. "
                + "If there are multiple visual branches, select one manually. "
                + "The player root and nested players cannot be used.";
            dependencyMessage.style.display = DisplayStyle.Flex;
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

        // Reuses the same rounded border and inner spacing for panels and badges.
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
