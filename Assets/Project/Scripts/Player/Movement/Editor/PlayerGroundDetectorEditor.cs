using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Player.Movement.Components;

namespace Player.Movement.Editor
{
    // Custom UIElements Inspector for PlayerGroundDetector.
    // Creates a sleek, modern card-based interface matching the project theme,
    // with a fully custom, styled expandable LayerMask selector.
    [CustomEditor(typeof(PlayerGroundDetector))]
    public class PlayerGroundDetectorEditor : UnityEditor.Editor
    {
        // Visual theme palette
        private static readonly Color GroundAccentColor = new Color(0.35f, 0.85f, 0.55f, 1f);
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

            var headerLabel = new Label("Player Ground Detector");
            headerLabel.style.fontSize = 14;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            headerLabel.style.marginBottom = 2;

            var descriptionLabel = new Label("Performs non-allocating sphere checks to detect solid ground and dispatches grounding events to the PlayerBus.");
            descriptionLabel.style.fontSize = 10;
            descriptionLabel.style.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;

            headerGroup.Add(headerLabel);
            headerGroup.Add(descriptionLabel);
            darkContainer.Add(headerGroup);

            // Serialized properties binding
            var groundLayersProp = serializedObject.FindProperty("_groundLayers");
            var detectionRadiusProp = serializedObject.FindProperty("_detectionRadius");
            var detectionOffsetProp = serializedObject.FindProperty("_detectionOffset");

            // Detection Settings Card
            var detectionCard = new VisualElement();
            detectionCard.style.backgroundColor = CardBackground;
            detectionCard.style.paddingTop = 8;
            detectionCard.style.paddingBottom = 8;
            detectionCard.style.paddingLeft = 8;
            detectionCard.style.paddingRight = 8;
            detectionCard.style.marginBottom = 4;

            detectionCard.style.borderTopLeftRadius = 5;
            detectionCard.style.borderTopRightRadius = 5;
            detectionCard.style.borderBottomLeftRadius = 5;
            detectionCard.style.borderBottomRightRadius = 5;
            detectionCard.style.borderLeftWidth = 1;
            detectionCard.style.borderRightWidth = 1;
            detectionCard.style.borderTopWidth = 1;
            detectionCard.style.borderBottomWidth = 1;
            detectionCard.style.borderLeftColor = LineColor;
            detectionCard.style.borderRightColor = LineColor;
            detectionCard.style.borderTopColor = LineColor;
            detectionCard.style.borderBottomColor = LineColor;

            // Section title
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 6;

            var titleLabel = new Label("Detection Settings");
            titleLabel.style.fontSize = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = GroundAccentColor;
            titleRow.Add(titleLabel);
            detectionCard.Add(titleRow);

            // Fully custom, styled expandable LayerMask dropdown list
            detectionCard.Add(CreateCustomLayerMaskField(groundLayersProp));

            // Detection Radius field
            var radiusField = new PropertyField(detectionRadiusProp, "Detection Radius");
            radiusField.style.marginTop = 2;
            radiusField.style.marginBottom = 4;
            StyleField(radiusField);
            radiusField.RegisterCallback<AttachToPanelEvent>(_ => StyleField(radiusField));
            detectionCard.Add(radiusField);

            // Detection Offset field
            var offsetField = new PropertyField(detectionOffsetProp, "Detection Offset");
            offsetField.style.marginTop = 2;
            offsetField.style.marginBottom = 2;
            StyleField(offsetField);
            offsetField.RegisterCallback<AttachToPanelEvent>(_ => StyleField(offsetField));
            detectionCard.Add(offsetField);

            darkContainer.Add(detectionCard);

            root.Add(darkContainer);
            root.Bind(serializedObject);
            return root;
        }

        // Builds a modern, custom dark-themed expandable LayerMask selector
        private VisualElement CreateCustomLayerMaskField(SerializedProperty property)
        {
            var container = new VisualElement();
            container.style.marginTop = 2;
            container.style.marginBottom = 4;

            // --- Header Bar ---
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;

            var label = new Label("Ground Layers");
            label.style.minWidth = 105;
            label.style.fontSize = 10;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.88f, 0.88f, 0.88f, 1f);
            headerRow.Add(label);

            var dropdownBtn = new VisualElement();
            dropdownBtn.style.flexGrow = 1;
            dropdownBtn.style.flexDirection = FlexDirection.Row;
            dropdownBtn.style.alignItems = Align.Center;
            dropdownBtn.style.justifyContent = Justify.SpaceBetween;
            dropdownBtn.style.backgroundColor = InputBackground;
            dropdownBtn.style.borderTopLeftRadius = 4;
            dropdownBtn.style.borderTopRightRadius = 4;
            dropdownBtn.style.borderBottomLeftRadius = 4;
            dropdownBtn.style.borderBottomRightRadius = 4;
            dropdownBtn.style.borderLeftWidth = 1;
            dropdownBtn.style.borderRightWidth = 1;
            dropdownBtn.style.borderTopWidth = 1;
            dropdownBtn.style.borderBottomWidth = 1;
            dropdownBtn.style.borderLeftColor = LineColor;
            dropdownBtn.style.borderRightColor = LineColor;
            dropdownBtn.style.borderTopColor = LineColor;
            dropdownBtn.style.borderBottomColor = LineColor;
            dropdownBtn.style.paddingLeft = 8;
            dropdownBtn.style.paddingRight = 8;
            dropdownBtn.style.minHeight = 22;

            var summaryContainer = new VisualElement();
            summaryContainer.style.flexDirection = FlexDirection.Row;
            summaryContainer.style.flexWrap = Wrap.Wrap;
            summaryContainer.style.flexGrow = 1;
            dropdownBtn.Add(summaryContainer);

            var arrowLabel = new Label("▼");
            arrowLabel.style.fontSize = 8;
            arrowLabel.style.color = GroundAccentColor;
            arrowLabel.style.marginLeft = 4;
            dropdownBtn.Add(arrowLabel);

            headerRow.Add(dropdownBtn);
            container.Add(headerRow);

            // --- Expandable Dropdown Panel ---
            var dropdownPanel = new VisualElement();
            dropdownPanel.style.display = DisplayStyle.None;
            dropdownPanel.style.backgroundColor = new Color(0.09f, 0.09f, 0.09f, 1f);
            dropdownPanel.style.borderTopLeftRadius = 5;
            dropdownPanel.style.borderTopRightRadius = 5;
            dropdownPanel.style.borderBottomLeftRadius = 5;
            dropdownPanel.style.borderBottomRightRadius = 5;
            dropdownPanel.style.borderLeftWidth = 1;
            dropdownPanel.style.borderRightWidth = 1;
            dropdownPanel.style.borderTopWidth = 1;
            dropdownPanel.style.borderBottomWidth = 1;
            dropdownPanel.style.borderLeftColor = LineColor;
            dropdownPanel.style.borderRightColor = LineColor;
            dropdownPanel.style.borderTopColor = LineColor;
            dropdownPanel.style.borderBottomColor = LineColor;
            dropdownPanel.style.marginTop = 4;
            dropdownPanel.style.paddingTop = 6;
            dropdownPanel.style.paddingBottom = 6;
            dropdownPanel.style.paddingLeft = 8;
            dropdownPanel.style.paddingRight = 8;

            // Layers list container
            var layersList = new VisualElement();

            // Quick Actions Bar
            var actionsRow = new VisualElement();
            actionsRow.style.flexDirection = FlexDirection.Row;
            actionsRow.style.justifyContent = Justify.FlexEnd;
            actionsRow.style.marginBottom = 6;
            actionsRow.style.paddingBottom = 4;
            actionsRow.style.borderBottomWidth = 1;
            actionsRow.style.borderBottomColor = new Color(0.18f, 0.18f, 0.18f, 1f);

            var selectAllBtn = CreateMiniButton("All", () =>
            {
                serializedObject.Update();
                int allMask = 0;
                foreach (var lName in UnityEditorInternal.InternalEditorUtility.layers)
                {
                    allMask |= 1 << LayerMask.NameToLayer(lName);
                }
                property.intValue = allMask;
                serializedObject.ApplyModifiedProperties();
                RefreshUI();
            });

            var clearBtn = CreateMiniButton("None", () =>
            {
                serializedObject.Update();
                property.intValue = 0;
                serializedObject.ApplyModifiedProperties();
                RefreshUI();
            });

            actionsRow.Add(selectAllBtn);
            actionsRow.Add(clearBtn);
            dropdownPanel.Add(actionsRow);
            dropdownPanel.Add(layersList);
            container.Add(dropdownPanel);

            // Toggle Open/Close
            bool isOpen = false;
            dropdownBtn.RegisterCallback<ClickEvent>(_ =>
            {
                isOpen = !isOpen;
                dropdownPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
                arrowLabel.text = isOpen ? "▲" : "▼";
            });

            void RefreshUI()
            {
                serializedObject.Update();
                int currentMask = property.intValue;
                string[] allLayers = UnityEditorInternal.InternalEditorUtility.layers;

                // 1. Update Header Badges
                summaryContainer.Clear();
                var selectedLayers = new System.Collections.Generic.List<string>();
                foreach (var lName in allLayers)
                {
                    int lIndex = LayerMask.NameToLayer(lName);
                    if ((currentMask & (1 << lIndex)) != 0)
                    {
                        selectedLayers.Add(lName);
                    }
                }

                if (selectedLayers.Count == 0)
                {
                    var noneLabel = new Label("Nothing");
                    noneLabel.style.fontSize = 10;
                    noneLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    summaryContainer.Add(noneLabel);
                }
                else if (selectedLayers.Count == allLayers.Length)
                {
                    var allLabel = new Label("Everything");
                    allLabel.style.fontSize = 10;
                    allLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    allLabel.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
                    summaryContainer.Add(allLabel);
                }
                else
                {
                    var textLabel = new Label(string.Join(", ", selectedLayers));
                    textLabel.style.fontSize = 10;
                    textLabel.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
                    textLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                    textLabel.style.overflow = Overflow.Hidden;
                    textLabel.style.textOverflow = TextOverflow.Ellipsis;
                    summaryContainer.Add(textLabel);
                }

                // 2. Update Checklist
                layersList.Clear();
                foreach (var layerName in allLayers)
                {
                    int layerIndex = LayerMask.NameToLayer(layerName);
                    bool isSelected = (currentMask & (1 << layerIndex)) != 0;

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.paddingTop = 3;
                    row.style.paddingBottom = 3;
                    row.style.paddingLeft = 4;
                    row.style.paddingRight = 4;
                    row.style.borderTopLeftRadius = 3;
                    row.style.borderTopRightRadius = 3;
                    row.style.borderBottomLeftRadius = 3;
                    row.style.borderBottomRightRadius = 3;

                    row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f));
                    row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);

                    var leftGroup = new VisualElement();
                    leftGroup.style.flexDirection = FlexDirection.Row;
                    leftGroup.style.alignItems = Align.Center;

                    var checkbox = new VisualElement();
                    checkbox.style.width = 14;
                    checkbox.style.height = 14;
                    checkbox.style.borderTopLeftRadius = 3;
                    checkbox.style.borderTopRightRadius = 3;
                    checkbox.style.borderBottomLeftRadius = 3;
                    checkbox.style.borderBottomRightRadius = 3;
                    checkbox.style.borderLeftWidth = 1;
                    checkbox.style.borderRightWidth = 1;
                    checkbox.style.borderTopWidth = 1;
                    checkbox.style.borderBottomWidth = 1;
                    checkbox.style.marginRight = 8;
                    checkbox.style.alignItems = Align.Center;
                    checkbox.style.justifyContent = Justify.Center;

                    if (isSelected)
                    {
                        checkbox.style.backgroundColor = GroundAccentColor;
                        checkbox.style.borderLeftColor = GroundAccentColor;
                        checkbox.style.borderRightColor = GroundAccentColor;
                        checkbox.style.borderTopColor = GroundAccentColor;
                        checkbox.style.borderBottomColor = GroundAccentColor;

                        var checkmark = new Label("✓");
                        checkmark.style.fontSize = 10;
                        checkmark.style.unityFontStyleAndWeight = FontStyle.Bold;
                        checkmark.style.color = new Color(0.05f, 0.05f, 0.05f, 1f);
                        checkmark.style.marginBottom = 1;
                        checkbox.Add(checkmark);
                    }
                    else
                    {
                        checkbox.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
                        checkbox.style.borderLeftColor = LineColor;
                        checkbox.style.borderRightColor = LineColor;
                        checkbox.style.borderTopColor = LineColor;
                        checkbox.style.borderBottomColor = LineColor;
                    }
                    leftGroup.Add(checkbox);

                    var nameLabel = new Label(layerName);
                    nameLabel.style.fontSize = 11;
                    nameLabel.style.color = isSelected ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
                    if (isSelected) nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    leftGroup.Add(nameLabel);

                    row.Add(leftGroup);

                    row.RegisterCallback<ClickEvent>(_ =>
                    {
                        serializedObject.Update();
                        property.intValue ^= (1 << layerIndex);
                        serializedObject.ApplyModifiedProperties();
                        RefreshUI();
                    });

                    layersList.Add(row);
                }
            }

            RefreshUI();
            container.RegisterCallback<AttachToPanelEvent>(_ => RefreshUI());

            return container;
        }

        // Helper button for Quick Actions (All / None)
        private Button CreateMiniButton(string text, System.Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.fontSize = 9;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            btn.style.borderTopLeftRadius = 3;
            btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3;
            btn.style.borderBottomRightRadius = 3;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftColor = LineColor;
            btn.style.borderRightColor = LineColor;
            btn.style.borderTopColor = LineColor;
            btn.style.borderBottomColor = LineColor;
            btn.style.paddingLeft = 8;
            btn.style.paddingRight = 8;
            btn.style.paddingTop = 2;
            btn.style.paddingBottom = 2;
            btn.style.marginLeft = 4;
            return btn;
        }

        // Customizes field labels and inputs to maintain a unified dark theme
        private void StyleField(VisualElement field)
        {
            var label = field.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.minWidth = 105;
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
