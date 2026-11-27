#if KUCHARZE_ONLINE
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Creates a Relay code or joins one, while keeping connection errors visible in the menu.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class RelayConnection : MonoBehaviour
    {
        [SerializeField, Range(2, 16), Tooltip("Maximum players, including the host.")]
        private int maximumPlayers = 4;
        private NetworkManager manager;
        private UnityTransport transport;
        private int operationVersion;
        private bool busy;
        private bool connecting;
        private float connectionDeadline;

        public string JoinCode { get; private set; } = "";
        public string Status { get; private set; } = "Ready";
        public bool IsBusy => busy || connecting;
        public bool IsListening => manager != null && manager.IsListening;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            if (GetComponent<NetworkConnectionDiagnostics>() == null) gameObject.AddComponent<NetworkConnectionDiagnostics>();
            transport = GetComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
        }

        public void Host() => _ = ConnectAsync(true, "");
        public void HostInScene(string scenePath) => _ = ConnectAsync(true, "", scenePath);
        public void Join(string code) => _ = ConnectAsync(false, code);

        private async Task ConnectAsync(bool host, string code, string hostScene = null)
        {
            if (IsBusy || IsListening || manager.ShutdownInProgress) return;
            code = (code ?? "").Trim().ToUpperInvariant();
            if (!host && code.Length == 0)
            {
                Status = "Enter a join code.";
                return;
            }
            int version = ++operationVersion;
            busy = true;
            JoinCode = "";
            Status = "Connecting to Unity Services...";
            try
            {
                // Editor and standalone tests on one computer use different anonymous identities.
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    var options = new InitializationOptions();
                    options.SetProfile(Application.isEditor ? "editor" : "player");
                    await UnityServices.InitializeAsync(options);
                }
                if (!IsCurrent(version)) return;
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (!IsCurrent(version)) return;

                if (host)
                {
                    var allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Clamp(maximumPlayers, 2, 16) - 1);
                    if (!IsCurrent(version)) return;
                    string newCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    if (!IsCurrent(version)) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    // Create the host's player only after the lobby and its spawn markers are loaded.
                    if (!string.IsNullOrEmpty(hostScene) && SceneManager.GetActiveScene().path != hostScene)
                    {
                        if (!Application.CanStreamedLevelBeLoaded(hostScene))
                            throw new InvalidOperationException("The lobby is missing from the build scene list.");
                        Status = "Opening the lobby...";
                        await SceneManager.LoadSceneAsync(hostScene, LoadSceneMode.Single);
                        if (!IsCurrent(version)) return;
                    }
                    if (!manager.StartHost()) throw new InvalidOperationException("Could not start the host.");
                    JoinCode = newCode;
                    Status = "Hosting. Share the code with another player.";
                }
                else
                {
                    var allocation = await RelayService.Instance.JoinAllocationAsync(code);
                    if (!IsCurrent(version)) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    connecting = true;
                    connectionDeadline = Time.unscaledTime + 20f;
                    Status = "Waiting for the host...";
                    if (!manager.StartClient()) throw new InvalidOperationException("Could not start the client.");
                }
            }
            catch (Exception exception)
            {
                if (!IsCurrent(version)) return;
                connecting = false;
                JoinCode = "";
                manager.Shutdown();
                Status = "Connection failed: " + exception.Message;
            }
            finally
            {
                if (this != null && version == operationVersion) busy = false;
            }
        }

        private bool IsCurrent(int version) => this != null && isActiveAndEnabled && version == operationVersion;

        private void Update()
        {
            if (!connecting || Time.unscaledTime < connectionDeadline) return;
            Disconnect();
            Status = "Connection timed out. Check the code and try again.";
        }

        public void Disconnect()
        {
            operationVersion++;
            busy = connecting = false;
            JoinCode = "";
            if (manager != null && manager.IsListening) manager.Shutdown();
            Status = "Disconnected";
        }

        private void OnConnected(ulong clientId)
        {
            if (clientId != manager.LocalClientId || manager.IsServer) return;
            connecting = false;
            Status = "Connected to the host.";
        }

        private void OnDisconnected(ulong clientId)
        {
            if (manager.IsServer || clientId != manager.LocalClientId) return;
            connecting = false;
            JoinCode = "";
            Status = "Disconnected from the host. " + manager.DisconnectReason;
        }

        private void OnDisable() => Disconnect();

        private void OnDestroy()
        {
            operationVersion++;
            if (manager == null) return;
            manager.OnClientConnectedCallback -= OnConnected;
            manager.OnClientDisconnectCallback -= OnDisconnected;
        }
    }
}
#endif
