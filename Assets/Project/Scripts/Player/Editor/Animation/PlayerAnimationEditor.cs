using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Builds PlayerAnimation's Inspector and shows setup advice beside its settings.
    // This editor-only script does not choose or play the player's animations.
    [CustomEditor(typeof(PlayerAnimation)), CanEditMultipleObjects]
    public sealed class PlayerAnimationEditor : UnityEditor.Editor
    {
        // Reuse the movement Inspectors' dark theme, with a peach accent for animation.
        private static readonly Color Accent = new Color(1f, 0.72f, 0.48f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);
        private static readonly Color Text = new Color(0.92f, 0.93f, 0.95f);
        private HelpBox setupMessage;

        public override VisualElement CreateInspectorGUI()
        {
            // VisualElement is a container. Nesting containers creates the panel, heading and settings card.
            var root = new VisualElement();
            var container = CreatePanel(6, 10, new Color(0.08f, 0.08f, 0.08f));
            container.style.marginTop = container.style.marginBottom = 4;
            root.Add(container);

            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;
            var title = CreateLabel("Player Animation", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);
            var description = CreateLabel("Idle, Run and Dash from this player's completed movement.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            container.Add(header);

            var card = CreatePanel(5, 8, new Color(0.12f, 0.12f, 0.12f));
            card.name = "animation-settings-card";
            var heading = CreateLabel("Animation Settings", 11, Accent);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6;
            card.Add(heading);
            // These names point to PlayerAnimation's saved fields; the friendly labels appear in Unity.
            AddField(card, "modelAnimator", "Model Animator",
                "Animator on the visual child. Leave empty to find it.");
            AddField(card, "defaultController", "Default Controller",
                "Used only when the model has no controller.");
            container.Add(card);
            setupMessage = new HelpBox("", HelpBoxMessageType.Warning);
            setupMessage.style.marginTop = 6;
            container.Add(setupMessage);
            UpdateSetupMessage();
            // Recheck twice a second while this Inspector is open, including changes made elsewhere.
            root.schedule.Execute(UpdateSetupMessage).Every(500);
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        private void AddField(VisualElement root, string propertyName, string label, string tooltip)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = row.style.marginBottom = 2;
            var property = serializedObject.FindProperty(propertyName);
            // Use Unity's saved settings so edits can be undone and applied to several selected players.
            var field = new PropertyField(property, label) { name = propertyName + "-field" };
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);
            root.Add(row);
            field.BindProperty(property);
            // Keep help on the label only, with the same fixed label width as other movement settings.
            MovementInspectorTooltips.LabelOnly(row, field, tooltip);
            MovementInspectorLayout.FixedLabel(field);
        }

        private void UpdateSetupMessage()
        {
            // Start with no warning. For several selected players, show the first setup issue found.
            setupMessage.style.display = DisplayStyle.None;
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerAnimation animation) || animation == null) continue;
                AssignMissingController(animation);
                Animator animator = animation.ResolvedAnimator;
                string message = null;
                if (animator == null)
                    message = "Add an Animator to the visual child, then assign Model Animator. "
                        + "A single local child Animator is selected automatically; multiple Animators need a manual choice.";
                else
                {
                    // Validate the model's own controller first; the default is used only when it has none.
                    RuntimeAnimatorController controller = animator.runtimeAnimatorController != null
                        ? animator.runtimeAnimatorController : animation.DefaultController;
                    message = controller == null
                        ? "Assign Player.controller to Default Controller or the model's Animator."
                        : PlayerAnimationControllerValidation.GetIssue(controller);
                    if (message == null && !animator.isActiveAndEnabled)
                        message = "Enable the model and its Animator to display animations.";
                    else if (message == null && animator.applyRootMotion)
                        message = "Apply Root Motion will be turned off in Play Mode. PlayerMotor owns movement.";
                }
                if (message == null) continue;
                setupMessage.text = message;
                setupMessage.style.display = DisplayStyle.Flex;
                return;
            }
        }

        private static void AssignMissingController(PlayerAnimation animation)
        {
            // Fill only an empty choice outside Play Mode. Keep the developer's explicit assignment.
            if (Application.isPlaying || animation.DefaultController != null) return;
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(animation));
            // Find the expected local asset relative to the script, so moving the package still works.
            RuntimeAnimatorController controller = PlayerAnimation.FindDefaultController(scriptPath);
            if (controller == null) return;
            // Save the found controller on the player and allow the assignment to be undone.
            using (var selectedObject = new SerializedObject(animation))
            {
                selectedObject.FindProperty("defaultController").objectReferenceValue = controller;
                selectedObject.ApplyModifiedProperties();
            }
        }

        private static Label CreateLabel(string text, int size, Color color)
        {
            // Allow long help text to wrap instead of being cut off in a narrow Inspector.
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        // Matches the rounded panels and borders used by the other movement Inspectors.
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
