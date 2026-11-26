#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Displays the server's ready-player count and start countdown on the room sign.
    [DisallowMultipleComponent, RequireComponent(typeof(TextMesh))]
    public sealed class LobbyReadinessSign : MonoBehaviour
    {
        private TextMesh label;
        private LobbyConnectionView view;
        private string lastText;

        private void Awake() => label = GetComponent<TextMesh>();

        private void LateUpdate()
        {
            if (view == null && NetworkManager.Singleton != null)
                view = NetworkManager.Singleton.GetComponent<LobbyConnectionView>();
            LobbySession session = view != null ? view.Session : null;
            int total = session != null ? session.PlayerCount : 0;
            int ready = session != null ? session.ReadyCount : 0;
            string status = session == null || !session.HasSelectedLevel ? "HOST: SELECT A LEVEL"
                : total < 2 ? "2 PLAYERS MINIMUM" : "STEP INSIDE";
            if (session != null)
            {
                if (session.IsLoading) status = "LOADING...";
                else if (!string.IsNullOrEmpty(session.Error)) status = "LEAVE AND RE-ENTER";
                else if (session.SecondsRemaining > 0) status = "STARTING IN " + session.SecondsRemaining + "...";
            }
            string text = "READY ROOM\n" + ready + "/" + total + " READY\n" + status;
            if (lastText == text) return;
            lastText = text;
            label.text = text;
        }
    }
}
#endif
