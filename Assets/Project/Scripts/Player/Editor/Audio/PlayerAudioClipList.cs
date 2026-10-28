using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Keeps Unity's normal clip list inside a softly separated settings area.
    internal sealed class PlayerAudioClipList : PropertyField
    {
        private static readonly Color Background = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color Border = new Color(0.30f, 0.31f, 0.34f);
        private readonly System.Collections.Generic.HashSet<VisualElement> configuredFields =
            new System.Collections.Generic.HashSet<VisualElement>();
        private Toggle headerToggle;

        public PlayerAudioClipList(SerializedObject settings)
            : base(settings.FindProperty("walkClips"), "Walk Clips")
        {
            name = "walk-clips-list";
            style.backgroundColor = Background;
            style.marginTop = 4;
            style.marginBottom = 10;
            style.paddingLeft = style.paddingRight = 6;
            style.paddingTop = 5;
            style.paddingBottom = 2;
            style.overflow = Overflow.Visible;
            style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderTopWidth = style.borderBottomWidth = 1;
            style.borderLeftColor = style.borderRightColor = Border;
            style.borderTopColor = style.borderBottomColor = Border;
            style.borderTopLeftRadius = style.borderTopRightRadius = 4;
            style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            this.BindProperty(settings.FindProperty("walkClips"));
            RegisterCallback<TooltipEvent>(evt =>
            {
                VisualElement hovered = evt.target as VisualElement;
                if (hovered != null && ((headerToggle != null && headerToggle.Contains(hovered))
                    || hovered is ObjectField || hovered.GetFirstAncestorOfType<ObjectField>() != null
                    || hovered is IntegerField || hovered.GetFirstAncestorOfType<IntegerField>() != null)) return;
                evt.tooltip = string.Empty;
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            RegisterCallback<AttachToPanelEvent>(_ => RefreshBackgrounds());
            RegisterCallback<GeometryChangedEvent>(_ => RefreshBackgrounds());
            // Unity can create or reuse list rows later, so keep their appearance up to date.
            schedule.Execute(RefreshBackgrounds).Every(250);
        }

        private void RefreshBackgrounds()
        {
            ListView list = this.Q<ListView>();
            if (list == null) return;
            list.style.backgroundColor = Background;
            list.style.overflow = Overflow.Visible;
            ScrollView scrollView = list.Q<ScrollView>();
            if (scrollView != null) scrollView.style.backgroundColor = Background;
            VisualElement footer = list.Q(className: BaseListView.footerUssClassName);
            Foldout foldout = list.Q<Foldout>();
            if (foldout != null)
            {
                foldout.style.backgroundColor = Background;
                foldout.style.overflow = Overflow.Visible;
                foldout.contentContainer.style.overflow = Overflow.Visible;
            }
            PositionFooter(footer, foldout);
            headerToggle = foldout?.Q<Toggle>();
            if (headerToggle != null)
            {
                headerToggle.style.marginLeft = 6;
                ConfigureLabel(headerToggle,
                    "Recordings shared by walking and running. Each step randomly chooses a nonempty entry.");
            }
            list.Query<ObjectField>().ForEach(field => ConfigureLabel(field,
                "Footstep recording. Empty entries are skipped."));
            list.Query<IntegerField>().ForEach(field => ConfigureLabel(field,
                "Number of footstep recordings in the list."));
        }

        private void PositionFooter(VisualElement footer, Foldout foldout)
        {
            if (footer == null)
            {
                style.marginBottom = 10;
                return;
            }
            // Keep the native buttons attached below the last row, outside the panel's border.
            footer.style.position = Position.Absolute;
            footer.style.top = Length.Percent(100);
            footer.style.bottom = StyleKeyword.Auto;
            footer.style.right = 0;
            footer.style.marginTop = -1;
            footer.style.backgroundColor = Background;
            footer.style.borderTopWidth = 0;
            footer.style.borderLeftWidth = footer.style.borderRightWidth = footer.style.borderBottomWidth = 1;
            footer.style.borderLeftColor = footer.style.borderRightColor = footer.style.borderBottomColor = Border;
            footer.style.borderBottomLeftRadius = footer.style.borderBottomRightRadius = 4;
            // Leave enough space for the protruding tab without changing Unity's button sizes.
            float height = footer.resolvedStyle.height;
            if (float.IsNaN(height) || float.IsInfinity(height) || height <= 0f) height = 20f;
            style.marginBottom = foldout == null || foldout.value ? height + 10f : 10f;
        }

        private void ConfigureLabel(VisualElement field, string tooltip)
        {
            if (!configuredFields.Add(field)) return;
            MovementInspectorTooltips.LabelOnly(field, field, tooltip);
        }
    }
}
