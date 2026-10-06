using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Player.Movement.Components;

namespace Player.Movement.Editor
{
    // Custom UIElements Inspector for PlayerInputActions.
    // Creates a sleek, modern card-based interface matching the project theme.
    [CustomEditor(typeof(PlayerInputActions))]
    public class PlayerInputActionsEditor : UnityEditor.Editor
    {
        // Visual theme palette
        private static readonly Color InputAccentColor = new Color(0.36f, 0.76f, 1f, 1f);
        private static readonly Color LineColor = new Color(0.24f, 0.27f, 0.32f, 1f);
        private static readonly Color DarkBackground = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color CardBackground = new Color(0.12f, 0.12f, 0.12f, 1f);

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

            var headerLabel = new Label("Player Input Actions");
            headerLabel.style.fontSize = 14;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            headerLabel.style.marginBottom = 2;

            var descriptionLabel = new Label("Binds Unity Input System directional actions and dispatches normalized vectors to the PlayerBus.");
            descriptionLabel.style.fontSize = 10;
            descriptionLabel.style.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;

            headerGroup.Add(headerLabel);
            headerGroup.Add(descriptionLabel);
            darkContainer.Add(headerGroup);

            // Serialized properties binding
            var moveActionProp = serializedObject.FindProperty("_moveAction");
            var sprintActionProp = serializedObject.FindProperty("_sprintAction");

            // Input Actions Card
            var actionsCard = new VisualElement();
            actionsCard.style.backgroundColor = CardBackground;
            actionsCard.style.paddingTop = 8;
            actionsCard.style.paddingBottom = 8;
            actionsCard.style.paddingLeft = 8;
            actionsCard.style.paddingRight = 8;
            actionsCard.style.marginBottom = 4;

            actionsCard.style.borderTopLeftRadius = 5;
            actionsCard.style.borderTopRightRadius = 5;
            actionsCard.style.borderBottomLeftRadius = 5;
            actionsCard.style.borderBottomRightRadius = 5;
            actionsCard.style.borderLeftWidth = 1;
            actionsCard.style.borderRightWidth = 1;
            actionsCard.style.borderTopWidth = 1;
            actionsCard.style.borderBottomWidth = 1;
            actionsCard.style.borderLeftColor = LineColor;
            actionsCard.style.borderRightColor = LineColor;
            actionsCard.style.borderTopColor = LineColor;
            actionsCard.style.borderBottomColor = LineColor;

            // Section title
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 6;

            var titleLabel = new Label("Input Actions");
            titleLabel.style.fontSize = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = InputAccentColor;
            titleRow.Add(titleLabel);
            actionsCard.Add(titleRow);

            // Action fields
            actionsCard.Add(CreateActionField(moveActionProp, "Move Action"));
            actionsCard.Add(CreateActionField(sprintActionProp, "Sprint Action"));

            darkContainer.Add(actionsCard);

            root.Add(darkContainer);
            root.Bind(serializedObject);
            return root;
        }

        // Creates a customized ObjectField for selecting an InputActionReference
        private VisualElement CreateActionField(SerializedProperty property, string labelText)
        {
            var objectField = new ObjectField(labelText)
            {
                bindingPath = property.propertyPath,
                objectType = typeof(InputActionReference),
                allowSceneObjects = false
            };

            objectField.style.marginTop = 2;
            objectField.style.marginBottom = 2;

            var label = objectField.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.minWidth = 85;
                label.style.fontSize = 10;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.color = new Color(0.88f, 0.88f, 0.88f, 1f);
            }

            return objectField;
        }
    }
}
