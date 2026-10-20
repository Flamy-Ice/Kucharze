using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Replaces this component's Inspector. It does not change how the player moves.
    [CustomEditor(typeof(PlayerInputActions)), CanEditMultipleObjects]
    public sealed class PlayerInputActionsEditor : UnityEditor.Editor
    {
        private SerializedProperty inputAsset;
        private VisualElement container;
        private VisualElement content;
        private HelpBox configurationMessage;

        // Unity calls this to build the panel shown when the component is selected.
        public override VisualElement CreateInspectorGUI()
        {
            // Finds the saved asset reference so the panel can edit it safely.
            inputAsset = serializedObject.FindProperty("inputActionAsset");
            var root = new VisualElement();
            container = new VisualElement { name = "input-inspector" };
            SetFrame(container, 6);
            container.style.marginTop = 4;
            container.style.marginBottom = 4;
            container.style.color = new Color(0.92f, 0.93f, 0.95f);
            root.Add(container);
            content = new VisualElement();
            container.Add(content);

            BuildContent();
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        private void BuildContent()
        {
            // Blue identifies input settings within the shared dark theme.
            Color accent = new Color(0.36f, 0.76f, 1f);
            Color background = new Color(0.08f, 0.08f, 0.08f);
            Color card = new Color(0.12f, 0.12f, 0.12f);
            Color line = new Color(0.24f, 0.27f, 0.32f);
            Color muted = new Color(0.62f, 0.65f, 0.7f);
            SetPadding(container, 10);
            container.style.backgroundColor = background;
            SetBorderColor(container, line);

            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = line;
            var title = CreateLabel("Player Input Actions", 14, accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);
            var description = CreateLabel("Reads movement input for this player and provides a normalized direction.", 10, muted);
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            content.Add(header);

            // Keep the asset selector in its own card below the component description.
            var actionsCard = new VisualElement { name = "input-actions-card" };
            SetFrame(actionsCard, 5);
            SetPadding(actionsCard, 8);
            SetBorderColor(actionsCard, line);
            actionsCard.style.backgroundColor = card;
            var sectionTitle = CreateLabel("Input Actions", 11, accent);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.marginBottom = 6;
            actionsCard.Add(sectionTitle);
            var field = new ObjectField("Input Action Asset")
            {
                name = "input-asset-field",
                objectType = typeof(InputActionAsset),
                allowSceneObjects = false
            };
            field.style.marginTop = 2;
            field.style.marginBottom = 2;
            actionsCard.Add(field);
            // Connects the selector to saved settings, including Undo and changes to prefab instances.
            field.BindProperty(inputAsset);
            MovementInspectorTooltips.LabelOnly(field, field, inputAsset.tooltip);
            MovementInspectorLayout.FixedLabel(field);
            content.Add(actionsCard);
            var deadzoneRow = new VisualElement();
            deadzoneRow.style.flexDirection = FlexDirection.Row;
            deadzoneRow.style.alignItems = Align.Center;
            var deadzone = new PropertyField(serializedObject.FindProperty("inputDeadzone"), "Input Deadzone");
            deadzone.style.flexGrow = 1;
            deadzone.style.flexBasis = 0;
            deadzone.style.minWidth = 0;
            deadzoneRow.Add(deadzone);
            var unit = new Label("0–1");
            unit.style.width = 44;
            unit.style.height = 20;
            unit.style.marginLeft = 6;
            unit.style.flexShrink = 0;
            unit.style.backgroundColor = new Color(0.08f, 0.16f, 0.24f);
            SetFrame(unit, 3);
            SetBorderColor(unit, line);
            unit.style.color = accent;
            unit.style.fontSize = 10;
            unit.style.unityFontStyleAndWeight = FontStyle.Bold;
            unit.style.unityTextAlign = TextAnchor.MiddleCenter;
            deadzoneRow.Add(unit);
            actionsCard.Add(deadzoneRow);
            deadzone.BindProperty(serializedObject.FindProperty("inputDeadzone"));
            MovementInspectorTooltips.LabelOnly(deadzoneRow, deadzone,
                "Input at or below this magnitude is ignored for all movement abilities. Larger input keeps its magnitude.");
            MovementInspectorLayout.FixedLabel(deadzone);
            configurationMessage = new HelpBox("", HelpBoxMessageType.Info);
            configurationMessage.style.marginTop = 6;
            content.Add(configurationMessage);
            // Refresh the help message whenever the assigned asset changes.
            field.TrackPropertyValue(inputAsset, _ => UpdateConfigurationMessage());
            UpdateConfigurationMessage();
        }

        private void UpdateConfigurationMessage()
        {
            configurationMessage.style.display = DisplayStyle.None;
            // This box also shows warnings, so clear the error outline before checking again.
            configurationMessage.style.borderLeftWidth = configurationMessage.style.borderRightWidth = StyleKeyword.Null;
            configurationMessage.style.borderTopWidth = configurationMessage.style.borderBottomWidth = StyleKeyword.Null;
            configurationMessage.style.borderTopLeftRadius = configurationMessage.style.borderTopRightRadius = StyleKeyword.Null;
            configurationMessage.style.borderBottomLeftRadius = configurationMessage.style.borderBottomRightRadius = StyleKeyword.Null;
            configurationMessage.style.borderLeftColor = configurationMessage.style.borderRightColor = StyleKeyword.Null;
            configurationMessage.style.borderTopColor = configurationMessage.style.borderBottomColor = StyleKeyword.Null;
            configurationMessage.style.alignItems = StyleKeyword.Null;
            Label messageLabel = configurationMessage.Q<Label>();
            messageLabel.style.unityTextAlign = StyleKeyword.Null;
            messageLabel.style.flexGrow = StyleKeyword.Null;
            VisualElement messageIcon = configurationMessage.Q(className: "unity-help-box__icon");
            messageIcon.style.alignSelf = StyleKeyword.Null;
            // A missing asset must be visible even when several players are selected.
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerInputActions input) || input == null || input.InputActionAsset != null)
                    continue;

                configurationMessage.text = "PlayerControls was not detected automatically and no Input Action Asset is assigned. "
                    + "Place PlayerControls.inputactions in the same folder as PlayerInputActions.cs, or assign an Input Action Asset manually.";
                configurationMessage.messageType = HelpBoxMessageType.Error;
                configurationMessage.style.alignItems = Align.Center;
                messageLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                messageLabel.style.flexGrow = 1;
                messageIcon.style.alignSelf = Align.Center;
                SetFrame(configurationMessage, 4);
                SetBorderColor(configurationMessage, new Color(0.85f, 0.35f, 0.35f));
                configurationMessage.style.display = DisplayStyle.Flex;
                return;
            }

            // Different assets on selected players cannot be described by one shared message.
            if (inputAsset.hasMultipleDifferentValues)
                return;
            var asset = inputAsset.objectReferenceValue as InputActionAsset;
            if (asset == null)
                return;

            // Warn only when an assigned asset does not provide the expected movement action.
            InputAction move = asset.FindActionMap("Player", false)?.FindAction("Move", false);
            if (move == null || move.type != InputActionType.Value || move.expectedControlType != "Vector2")
            {
                configurationMessage.text = "This asset needs a Player map with a Move action of type Value and control type Vector2.";
                configurationMessage.messageType = HelpBoxMessageType.Warning;
                configurationMessage.style.display = DisplayStyle.Flex;
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

        // Adds breathing room between a box's border and its contents.
        private static void SetPadding(VisualElement element, int padding)
        {
            element.style.paddingTop = element.style.paddingBottom = padding;
            element.style.paddingLeft = element.style.paddingRight = padding;
        }

        // Gives each box a thin outline and rounded corners.
        private static void SetFrame(VisualElement element, int radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
            element.style.borderLeftWidth = element.style.borderRightWidth = 1;
            element.style.borderTopWidth = element.style.borderBottomWidth = 1;
        }

        // Applies one outline color to all four sides of a box.
        private static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderLeftColor = element.style.borderRightColor = color;
            element.style.borderTopColor = element.style.borderBottomColor = color;
        }
    }
}
