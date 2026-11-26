using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Reveals the chosen row once when a dropdown opens, then leaves scrolling to the player.
    [DisallowMultipleComponent, RequireComponent(typeof(Dropdown))]
    public sealed class MenuDropdownSelectionScroll : MonoBehaviour
    {
        private Transform lastList;

        private void LateUpdate()
        {
            Transform list = transform.Find("Dropdown List");
            if (list == null || list == lastList || !list.gameObject.activeInHierarchy) return;
            lastList = list;
            ScrollRect scroll = list.GetComponentInChildren<ScrollRect>();
            if (scroll == null || scroll.content == null) return;
            Toggle selected = null;
            foreach (Toggle row in scroll.content.GetComponentsInChildren<Toggle>())
                if (row.isOn) { selected = row; break; }
            if (selected == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            Bounds content = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, scroll.content);
            Bounds item = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
            float hiddenHeight = content.size.y - viewport.rect.height;
            if (hiddenHeight <= 0) return;
            scroll.StopMovement();
            float shift = viewport.rect.center.y - item.center.y;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - shift / hiddenHeight);
        }
    }
}
