using UnityEngine;

namespace ModularMovement
{
    // Keeps the HUD clear of notches and Android's system navigation area.
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class HudSafeArea : MonoBehaviour
    {
        private Rect lastArea;
        private Vector2Int lastSize;
        private void OnEnable() => Apply();
        private void Update() => Apply();

        public void Apply()
        {
            int width = UnityEngine.Device.Screen.width;
            int height = UnityEngine.Device.Screen.height;
            if (width <= 0 || height <= 0) return;
            Rect area = UnityEngine.Device.Screen.safeArea;
            var size = new Vector2Int(width, height);
            if (area == lastArea && size == lastSize) return;
            lastArea = area;
            lastSize = size;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            rect.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
