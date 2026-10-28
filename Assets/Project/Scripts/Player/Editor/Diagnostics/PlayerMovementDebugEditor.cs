using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Builds the Inspector for the debug panel's appearance and visible sections.
    [CustomEditor(typeof(PlayerMovementDebug)), CanEditMultipleObjects]
    public sealed class PlayerMovementDebugEditor : UnityEditor.Editor
    {
        private static readonly Color Accent = new Color(74f / 255f, 214f / 255f, 199f / 255f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);
        private static readonly Color Text = new Color(0.92f, 0.93f, 0.95f);

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var container = CreatePanel(6, 10, new Color(0.08f, 0.08f, 0.08f));
            container.style.marginTop = container.style.marginBottom = 4;
            root.Add(container);

            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;
            var title = CreateLabel("Player Movement Debug", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);
            var description = CreateLabel("Shows local movement data in the debug panel.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            container.Add(header);

            var appearance = CreateCard("Terminal Appearance", "debug-appearance-card");
            AddAnchor(appearance, root);
            AddScreenOffset(appearance);
            AddProperty(appearance, "panelWidth", "Panel Width", "px");
            AddProperty(appearance, "fontSize", "Font Size", "px");
            AddProperty(appearance, "backgroundOpacity", "Background Opacity", "0.2–1");
            AddProperty(appearance, "startVisible", "Start Visible");
            container.Add(appearance);

            var sections = CreateCard("Visible Sections", "debug-sections-card");
            sections.style.marginTop = 8;
            var columns = new VisualElement { name = "debug-section-columns" };
            columns.style.flexDirection = FlexDirection.Row;
            columns.style.flexWrap = Wrap.Wrap;
            sections.Add(columns);
            AddSectionToggle(columns, "showTransform", "Transform", typeof(Transform));
            AddSectionToggle(columns, "showStates", "States", typeof(PlayerStates));
            AddSectionToggle(columns, "showAnimation", "Animation", typeof(PlayerAnimation));
            AddSectionToggle(columns, "showAudio", "Audio", typeof(PlayerAudio));
            AddSectionToggle(columns, "showInput", "Input", typeof(PlayerInputActions));
            AddSectionToggle(columns, "showGravity", "Gravity", typeof(PlayerGravity));
            AddSectionToggle(columns, "showMotor", "Motor", typeof(PlayerMotor));
            AddSectionToggle(columns, "showRotation", "Rotation", typeof(PlayerRotation));
            AddSectionToggle(columns, "showHorizontalMovement", "Horizontal", typeof(PlayerHorizontalMovement));
            AddSectionToggle(columns, "showWalk", "Walk", typeof(PlayerWalk));
            AddSectionToggle(columns, "showRun", "Run", typeof(PlayerRun));
            AddSectionToggle(columns, "showDash", "Dash", typeof(PlayerDash));
            foreach (VisualElement slot in columns.Children())
            {
                slot.style.minWidth = 0;
                slot.style.flexShrink = 0;
                slot.style.flexBasis = new Length(50f, LengthUnit.Percent);
            }
            columns.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                // Keep screen order when the two-column layout wraps into one column.
                bool narrow = evt.newRect.width < 260f;
                foreach (VisualElement slot in columns.Children())
                    slot.style.flexBasis = new Length(narrow ? 100f : 50f, LengthUnit.Percent);
            });
            container.Add(sections);
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        private PropertyField AddProperty(VisualElement parent, string propertyName, string label = null, string unit = null)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = row.style.marginBottom = 2;
            var field = new PropertyField(property, label ?? property.displayName)
                { name = propertyName + "-debug-field" };
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);
            if (!string.IsNullOrEmpty(unit))
            {
                var badge = CreatePanel(3, 0, new Color(0.08f, 0.19f, 0.17f));
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
            parent.Add(row);
            // Save panel settings on the player and let Unity undo edits.
            field.BindProperty(property);
            MovementInspectorTooltips.LabelOnly(row, field, property.tooltip);
            MovementInspectorLayout.FixedLabel(field);
            return field;
        }

        private void AddAnchor(VisualElement parent, VisualElement inspector)
        {
            VisualElement field = MovementInspectorEnumDropdown.Add(parent, inspector, this,
                "anchor", "Anchor", serializedObject.FindProperty("anchor").tooltip, Accent,
                new Color(0.14f, 0.22f, 0.26f));
            field.name = "anchor-debug-field";
        }
        private void AddScreenOffset(VisualElement parent)
        {
            SerializedProperty property = serializedObject.FindProperty("screenOffset");
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = row.style.marginBottom = 2;
            var field = new VisualElement { name = "screenOffset-debug-field" };
            field.AddToClassList("unity-base-field");
            field.style.flexDirection = FlexDirection.Row;
            field.style.flexGrow = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            field.style.marginLeft = field.style.marginRight = 0;
            field.style.alignItems = Align.Center;
            var caption = new Label("Screen Offset");
            caption.AddToClassList("unity-base-field__label");
            field.Add(caption);
            var inputs = new VisualElement();
            inputs.style.flexDirection = FlexDirection.Row;
            inputs.style.flexGrow = 1;
            inputs.style.flexBasis = 0;
            inputs.style.minWidth = 0;
            field.Add(inputs);
            // Edit horizontal and vertical offsets separately to keep both inputs aligned.
            foreach (string axis in new[] { "x", "y" })
            {
                var component = new FloatField(axis.ToUpperInvariant());
                component.name = "screenOffset-" + axis;
                component.style.flexDirection = FlexDirection.Row;
                component.style.flexGrow = 1;
                component.style.flexBasis = 0;
                component.style.width = StyleKeyword.Auto;
                component.style.minWidth = 50;
                component.style.height = 20;
                component.style.marginLeft = axis == "y" ? 4 : 0;
                component.style.marginRight = 0;
                component.labelElement.style.minWidth = 14;
                component.labelElement.style.width = 14;
                component.labelElement.style.flexGrow = 0;
                component.labelElement.style.flexShrink = 0;
                VisualElement number = component.Q<VisualElement>(className: "unity-base-field__input");
                number.style.flexGrow = 1;
                number.style.flexBasis = 0;
                number.style.minWidth = 0;
                number.style.height = 20;
                number.style.marginLeft = number.style.marginRight = 0;
                inputs.Add(component);
                component.BindProperty(property.FindPropertyRelative(axis));
            }
            row.Add(field);
            var badge = CreatePanel(3, 0, new Color(0.08f, 0.19f, 0.17f));
            badge.name = "screenOffset-unit";
            badge.style.width = 44;
            badge.style.height = 20;
            badge.style.marginLeft = 6;
            badge.style.flexShrink = 0;
            badge.style.justifyContent = Justify.Center;
            var unit = CreateLabel("px", 10, Accent);
            unit.style.unityTextAlign = TextAnchor.MiddleCenter;
            unit.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.Add(unit);
            row.Add(badge);
            parent.Add(row);
            MovementInspectorTooltips.LabelOnly(row, field, property.tooltip);
            MovementInspectorLayout.FixedLabel(field);
        }

        private void AddSectionToggle(VisualElement root, string propertyName, string caption, Type componentType)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var slot = new VisualElement();
            root.Add(slot);
            Toggle field = CreateSectionToggle(caption, propertyName + "-debug-field");
            slot.Add(field);
            field.BindProperty(property);
            MovementInspectorTooltips.LabelOnly(field, field, property.tooltip, 0f);
            Toggle missing = CreateSectionToggle(caption, propertyName + "-missing");
            missing.SetEnabled(false);
            slot.Add(missing);
            MovementInspectorTooltips.LabelOnly(missing, missing,
                "Requires " + componentType.Name + " on every selected GameObject.", 0f);

            void RefreshAvailability()
            {
                bool available = HasComponentOnEveryTarget(componentType);
                field.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
                missing.style.display = available ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Keep the saved preference while showing an unchecked, disabled missing section.
            RefreshAvailability();
            root.schedule.Execute(RefreshAvailability).Every(250);
        }

        private static Toggle CreateSectionToggle(string caption, string name)
        {
            var toggle = new Toggle(caption) { name = name };
            toggle.style.marginTop = toggle.style.marginBottom = 2;
            // Keep the caption after the checkbox, with no empty label column.
            Label label = toggle.labelElement;
            label.RemoveFromHierarchy();
            VisualElement input = toggle.Q<VisualElement>(className: "unity-base-field__input");
            input.Add(label);
            input.style.flexDirection = FlexDirection.Row;
            input.style.flexGrow = 0;
            label.style.minWidth = 0;
            label.style.width = StyleKeyword.Auto;
            label.style.marginLeft = 5;
            label.style.marginRight = 0;
            label.style.flexShrink = 1;
            return toggle;
        }

        private bool HasComponentOnEveryTarget(Type componentType)
        {
            foreach (UnityEngine.Object selected in targets)
            {
                if (!(selected is PlayerMovementDebug panel) || panel == null ||
                    panel.GetComponent(componentType) == null)
                    return false;
            }
            return true;
        }

        private static VisualElement CreateCard(string title, string name)
        {
            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = name;
            var heading = CreateLabel(title, 11, Accent);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6;
            card.Add(heading);
            return card;
        }

        private static Label CreateLabel(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        // Match the rounded dark panels used by the other movement Inspectors.
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
