#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Supplies replicated lobby details to scene displays.
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

    }
}
#endif
