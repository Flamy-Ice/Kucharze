#if KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace ModularMovement
{
    // Checks two test builds over a local connection or Relay when test arguments are supplied.
    public sealed class OnlineSmokeProbe : MonoBehaviour, IPlayerInputSource
    {
        private string role;
        private float started;
        private float bothAt = -1f;
        private NetworkManager manager;
        private PlayerInputActions ownerInput;
        private bool movementChecked;
        private bool dashChecked;
        private int acceptedDashes;
        private int initialCoins;
        private bool useRelay;
        private RelayConnection connection;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            string[] args = Environment.GetCommandLineArgs();
            string mode = args.Contains("-kucharze-smoke-host") || args.Contains("-kucharze-smoke-relay-host") ? "host"
                : args.Contains("-kucharze-smoke-client") || args.Contains("-kucharze-smoke-relay-client") ? "client" : null;
            if (mode == null) return;
            var probe = new GameObject("OnlineSmokeProbe").AddComponent<OnlineSmokeProbe>();
            probe.role = mode;
            probe.useRelay = args.Contains("-kucharze-smoke-relay-host") || args.Contains("-kucharze-smoke-relay-client");
            probe.initialCoins = FindAnyObjectByType<CoinWallet>()?.Coins ?? 0;
            probe.started = Time.realtimeSinceStartup;
            Application.runInBackground = true;
        }

        private void Update()
        {
            try { Check(); }
            catch (Exception exception) { Finish("FAILED: " + exception.Message, 1); }
        }

        private void Check()
        {
            if (Time.realtimeSinceStartup - started > 35f) throw new Exception("Network smoke test timed out.");
            if (manager == null)
            {
                manager = FindAnyObjectByType<NetworkManager>();
                if (manager == null) return;
                if (useRelay)
                {
                    connection = manager.GetComponent<RelayConnection>();
                    _ = BeginRelayAsync();
                }
                else
                {
                    manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 17837);
                    bool result = role == "host" ? manager.StartHost() : manager.StartClient();
                    if (!result) throw new Exception("Could not start " + role);
                }
            }
            if (useRelay && connection.Status.StartsWith("Connection failed:")) throw new Exception(connection.Status);
            if (useRelay && role == "host" && connection.JoinCode.Length > 0 && !File.Exists(RelayCodePath))
                File.WriteAllText(RelayCodePath, connection.JoinCode);
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null && ownerInput == null)
            {
                ownerInput = manager.LocalClient.PlayerObject.GetComponent<PlayerInputActions>();
                ownerInput.SetExternalSource(this);
                manager.LocalClient.PlayerObject.GetComponent<PlayerBus>().DashStarted += () => acceptedDashes++;
            }
            var players = FindObjectsByType<PlayerNetworkInput>().Where(player => player.IsSpawned).ToArray();
            if (players.Length < 2)
            {
                if (role == "host" && movementChecked && dashChecked && bothAt >= 0f
                    && Time.realtimeSinceStartup - bothAt > 4.5f)
                    Finish("SUCCESS: server movement, dash, shared state and client disconnect", 0);
                return;
            }
            if (bothAt < 0f)
            {
                bothAt = Time.realtimeSinceStartup;
                if (role == "host")
                {
                    CoinWallet wallet = FindAnyObjectByType<CoinWallet>();
                    if (wallet == null || !wallet.TryAddCoins(7)) throw new Exception("Server could not award coins.");
                }
            }
            float elapsed = Time.realtimeSinceStartup - bothAt;
            if (!movementChecked && elapsed > 1f)
            {
                foreach (var player in players)
                {
                    var resolver = player.GetComponent<PlayerMovementResolver>();
                    var state = player.GetComponent<PlayerStates>();
                    if (resolver.SimulationEnabled != manager.IsServer) throw new Exception("Wrong movement authority.");
                    Vector3 velocity = state.ActualVelocity;
                    if (player.OwnerClientId == 0 && velocity.x <= 0.1f) throw new Exception("Host did not move along X.");
                    if (player.OwnerClientId != 0 && velocity.z <= 0.1f) throw new Exception("Client did not move along Z.");
                }
                CoinWallet wallet = FindAnyObjectByType<CoinWallet>();
                CountdownTimer timer = FindAnyObjectByType<CountdownTimer>();
                if (wallet == null || wallet.Coins != initialCoins + 7) throw new Exception("Coin total did not replicate.");
                if (timer == null || timer.RemainingSeconds <= 0f || !timer.IsRunning) throw new Exception("Timer did not replicate.");
                if (role == "client")
                {
                    if (wallet.TryAddCoins(99)) throw new Exception("Client awarded coins.");
                    timer.PauseTimer();
                    if (!timer.IsRunning) throw new Exception("Client paused the shared timer.");
                }
                movementChecked = true;
            }
            if (elapsed > 2.8f && !dashChecked)
            {
                if (acceptedDashes != 1) throw new Exception("Expected one accepted owner dash, got " + acceptedDashes);
                dashChecked = true;
            }
            if (role == "client" && elapsed > 3.2f && movementChecked && dashChecked)
                Finish("SUCCESS: two players, independent X/Z controls, server authority, dash, wallet and timer", 0);
        }

        public PlayerInputSnapshot ReadInput()
        {
            bool dash = bothAt >= 0f && Time.realtimeSinceStartup - bothAt > 1.6f;
            return new PlayerInputSnapshot
            {
                Move = role == "host" ? Vector2.right : Vector2.up,
                DashAvailable = true, DashPressCount = dash ? 1u : 0u
            };
        }

        private static string RelayCodePath => Path.Combine(Path.GetTempPath(), "KucharzeOnlineSmoke", "relay-code.txt");

        private async Task BeginRelayAsync()
        {
            try
            {
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile("networktest" + role));
                if (role == "host") connection.Host();
                else
                {
                    while (this != null && !File.Exists(RelayCodePath)) await Task.Delay(100);
                    if (this != null) connection.Join(File.ReadAllText(RelayCodePath));
                }
            }
            catch (Exception exception)
            {
                if (this != null) Finish("FAILED: " + exception.Message, 1);
            }
        }

        private void Finish(string result, int exitCode)
        {
            string directory = Path.Combine(Path.GetTempPath(), "KucharzeOnlineSmoke");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, role + ".result"), result);
            enabled = false;
            if (manager != null) manager.Shutdown();
            Application.Quit(exitCode);
        }
    }
}
#endif
