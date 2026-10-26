using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Replaces this component's Inspector. It does not change how the player moves.
    [CustomEditor(typeof(PlayerDash)), CanEditMultipleObjects]
    public sealed class PlayerDashEditor : UnityEditor.Editor
    {
        // These colors give this component its own accent within the shared dark theme.
        private static readonly Color Accent = new Color(97f / 255f, 232f / 255f, 1f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);
        private static readonly Color Text = new Color(0.92f, 0.93f, 0.95f);
        private HelpBox dependencyMessage;
        private Label estimatedDistance;

        [MenuItem("CONTEXT/PlayerDash/Reset Speed Curve")]
        private static void ResetSpeedCurve(MenuCommand command)
        {
            // Restore only the profile, keeping the player's other dash settings and Undo history.
            if (!(command.context is PlayerDash dash)) return;
            var settings = new SerializedObject(dash);
            settings.FindProperty("speedCurve").animationCurveValue = PlayerDash.CreateDefaultSpeedCurve();
            settings.ApplyModifiedProperties();
        }

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

            var title = CreateLabel("Player Dash", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);

            var description = CreateLabel("Offers a short burst in the current or last movement direction.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            container.Add(header);

            // The settings card groups editable values below the title and description.
            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = "dash-settings-card";
            var sectionTitle = CreateLabel("Dash Settings", 11, Accent);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.marginBottom = 6;
            card.Add(sectionTitle);

            AddField(card, "dashSpeed", "Dash Speed", "m/s",
                "Horizontal burst speed. Zero prevents a new dash.");
            AddField(card, "dashDuration", "Dash Duration", "s",
                "How long a dash lasts. Direction stays fixed during the burst.");
            AddField(card, "speedCurve", "Speed Curve", null,
                "Speed multiplier over dash time (0–1). Held movement sets a speed floor, up to Dash Speed.");
            AddField(card, "cooldown", "Cooldown", "s",
                "Wait after a dash ends before another press can start one.");
            container.Add(card);
            AddEstimatedDistance(container);

            dependencyMessage = new HelpBox("", HelpBoxMessageType.Error);
            dependencyMessage.style.marginTop = 6;
            container.Add(dependencyMessage);
            UpdateDependencyMessage();
            // Checks for removed dependencies every half second while this panel is attached.
            root.schedule.Execute(UpdateDependencyMessage).Every(500);
            root.TrackSerializedObjectValue(serializedObject, _ => UpdateEstimatedDistance());
            // Also follows settings changed by game code while this Inspector is open.
            root.schedule.Execute(UpdateEstimatedDistance).Every(500);
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        private void AddEstimatedDistance(VisualElement container)
        {
            // Plain text and an approximation sign distinguish the preview from input fields.
            var row = new VisualElement { name = "estimatedDistance-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 8;
            row.style.marginBottom = 2;
            row.style.marginLeft = row.style.marginRight = 8;
            var label = CreateLabel("Base Distance", 12, Text);
            label.AddToClassList("unity-base-field__label");
            row.Add(label);
            var approximation = CreateLabel("\u2248", 12, Accent);
            approximation.style.marginRight = 4;
            row.Add(approximation);
            estimatedDistance = CreateLabel("", 12, Accent);
            estimatedDistance.name = "estimatedDistance-value";
            estimatedDistance.style.flexShrink = 0;
            row.Add(estimatedDistance);
            var unit = new VisualElement();
            unit.name = "estimatedDistance-unit";
            unit.style.width = 14;
            unit.style.height = 20;
            unit.style.flexShrink = 0;
            unit.style.marginLeft = 4;
            unit.style.justifyContent = Justify.Center;
            var unitLabel = CreateLabel("m", 12, Accent);
            unitLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            unit.Add(unitLabel);
            row.Add(unit);
            container.Add(row);
            MovementInspectorLayout.FixedLabel(row);
            MovementInspectorTooltips.LabelOnly(row, row,
                "Curve distance before the held-movement speed floor and collisions.");
            UpdateEstimatedDistance();
        }

        private void UpdateEstimatedDistance()
        {
            double? distance = null;
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerDash dash) || dash == null) continue;
                // Use the movement calculation, including its curve limits and empty-curve fallback.
                double next = (double)dash.DashSpeed * dash.DashDuration * dash.IntegrateSpeedCurve(0f, 1f);
                if (distance.HasValue && System.Math.Abs(distance.Value - next) > 0.000001 * System.Math.Max(1d, next))
                {
                    estimatedDistance.text = "\u2014";
                    return;
                }
                distance = next;
            }
            estimatedDistance.text = distance.HasValue
                ? distance.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "\u2014";
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
                var unitBadge = CreatePanel(3, 0, new Color(0.08f, 0.19f, 0.22f));
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
            if (property.propertyType == SerializedPropertyType.AnimationCurve)
            {
                field.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    CurveField curve = field.Q<CurveField>();
                    if (curve == null) return;
                    curve.ranges = new Rect(0f, 0f, 1f, 1f);
                });
            }
        }

        private void UpdateDependencyMessage()
        {
            dependencyMessage.style.display = DisplayStyle.None;
            // Check each selected player, since one may be missing a required component.
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerDash dash) || dash == null)
                    continue;
                PlayerContext context = dash.GetComponent<PlayerContext>();
                if (context != null && context.ValidateDependencies())
                {
                    var asset = context.Input.InputActionAsset;
                    var action = asset != null ? asset.FindAction("Player/Dash", false) : null;
                    if (asset == null || (action != null && action.type == UnityEngine.InputSystem.InputActionType.Button))
                        continue;
                    dependencyMessage.text = "Add Player/Dash as a Button action to the assigned Input Action Asset.";
                    dependencyMessage.style.display = DisplayStyle.Flex;
                    return;
                }

                // Show the first incomplete setup without adding or repairing components here.
                dependencyMessage.text = dash.name + ": Movement foundation is incomplete. "
                    + "Restore required components or re-enable PlayerDash in Play Mode to add missing dependencies.";
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
