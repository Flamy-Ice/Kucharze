using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Keeps audio settings in the same readable layout as the movement Inspectors.
    [CustomEditor(typeof(PlayerAudio)), CanEditMultipleObjects]
    public sealed class PlayerAudioEditor : UnityEditor.Editor
    {
        private static readonly Color Accent = new Color(0.78f, 0.64f, 1f);
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);

        public override VisualElement CreateInspectorGUI()
        {
            var root = CreatePanel(10, new Color(0.08f, 0.08f, 0.08f));
            root.style.marginTop = root.style.marginBottom = 4;
            var header = new VisualElement();
            header.style.marginBottom = 10;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Line;
            var title = CreateLabel("Player Audio", 14, Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            header.Add(title);
            var description = CreateLabel("Footsteps and dash sounds for this player.", 10,
                new Color(0.62f, 0.65f, 0.7f));
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(description);
            root.Add(header);

            VisualElement output = AddSection(root, "Output");
            AddField(output, "outputMixerGroup", "Audio Mixer Group", null,
                "Routes footsteps and dash sounds to this mixer group. Leave empty for direct output to the AudioListener.");
            VisualElement shared = AddSection(root, "Shared");
            AddField(shared, "pitchVariation", "Pitch Variation", "±ratio",
                "Random pitch offset for footsteps and dash sounds. 0.08 gives 0.92 to 1.08. Zero disables pitch variation.");
            VisualElement footsteps = AddSection(root, "Walk");
            AddWalkClips(footsteps);
            var eventSetup = new HelpBox("", HelpBoxMessageType.Warning);
            eventSetup.style.marginTop = eventSetup.style.marginBottom = 6;
            footsteps.Add(eventSetup);
            void RefreshEventSetup(bool provision)
            {
                string message = null;
                foreach (Object selected in targets)
                {
                    if (!(selected is PlayerAudio audio) || audio == null) continue;
                    string issue = PlayerAudioEventSetup.EnsureReceiver(audio, provision);
                    if (message == null) message = issue;
                }
                eventSetup.style.display = message != null ? DisplayStyle.Flex : DisplayStyle.None;
                eventSetup.text = message ?? string.Empty;
            }
            // Keep checking the setup, but do not add back a component removed with Undo.
            root.schedule.Execute(() => RefreshEventSetup(false)).Every(500);
            RefreshEventSetup(true);
            AddField(footsteps, "footstepVolume", "Footstep Volume", "0–1", "Footstep loudness from silent (0) to full volume (1).");
            VisualElement dash = AddSection(root, "Dash");
            AddField(dash, "dashClip", "Dash Clip", null, "Sound played once for each accepted dash.");
            AddField(dash, "dashVolume", "Dash Volume", "0–1", "Dash loudness from silent (0) to full volume (1).");
            MovementInspectorCheckboxes.Apply(root);
            return root;
        }

        private void AddWalkClips(VisualElement card)
        {
            var clipList = new PlayerAudioClipList(serializedObject);
            // Load the list's appearance file beside this script, so moving the folder keeps it working.
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string folder = System.IO.Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder))
            {
                StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(folder + "/PlayerAudioClipList.uss");
                if (sheet != null) clipList.styleSheets.Add(sheet);
            }
            card.Add(clipList);
        }

        private void AddField(VisualElement card, string propertyName, string label, string unit, string tooltip)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = row.style.marginBottom = 2;
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var field = new PropertyField(property, label) { name = propertyName + "-field" };
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.flexBasis = 0;
            field.style.minWidth = 0;
            row.Add(field);
            if (!string.IsNullOrEmpty(unit))
            {
                VisualElement badge = CreatePanel(0, new Color(0.18f, 0.13f, 0.24f));
                badge.style.width = 44;
                badge.style.height = 20;
                badge.style.flexShrink = 0;
                badge.style.marginLeft = 6;
                badge.style.justifyContent = Justify.Center;
                Label unitLabel = CreateLabel(unit, 10, Accent);
                unitLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                unitLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.Add(unitLabel);
                row.Add(badge);
            }
            card.Add(row);
            field.BindProperty(property);
            MovementInspectorTooltips.LabelOnly(row, field, tooltip);
            MovementInspectorLayout.FixedLabel(field);
        }

        private static VisualElement AddSection(VisualElement root, string name)
        {
            VisualElement card = CreatePanel(8, new Color(0.12f, 0.12f, 0.12f));
            card.style.marginBottom = 6;
            Label heading = CreateLabel(name, 11, Accent);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6;
            card.Add(heading);
            root.Add(card);
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
