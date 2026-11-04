using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Shows prefab settings and setup problems without writing runtime messages to the Console.
    [CustomEditor(typeof(PlayerVFX)), CanEditMultipleObjects]
    public sealed class PlayerVFXEditor : UnityEditor.Editor
    {
        private static readonly Color Accent = new Color(1f, 0.70f, 0.35f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);

        // Unity calls this to build the settings panel when the component is selected.
        public override VisualElement CreateInspectorGUI()
        {
            PrepareSelection();
            serializedObject.Update();
            var root = CreatePanel(10, new Color(0.08f, 0.08f, 0.08f));
            root.style.marginTop = root.style.marginBottom = 4;
            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;
            Label title = CreateLabel("Player VFX", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);
            Label description = CreateLabel("Step and dash effects for this player.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            root.Add(header);
            VisualElement run = AddSection(root, "Run");
            AddField(run, "runPrefab", "Prefab", null, "One-shot Particle System or Visual Effect prefab played on Walk and Run footstep markers.");
            AddField(run, "runSpawnPoint", "Spawn Point", null, "Local VFX/Run transform. Its world position and rotation set the emission point; scale comes from the prefab.");
            VisualElement dash = AddSection(root, "Dash");
            AddField(dash, "dashPrefab", "Prefab", null, "One-shot Particle System or Visual Effect prefab played for each accepted dash.");
            AddField(dash, "dashSpawnPoint", "Spawn Point", null, "Local VFX/Dash transform. Place VFX under the visual model if its points should turn with that model.");
            VisualElement cleanup = AddSection(root, "Cleanup");
            AddField(cleanup, "maximumLifetime", "Maximum Lifetime", "s", "Lifetime in scaled game seconds. Visual Effect Graph copies stay until this limit; particle-only copies can finish earlier. A short limit cuts off the effect.");
            var issues = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            issues.style.marginTop = issues.style.marginBottom = 6;
            root.Add(issues);
            void RefreshIssues()
            {
                string message = null;
                foreach (Object selected in targets)
                {
                    if (!(selected is PlayerVFX vfx) || vfx == null) continue;
                    string issue = GetIssue(vfx);
                    if (issue != null) { message = issue; break; }
                }
                issues.text = message ?? string.Empty;
                issues.style.display = message == null ? DisplayStyle.None : DisplayStyle.Flex;
            }
            RefreshIssues();
            // Follow setup changes while the Inspector is open, without printing to the Console.
            root.schedule.Execute(RefreshIssues).Every(500);
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        // Fill empty references in editable scenes, with Undo support and no changes to manual choices.
        private void PrepareSelection()
        {
            if (Application.isPlaying) return;
            foreach (Object selected in targets)
            {
                if (!(selected is PlayerVFX vfx) || vfx == null || EditorUtility.IsPersistent(vfx)
                    || !vfx.gameObject.scene.IsValid() || !vfx.gameObject.scene.isLoaded
                    || (vfx.hideFlags & HideFlags.NotEditable) != 0
                    || (vfx.gameObject.hideFlags & HideFlags.NotEditable) != 0) continue;
                Undo.RecordObject(vfx, "Set Player VFX Points");
                Transform previousRun = vfx.RunSpawnPoint;
                Transform previousDash = vfx.DashSpawnPoint;
                vfx.AssignMissingSpawnPoints();
                if (previousRun != vfx.RunSpawnPoint || previousDash != vfx.DashSpawnPoint)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(vfx);
                    EditorUtility.SetDirty(vfx);
                }
                // Animation markers arrive on the model; Audio and VFX share its receiver.
                Animator animator = vfx.ResolvedFootstepAnimator;
                if (animator != null && animator.GetComponent<PlayerFootstepEvents>() == null
                    && !EditorUtility.IsPersistent(animator) && animator.gameObject.scene.IsValid()
                    && animator.gameObject.scene.isLoaded && (animator.hideFlags & HideFlags.NotEditable) == 0
                    && (animator.gameObject.hideFlags & HideFlags.NotEditable) == 0)
                    Undo.AddComponent<PlayerFootstepEvents>(animator.gameObject);
            }
        }

        // A correct setup needs no warning; show the first problem that needs attention.
        private static string GetIssue(PlayerVFX vfx)
        {
            string issue = PlayerVFXInstances.GetPrefabIssue(vfx.RunPrefab);
            if (issue != null) return "Run: " + issue;
            issue = PlayerVFXInstances.GetPrefabIssue(vfx.DashPrefab);
            if (issue != null) return "Dash: " + issue;
            if (vfx.RunPrefab != null && !vfx.IsLocalPoint(vfx.RunSpawnPoint))
                return "Run: assign a point under this player, or reopen this Inspector after creating one unambiguous VFX/Run.";
            if (vfx.DashPrefab != null && !vfx.IsLocalPoint(vfx.DashSpawnPoint))
                return "Dash: assign a point under this player, or reopen this Inspector after creating one unambiguous VFX/Dash.";
            if (vfx.RunPrefab != null)
            {
                Animator animator = vfx.ResolvedFootstepAnimator;
                if (animator == null) return "Run: select a local Model Animator in Player Animation, or provide one unambiguous local Animator.";
                PlayerFootstepEvents receiver = animator.GetComponent<PlayerFootstepEvents>();
                if (receiver == null) return "Run: reopen this Inspector in an editable scene or Prefab Mode to prepare OnFootstep. It is also prepared when Play Mode starts.";
                if (!receiver.enabled) return "Run: enable Player Footstep Events on the selected Animator.";
            }
            return null;
        }

        // Draw a saved setting with an optional unit and help over its text label only.
        private void AddField(VisualElement parent, string propertyName, string caption, string unit, string help)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = row.style.marginBottom = 2;
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var field = new PropertyField(property, caption) { name = propertyName + "-field" };
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);
            if (!string.IsNullOrEmpty(unit))
            {
                VisualElement badge = CreatePanel(0, new Color(0.24f, 0.17f, 0.10f));
                badge.name = propertyName + "-unit";
                badge.style.width = 44;
                badge.style.height = 20;
                badge.style.flexShrink = 0;
                badge.style.marginLeft = 6;
                badge.style.justifyContent = Justify.Center;
                Label text = CreateLabel(unit, 10, Accent);
                text.style.unityFontStyleAndWeight = FontStyle.Bold;
                text.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.Add(text);
                row.Add(badge);
            }
            parent.Add(row);
            // Connect the input to saved data so Unity can save edits and undo them.
            field.BindProperty(property);
            MovementInspectorLayout.FixedLabel(field);
            MovementInspectorTooltips.LabelOnly(row, field, help);
        }

        // Group related settings in the same dark cards used by the other Player Inspectors.
        private static VisualElement AddSection(VisualElement root, string caption)
        {
            VisualElement section = CreatePanel(8, new Color(0.12f, 0.12f, 0.12f));
            section.style.marginBottom = 6;
            Label heading = CreateLabel(caption, 11, Accent);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6;
            section.Add(heading);
            root.Add(section);
            return section;
        }

        private static Label CreateLabel(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static VisualElement CreatePanel(int padding, Color background)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = background;
            panel.style.paddingTop = panel.style.paddingBottom = padding;
            panel.style.paddingLeft = panel.style.paddingRight = padding;
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = 5;
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 5;
            panel.style.borderLeftWidth = panel.style.borderRightWidth = 1;
            panel.style.borderTopWidth = panel.style.borderBottomWidth = 1;
            panel.style.borderLeftColor = panel.style.borderRightColor = Line;
            panel.style.borderTopColor = panel.style.borderBottomColor = Line;
            return panel;
        }
    }
}
