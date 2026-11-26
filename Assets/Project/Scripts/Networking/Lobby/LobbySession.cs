#if KUCHARZE_ONLINE
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Checks everyone on the server and changes scenes together when the group is ready.
    [RequireComponent(typeof(NetworkObject), typeof(LobbyLevelSelection))]
    public sealed class LobbySession : NetworkBehaviour
    {
        [SerializeField] private string lobbyScene;
        [SerializeField, Tooltip("Legacy destination used to seed Level 1 during editor setup. Players still need to select it.")]
        private string gameScene;
        [SerializeField, Min(0.1f), Tooltip("Time everyone must stay inside the start zone, in seconds.")]
        private float readySeconds = 5f;
        private readonly NetworkVariable<int> total = new NetworkVariable<int>();
        private readonly NetworkVariable<int> ready = new NetworkVariable<int>();
        private readonly NetworkVariable<double> deadline = new NetworkVariable<double>();
        private readonly NetworkVariable<bool> inLobby = new NetworkVariable<bool>(true);
        private readonly NetworkVariable<bool> loading = new NetworkVariable<bool>();
        private readonly NetworkVariable<FixedString128Bytes> error = new NetworkVariable<FixedString128Bytes>();
        private readonly NetworkVariable<FixedString64Bytes> joinCode = new NetworkVariable<FixedString64Bytes>();
        private readonly LobbyRoomAssignments rooms = new LobbyRoomAssignments();
        private readonly LobbyHostLevelSelector hostLevelSelector = new LobbyHostLevelSelector();
        private readonly List<PlayerMovementResolver> pausedMovement = new List<PlayerMovementResolver>();
        private LobbyLayout layout;
        private LobbyLevelZones levelZones;
        private LobbyLevelSelection levelSelection;
        private string pendingScene;
        private bool sceneFinished;

        public bool AcceptingPlayers => IsSpawned && inLobby.Value && !loading.Value;
        public bool IsLobby => inLobby.Value;
        public bool IsLoading => loading.Value;
        public int PlayerCount => total.Value;
        public int ReadyCount => ready.Value;
        public string Error => error.Value.ToString();
        public string JoinCode => joinCode.Value.ToString();
        public string LobbyScenePath => lobbyScene;
        public string DefaultGameScenePath => gameScene;
        public LobbyLevelSelection LevelSelection => levelSelection;
        public bool HasSelectedLevel => levelSelection != null && levelSelection.HasSelection;
        public int SecondsRemaining => deadline.Value <= 0 ? 0 : Mathf.Max(0,
            Mathf.CeilToInt((float)(deadline.Value - NetworkManager.ServerTime.Time)));

        public override void OnNetworkSpawn()
        {
            levelSelection = GetComponent<LobbyLevelSelection>();
            if (!IsServer) return;
            NetworkManager.OnClientConnectedCallback += OnRosterChanged;
            NetworkManager.OnClientDisconnectCallback += OnRosterChanged;
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            FindLayout();
        }

        private void OnRosterChanged(ulong clientId)
        {
            deadline.Value = 0;
            error.Value = default;
        }

        private void FindLayout()
        {
            layout = null;
            levelZones = null;
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (layout == null) layout = root.GetComponentInChildren<LobbyLayout>();
                if (levelZones == null) levelZones = root.GetComponentInChildren<LobbyLevelZones>();
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer) return;
            RelayConnection connection = NetworkManager.GetComponent<RelayConnection>();
            if (connection != null) joinCode.Value = new FixedString64Bytes(connection.JoinCode);
            // Scene requests wait until the preceding scene event has fully finished.
            if (sceneFinished)
            {
                sceneFinished = false;
                loading.Value = false;
                rooms.BeginScene();
                FindLayout();
            }
            total.Value = NetworkManager.ConnectedClients.Count;
            rooms.Refresh(NetworkManager);
            if (pendingScene != null && !loading.Value)
            {
                string scene = pendingScene;
                pendingScene = null;
                BeginSceneChange(scene);
                return;
            }
            if (loading.Value) return;
            if (layout == null) FindLayout();
            rooms.PlacePlayers(NetworkManager, layout);
            RestoreMovement();
            if (!inLobby.Value)
            {
                if (total.Value < 2) ReturnToLobby();
                return;
            }
            if (hostLevelSelector.TrySelect(NetworkManager, levelSelection, levelZones, Time.unscaledDeltaTime))
            {
                deadline.Value = 0;
                error.Value = default;
            }
            int inside = 0;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
                if (client.PlayerObject != null && client.PlayerObject.IsSpawned && layout != null
                    && layout.Contains(client.PlayerObject.transform.position)) inside++;
            ready.Value = inside;
            if (total.Value < 2 || total.Value > 4 || inside != total.Value || !HasSelectedLevel)
            {
                deadline.Value = 0;
                error.Value = default;
                return;
            }
            if (error.Value.Length > 0) return;
            if (deadline.Value <= 0) deadline.Value = NetworkManager.ServerTime.Time + readySeconds;
            if (NetworkManager.ServerTime.Time >= deadline.Value) BeginSceneChange(levelSelection.SelectedScenePath);
        }

        private void BeginSceneChange(string scene)
        {
            deadline.Value = 0;
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
            {
                error.Value = new FixedString128Bytes("The destination scene is missing from the build scene list.");
                return;
            }
            bool destinationLobby = scene == lobbyScene;
            SceneEventProgressStatus result = NetworkManager.SceneManager.LoadScene(scene, LoadSceneMode.Single);
            if (result != SceneEventProgressStatus.Started)
            {
                error.Value = new FixedString128Bytes("Scene loading could not start. Leave and enter the start room to retry.");
                return;
            }
            inLobby.Value = destinationLobby;
            loading.Value = true;
            error.Value = default;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                PlayerMovementResolver resolver = client.PlayerObject != null
                    ? client.PlayerObject.GetComponent<PlayerMovementResolver>() : null;
                if (resolver == null || !resolver.SimulationEnabled) continue;
                pausedMovement.Add(resolver);
                resolver.SimulationEnabled = false;
            }
        }

        private void RestoreMovement()
        {
            foreach (PlayerMovementResolver resolver in pausedMovement)
                if (resolver != null) resolver.SimulationEnabled = true;
            pausedMovement.Clear();
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted) sceneFinished = true;
        }

        // A future summary screen can call this on the server after the round ends.
        public void ReturnToLobby()
        {
            if (!IsServer || !IsSpawned || inLobby.Value) return;
            pendingScene = lobbyScene;
        }

        public override void OnNetworkDespawn()
        {
            RestoreMovement();
            if (NetworkManager == null || !IsServer) return;
            NetworkManager.OnClientConnectedCallback -= OnRosterChanged;
            NetworkManager.OnClientDisconnectCallback -= OnRosterChanged;
            if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }
}
#endif
