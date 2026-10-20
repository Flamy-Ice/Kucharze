using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Keeps enum menus inside Inspector content so they scroll with their fields.
    internal static class MovementInspectorEnumDropdown
    {
        private static readonly Color Line = new Color(0.24f, 0.27f, 0.32f);
        private static readonly Color Text = new Color(0.92f, 0.93f, 0.95f);
        internal static VisualElement Add(VisualElement parent, VisualElement inspector, UnityEditor.Editor owner,
            string propertyName, string caption, string tooltip, Color accent, Color? highlightedBackground = null)
        {
            SerializedObject serializedObject = owner.serializedObject;
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            string[] names = property.enumDisplayNames;
            var field = new VisualElement { name = propertyName + "-field" };
            field.AddToClassList("unity-base-field");
            field.style.flexDirection = FlexDirection.Row;
            field.style.alignItems = Align.Center;
            field.style.marginTop = field.style.marginBottom = 2;
            var label = new Label(caption);
            label.AddToClassList("unity-base-field__label");
            label.style.height = 20;
            label.style.marginTop = label.style.marginBottom = 0;
            label.style.paddingTop = label.style.paddingBottom = 0;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            field.Add(label);
            var selector = new Button { name = propertyName + "-selector" };
            selector.AddToClassList("unity-base-field__input");
            selector.style.flexGrow = 1;
            selector.style.minWidth = 0;
            selector.style.backgroundColor = new Color(0.12f, 0.13f, 0.14f);
            selector.style.color = Text;
            selector.style.flexDirection = FlexDirection.Row;
            selector.style.alignItems = Align.Center;
            selector.style.height = 20;
            selector.style.marginLeft = selector.style.marginRight = 0;
            selector.style.paddingLeft = selector.style.paddingRight = 8;
            selector.style.paddingTop = selector.style.paddingBottom = 0;
            selector.style.borderLeftWidth = selector.style.borderRightWidth = 1;
            selector.style.borderTopWidth = selector.style.borderBottomWidth = 1;
            selector.style.borderLeftColor = selector.style.borderRightColor = new Color(0.25f, 0.31f, 0.34f);
            selector.style.borderTopColor = selector.style.borderBottomColor = new Color(0.25f, 0.31f, 0.34f);
            selector.style.borderTopLeftRadius = selector.style.borderTopRightRadius = 4;
            selector.style.borderBottomLeftRadius = selector.style.borderBottomRightRadius = 4;
            var valueLabel = CreateLabel(string.Empty, 12, Text);
            valueLabel.name = propertyName + "-value";
            valueLabel.style.flexGrow = 1;
            valueLabel.style.minWidth = 0;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            selector.Add(valueLabel);
            var arrow = CreateLabel("▾", 12, accent);
            arrow.style.width = 16;
            arrow.style.flexShrink = 0;
            arrow.style.unityTextAlign = TextAnchor.MiddleRight;
            selector.Add(arrow);
            field.Add(selector);
            parent.Add(field);
            void RefreshSelection()
            {
                // A dash means the selected players currently use different values.
                valueLabel.text = property.hasMultipleDifferentValues ? "—"
                    : names[Mathf.Clamp(property.enumValueIndex, 0, names.Length - 1)];
            }
            EnumMenu menu = null;
            VisualElement eventRoot = null;
            void CloseMenu()
            {
                if (eventRoot != null)
                    eventRoot.UnregisterCallback<PointerDownEvent>(CloseOnOutsideClick, TrickleDown.TrickleDown);
                eventRoot = null;
                EnumMenu oldMenu = menu;
                menu = null;
                oldMenu?.RemoveFromHierarchy();
            }
            void CloseOnOutsideClick(PointerDownEvent evt)
            {
                VisualElement clicked = evt.target as VisualElement;
                if (clicked != null && menu != null && !menu.Contains(clicked) && clicked != menu &&
                    !selector.Contains(clicked) && clicked != selector)
                    CloseMenu();
            }
            void PositionMenu()
            {
                if (menu == null)
                    return;
                Vector2 position = inspector.WorldToLocal(selector.worldBound.position);
                menu.style.left = position.x;
                menu.style.top = position.y + selector.worldBound.height + 2f;
                menu.style.width = selector.worldBound.width;
            }
            selector.clicked += () =>
            {
                if (menu != null)
                {
                    CloseMenu();
                    return;
                }
                Color highlight = highlightedBackground ?? Color.Lerp(new Color(0.1f, 0.11f, 0.12f), accent, 0.16f);
                menu = new EnumMenu(propertyName, names, accent, highlight,
                    property.hasMultipleDifferentValues ? -1 : property.enumValueIndex, index =>
                {
                    if (owner != null && owner.target != null)
                    {
                        serializedObject.Update();
                        property.enumValueIndex = index;
                        serializedObject.ApplyModifiedProperties();
                        RefreshSelection();
                    }
                    CloseMenu();
                    selector.Focus();
                }, CloseMenu);
                // Keep the dropdown inside the Inspector content so it scrolls with its own field.
                inspector.Add(menu);
                PositionMenu();
                eventRoot = inspector.panel.visualTree;
                eventRoot.RegisterCallback<PointerDownEvent>(CloseOnOutsideClick, TrickleDown.TrickleDown);
                menu.Focus();
            };
            selector.RegisterCallback<GeometryChangedEvent>(_ => PositionMenu());
            inspector.RegisterCallback<DetachFromPanelEvent>(_ => CloseMenu());
            field.TrackPropertyValue(property, _ => RefreshSelection());
            RefreshSelection();
            MovementInspectorTooltips.LabelOnly(field, field, tooltip);
            MovementInspectorLayout.FixedLabel(field);
            return field;
        }

        // This dropdown belongs to the Inspector content rather than a separate floating window.
        private sealed class EnumMenu : VisualElement
        {
            private readonly Button[] options;
            private readonly Color highlight;
            private int highlighted;

            internal EnumMenu(string propertyName, string[] names, Color accent, Color highlight, int selected, Action<int> choose, Action close)
            {
                name = propertyName + "-menu";
                options = new Button[names.Length];
                this.highlight = highlight;
                focusable = true;
                style.position = Position.Absolute;
                style.backgroundColor = new Color(0.1f, 0.11f, 0.12f);
                style.paddingTop = style.paddingBottom = 3;
                style.borderLeftWidth = style.borderRightWidth = 1;
                style.borderTopWidth = style.borderBottomWidth = 1;
                style.borderLeftColor = style.borderRightColor = Line;
                style.borderTopColor = style.borderBottomColor = Line;
                highlighted = Mathf.Max(0, selected);
                for (int index = 0; index < names.Length; index++)
                {
                    // Each button remembers its own option instead of the loop's changing index.
                    int choice = index;
                    var option = new Button(() => choose(choice))
                    {
                        name = propertyName + "-option-" + index,
                        text = (index == selected ? "✓  " : "    ") + names[index]
                    };
                    option.style.height = 22;
                    option.style.paddingTop = option.style.paddingBottom = 0;
                    option.style.marginTop = option.style.marginBottom = 0;
                    option.style.marginLeft = option.style.marginRight = 3;
                    option.style.borderLeftWidth = option.style.borderRightWidth = 0;
                    option.style.borderTopWidth = option.style.borderBottomWidth = 0;
                    option.style.color = index == selected ? accent : Text;
                    option.style.fontSize = 12;
                    option.style.unityTextAlign = TextAnchor.MiddleLeft;
                    option.RegisterCallback<PointerEnterEvent>(_ =>
                    {
                        highlighted = choice;
                        RefreshHighlight();
                    });
                    options[index] = option;
                    Add(option);
                }
                RefreshHighlight();
                RegisterCallback<TooltipEvent>(evt =>
                {
                    evt.tooltip = string.Empty;
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);
                RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape)
                        close();
                    else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                        choose(highlighted);
                    else if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow ||
                        evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.RightArrow)
                    {
                        bool previous = evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.LeftArrow;
                        highlighted = (highlighted + (previous ? names.Length - 1 : 1)) % names.Length;
                        RefreshHighlight();
                    }
                    else
                        return;
                    // This menu has handled the key; other controls should not handle it again.
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);
                RegisterCallback<FocusOutEvent>(evt =>
                {
                    if (!(evt.relatedTarget is VisualElement next) || !Contains(next))
                        close();
                });
            }

            private void RefreshHighlight()
            {
                for (int index = 0; index < options.Length; index++)
                    options[index].style.backgroundColor = index == highlighted
                        ? highlight : new Color(0.1f, 0.11f, 0.12f);
            }
        }
        private static Label CreateLabel(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            // Fill the button's inner height so text and arrow share its vertical center.
            label.style.alignSelf = Align.Stretch;
            label.style.marginTop = label.style.marginBottom = 0;
            label.style.paddingTop = label.style.paddingBottom = 0;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            return label;
        }
    }
}
