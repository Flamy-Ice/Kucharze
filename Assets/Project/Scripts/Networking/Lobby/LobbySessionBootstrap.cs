#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Creates session rules on the host and approves connections only while waiting in the lobby.
    [RequireComponent(typeof(NetworkManager))]
    public sealed class LobbySessionBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkObject sessionPrefab;
        private NetworkManager manager;
        private bool returnToEntry;
        public LobbySession Session { get; private set; }
        public string EntryScene { get; set; }

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            manager.OnServerStarted += OnServerStarted;
            manager.OnClientStopped += OnStopped;
            manager.ConnectionApprovalCallback += Approve;
        }

        private void OnServerStarted()
        {
            NetworkObject instance = Instantiate(sessionPrefab);
            instance.Spawn(false);
            Session = instance.GetComponent<LobbySession>();
        }

        private void OnStopped(bool wasHost) => returnToEntry = true;

        private void Update()
        {
            if (!returnToEntry || manager.IsListening || manager.ShutdownInProgress) return;
            returnToEntry = false;
            string scene = string.IsNullOrEmpty(EntryScene)
                ? sessionPrefab.GetComponent<LobbySession>().LobbyScenePath : EntryScene;
            // A disconnected client needs its local entry scene; it no longer has a server to load one.
            if (SceneManager.GetActiveScene().path != scene && Application.CanStreamedLevelBeLoaded(scene))
                SceneManager.LoadScene(scene);
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // The host's own player is approved before the session object is created.
            bool host = request.ClientNetworkId == NetworkManager.ServerClientId;
            bool allowed = host || (Session != null && Session.AcceptingPlayers && manager.ConnectedClients.Count < 4);
            response.Approved = allowed;
            response.CreatePlayerObject = allowed;
            response.Pending = false;
            response.Reason = allowed ? "" : "The game is in progress or the lobby is full.";
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnServerStarted -= OnServerStarted;
            manager.OnClientStopped -= OnStopped;
            manager.ConnectionApprovalCallback -= Approve;
        }
    }
}
#endif
