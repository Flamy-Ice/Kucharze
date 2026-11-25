#if KUCHARZE_ONLINE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace ModularMovement
{
    // Exercises the lobby with real test clients only when explicit development arguments are supplied.
    public sealed class LobbySmokeProbe : MonoBehaviour
    {
        private NetworkManager manager;
        private LobbySession session;
        private string report;
        private IEnumerator scenario;
        private float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-kucharze-lobby-probe");
            if (index < 0 || args.Length <= index + 2) return;
            var probe = new GameObject("LobbySmokeProbe").AddComponent<LobbySmokeProbe>();
            DontDestroyOnLoad(probe.gameObject);
            probe.report = args[index + 2];
            probe.scenario = probe.Run(args[index + 1] == "host");
            probe.started = Time.realtimeSinceStartup;
            Application.runInBackground = true;
        }

        private void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 110) throw new Exception("Lobby probe timed out.");
                if (scenario != null && !scenario.MoveNext()) Finish("PASS", 0);
            }
            catch (Exception exception) { Finish("FAIL: " + exception, 1); }
        }

        private IEnumerator Run(bool host)
        {
            while (manager == null)
            {
                manager = FindAnyObjectByType<NetworkManager>();
                yield return null;
            }
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 17839);
            Check(host ? manager.StartHost() : manager.StartClient(), "Network startup failed.");
            if (!host)
            {
                while (!manager.IsConnectedClient) yield return null;
                File.WriteAllText(report, "CONNECTED");
                float fourPlayersAt = -1;
                bool colorsChecked = false;
                while (manager.IsConnectedClient)
                {
                    var appearances = FindObjectsByType<PlayerNetworkAppearance>().Where(player => player.IsSpawned).ToArray();
                    if (appearances.Length == 4 && fourPlayersAt < 0) fourPlayersAt = Time.realtimeSinceStartup;
                    if (appearances.Length == 4 && fourPlayersAt > 0 && Time.realtimeSinceStartup - fourPlayersAt > 2)
                    {
                        Check(appearances.Select(player => player.PaletteIndex).Distinct().Count() == 4, "Player colors did not replicate independently.");
                        Check(CountCoatColors(appearances) == 4, "Player materials shared a tint on the client.");
                        colorsChecked = true;
                    }
                    yield return null;
                }
                Check(colorsChecked, "The client did not verify four player colors.");
                yield break;
            }
            while (session == null)
            {
                session = FindAnyObjectByType<LobbySession>();
                yield return null;
            }
            yield return null;
            MoveEveryoneInside();
            float until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) yield return null;
            Check(session.PlayerCount == 1 && session.SecondsRemaining == 0, "A solo host started a countdown.");
            File.WriteAllText(report, "WAIT2");
            while (session.PlayerCount != 2 || manager.ConnectedClientsList.Any(c => c.PlayerObject == null)) yield return null;
            yield return null;
            Vector3[] positions = manager.ConnectedClientsList.Select(c => c.PlayerObject.transform.position).ToArray();
            Check(Vector3.Distance(positions[0], positions[1]) > 3, "Players shared a spawn.");
            MoveEveryoneInside();
            until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) yield return null;
            Check(session.SecondsRemaining > 0, "Two players did not start the countdown.");
            NetworkObject second = manager.ConnectedClientsList.First(c => c.ClientId != 0).PlayerObject;
            Place(second, new Vector3(0, 1.1f, 0));
            yield return null;
            yield return null;
            Check(session.SecondsRemaining == 0, "Leaving did not reset the countdown.");
            MoveEveryoneInside();
            until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) yield return null;
            File.WriteAllText(report, "WAIT3");
            while (session.PlayerCount != 3) yield return null;
            yield return null;
            Check(session.SecondsRemaining == 0, "Joining did not reset the countdown.");
            while (manager.ConnectedClientsList.Any(c => c.PlayerObject == null)) yield return null;
            MoveEveryoneInside();
            until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) yield return null;
            Check(session.SecondsRemaining > 0, "Three players did not start the countdown.");
            File.WriteAllText(report, "WAIT4");
            while (session.PlayerCount != 4 || manager.ConnectedClientsList.Any(c => c.PlayerObject == null)) yield return null;
            until = Time.realtimeSinceStartup + 5.5f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Check(session.IsLobby && session.SecondsRemaining == 0 && session.ReadyCount == 3,
                "Three of four players incorrectly started the game.");
            Check(manager.ConnectedClientsList.Select(client => client.PlayerObject.GetComponent<PlayerNetworkAppearance>().PaletteIndex)
                .Distinct().Count() == 4, "Room colors were not assigned independently.");
            Check(CountCoatColors(manager.ConnectedClientsList.Select(client => client.PlayerObject.GetComponent<PlayerNetworkAppearance>())) == 4,
                "Player materials shared a tint on the server.");
            MoveEveryoneInside();
            float allReadyAt = Time.realtimeSinceStartup;
            while (!session.IsLoading) yield return null;
            Check(Time.realtimeSinceStartup - allReadyAt >= 4.8f, "The start delay was shorter than five seconds.");
            while (session.IsLoading) yield return null;
            Check(!session.IsLobby && manager.ConnectedClients.Count == 4, "The shared game scene did not load.");
            Check(!session.AcceptingPlayers, "Joining remained enabled during gameplay.");
            foreach (ulong clientId in manager.ConnectedClientsIds.ToArray())
                if (clientId != 0) manager.DisconnectClient(clientId);
            while (!session.IsLobby || session.IsLoading) yield return null;
            yield return null;
            Check(session.PlayerCount == 1 && session.SecondsRemaining == 0, "A lone player did not return to the lobby.");
            LobbyLayout layout = FindAnyObjectByType<LobbyLayout>();
            Check(layout != null && layout.TryGetSpawn(0, out Vector3 spawn) &&
                Vector2.Distance(new Vector2(spawn.x, spawn.z), new Vector2(manager.LocalClient.PlayerObject.transform.position.x,
                    manager.LocalClient.PlayerObject.transform.position.z)) < 0.5f,
                "Returning did not place the host in the assigned room.");
        }

        private void MoveEveryoneInside()
        {
            int index = 0;
            foreach (NetworkClient client in manager.ConnectedClientsList)
                Place(client.PlayerObject, new Vector3(-2.25f + index++ * 1.5f, 1.1f, 5.5f));
        }

        private static int CountCoatColors(System.Collections.Generic.IEnumerable<PlayerNetworkAppearance> players)
        {
            var properties = new MaterialPropertyBlock();
            return players.Select(player =>
            {
                player.GetComponentInChildren<SkinnedMeshRenderer>().GetPropertyBlock(properties, 0);
                return properties.GetColor("_BaseColor");
            }).Distinct().Count();
        }

        private static void Place(NetworkObject player, Vector3 position)
        {
            // Test placement avoids keyboard focus and does not replace normal locomotion.
            player.GetComponent<PlayerMovementResolver>().SimulationEnabled = false;
            CharacterController controller = player.GetComponent<CharacterController>();
            bool enabled = controller.enabled;
            controller.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(position, player.transform.rotation, player.transform.localScale);
            controller.enabled = enabled;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private void Finish(string result, int exitCode)
        {
            scenario = null;
            File.WriteAllText(report, result);
            if (manager != null) manager.Shutdown();
            Application.Quit(exitCode);
            enabled = false;
        }
    }
}
#endif
