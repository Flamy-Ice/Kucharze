using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Player.Movement.Components;

namespace Player.Movement.Editor
{
    // Custom UIElements Inspector for PlayerRotation.
    // Creates a sleek, modern card-based interface matching the project theme.
    [CustomEditor(typeof(PlayerRotation))]
    public class PlayerRotationEditor : UnityEditor.Editor
    {
        // Visual theme palette
        private static readonly Color RotationAccentColor = new Color(0.98f, 0.68f, 0.28f, 1f);
        private static readonly Color LineColor = new Color(0.24f, 0.27f, 0.32f, 1f);
        private static readonly Color DarkBackground = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color CardBackground = new Color(0.12f, 0.12f, 0.12f, 1f);
        private static readonly Color InputBackground = new Color(0.16f, 0.16f, 0.16f, 1f);

        // Creates the modern visual inspector tree
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Main dark container
            var darkContainer = new VisualElement();
            darkContainer.style.backgroundColor = DarkBackground;
            darkContainer.style.paddingTop = 10;
            darkContainer.style.paddingBottom = 10;
            darkContainer.style.paddingLeft = 10;
            darkContainer.style.paddingRight = 10;
            darkContainer.style.marginTop = 4;
            darkContainer.style.marginBottom = 4;

            darkContainer.style.borderTopLeftRadius = 6;
            darkContainer.style.borderTopRightRadius = 6;
            darkContainer.style.borderBottomLeftRadius = 6;
            darkContainer.style.borderBottomRightRadius = 6;
            darkContainer.style.borderLeftWidth = 1;
            darkContainer.style.borderRightWidth = 1;
            darkContainer.style.borderTopWidth = 1;
            darkContainer.style.borderBottomWidth = 1;
            darkContainer.style.borderLeftColor = LineColor;
            darkContainer.style.borderRightColor = LineColor;
            darkContainer.style.borderTopColor = LineColor;
            darkContainer.style.borderBottomColor = LineColor;

            // Header section with title and description
            var headerGroup = new VisualElement();
            headerGroup.style.marginBottom = 10;
            headerGroup.style.paddingBottom = 8;
            headerGroup.style.borderBottomWidth = 1;
            headerGroup.style.borderBottomColor = LineColor;

            var headerLabel = new Label("Player Rotation");
            headerLabel.style.fontSize = 14;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            headerLabel.style.marginBottom = 2;

            var descriptionLabel = new Label("Smoothly rotates the child PlayerModel (or root GameObject) towards the movement direction.");
            descriptionLabel.style.fontSize = 10;
            descriptionLabel.style.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;

            headerGroup.Add(headerLabel);
            headerGroup.Add(descriptionLabel);
            darkContainer.Add(headerGroup);

            // Serialized properties binding
            var rotationSpeedProp = serializedObject.FindProperty("_rotationSpeed");

            // Rotation Settings Card
            var settingsCard = CreateCard();

            var settingsTitleRow = CreateTitleRow("Rotation Settings", RotationAccentColor);
            settingsCard.Add(settingsTitleRow);

            var speedField = new PropertyField(rotationSpeedProp, "Rotation Speed");
            speedField.style.marginTop = 2;
            speedField.style.marginBottom = 4;
            StyleField(speedField);
            speedField.RegisterCallback<AttachToPanelEvent>(_ => StyleField(speedField));
            settingsCard.Add(speedField);

            darkContainer.Add(settingsCard);

            root.Add(darkContainer);
            root.Bind(serializedObject);
            return root;
        }

        // Helper to construct modern dark-card sections
        private VisualElement CreateCard()
        {
            var card = new VisualElement();
            card.style.backgroundColor = CardBackground;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.marginBottom = 4;

            card.style.borderTopLeftRadius = 5;
            card.style.borderTopRightRadius = 5;
            card.style.borderBottomLeftRadius = 5;
            card.style.borderBottomRightRadius = 5;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftColor = LineColor;
            card.style.borderRightColor = LineColor;
            card.style.borderTopColor = LineColor;
            card.style.borderBottomColor = LineColor;

            return card;
        }

        // Helper to construct consistent section title headers
        private VisualElement CreateTitleRow(string titleText, Color titleColor)
        {
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 6;

            var titleLabel = new Label(titleText);
            titleLabel.style.fontSize = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = titleColor;
            titleRow.Add(titleLabel);

            return titleRow;
        }

        // Customizes field labels and inputs to maintain a unified dark theme
        private void StyleField(VisualElement field)
        {
            var label = field.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.minWidth = 120;
                label.style.fontSize = 10;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.color = new Color(0.88f, 0.88f, 0.88f, 1f);
            }

            var input = field.Q(className: "unity-base-field__input");
            if (input != null)
            {
                input.style.backgroundColor = InputBackground;
                input.style.borderTopLeftRadius = 4;
                input.style.borderTopRightRadius = 4;
                input.style.borderBottomLeftRadius = 4;
                input.style.borderBottomRightRadius = 4;
                input.style.borderLeftWidth = 1;
                input.style.borderRightWidth = 1;
                input.style.borderTopWidth = 1;
                input.style.borderBottomWidth = 1;
                input.style.borderLeftColor = LineColor;
                input.style.borderRightColor = LineColor;
                input.style.borderTopColor = LineColor;
                input.style.borderBottomColor = LineColor;
            }
        }
    }
}
