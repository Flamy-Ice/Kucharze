#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Supplies replicated lobby details to scene displays and keeps the leave action available.
    [RequireComponent(typeof(RelayConnection), typeof(NetworkManager))]
    public sealed class LobbyConnectionView : MonoBehaviour
    {
        private RelayConnection connection;
        private NetworkManager manager;
        private LobbySession session;
        public string HiddenScene { get; set; }
        public LobbySession Session => session != null && session.IsSpawned ? session : null;
        public string JoinCode => Session != null && !string.IsNullOrEmpty(Session.JoinCode)
            ? Session.JoinCode : connection != null ? connection.JoinCode : "";

        private void Awake()
        {
            connection = GetComponent<RelayConnection>();
            manager = GetComponent<NetworkManager>();
        }

        private void Update()
        {
            if (session != null && session.IsSpawned) return;
            session = null;
            if (manager == null || !manager.IsListening || manager.SpawnManager == null) return;
            foreach (NetworkObject instance in manager.SpawnManager.SpawnedObjects.Values)
                if (instance.TryGetComponent(out LobbySession found) && found.IsSpawned)
                {
                    session = found;
                    break;
                }
        }

        private void OnGUI()
        {
            if (manager == null || !manager.IsListening || connection == null
                || (!string.IsNullOrEmpty(HiddenScene) && SceneManager.GetActiveScene().path == HiddenScene)) return;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1000f, Screen.height / 720f), 0.5f, 1.6f);
            Matrix4x4 previous = GUI.matrix;
            try
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
                Rect safe = Screen.safeArea;
                var button = new Rect(safe.xMax / scale - 100, (Screen.height - safe.yMax) / scale + 18, 82, 34);
                if (GUI.Button(button, "Leave")) connection.Disconnect();
            }
            finally { GUI.matrix = previous; }
        }
    }
}
#endif
