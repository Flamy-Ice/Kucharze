#if KUCHARZE_ONLINE
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Shows the existing session code on the lobby's timer-style panel.
    [DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class LobbyCodeDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("The parent area kept inside the phone's usable screen.")]
        private RectTransform safeArea;
        private TMP_Text label;
        private LobbyConnectionView view;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;
        private string lastCode;

        private void Awake() => label = GetComponent<TMP_Text>();

        private void LateUpdate()
        {
            if (view == null && NetworkManager.Singleton != null)
                view = NetworkManager.Singleton.GetComponent<LobbyConnectionView>();
            string code = view != null ? view.JoinCode : "";
            if (lastCode != code)
            {
                lastCode = code;
                label.text = "<size=22>LOBBY CODE</size>\n" + (string.IsNullOrEmpty(code) ? "------" : code);
            }
            FitSafeArea();
        }

        private void FitSafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect current = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (current == lastSafeArea && screen == lastScreen) return;
            lastSafeArea = current;
            lastScreen = screen;
            safeArea.anchorMin = new Vector2(current.xMin / Screen.width, current.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(current.xMax / Screen.width, current.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
    }
}
#endif
