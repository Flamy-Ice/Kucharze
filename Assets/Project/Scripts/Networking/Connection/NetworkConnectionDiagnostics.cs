#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ModularMovement
{
    // Measures frame rate and transport delay without changing network or movement settings.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkConnectionDiagnostics : MonoBehaviour
    {
        [SerializeField] private bool showOverlay;
        private NetworkManager manager;
        private float elapsed, fps, rtt, variation, previousRtt;
        private int frames;

        private void Awake() => manager = GetComponent<NetworkManager>();

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) showOverlay = !showOverlay;
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < 0.5f) return;
            fps = frames / elapsed;
            frames = 0;
            elapsed = 0;
            if (manager == null || !manager.IsListening) return;
            ulong peer = NetworkManager.ServerClientId;
            if (manager.IsServer)
                foreach (ulong client in manager.ConnectedClientsIds)
                    if (client != NetworkManager.ServerClientId) { peer = client; break; }
            rtt = manager.NetworkConfig.NetworkTransport.GetCurrentRtt(peer);
            variation = Mathf.Abs(rtt - previousRtt);
            previousRtt = rtt;
        }

        private void OnGUI()
        {
            if (!showOverlay || manager == null || !manager.IsListening) return;
            PlayerNetworkPrediction prediction = manager.LocalClient?.PlayerObject != null
                ? manager.LocalClient.PlayerObject.GetComponent<PlayerNetworkPrediction>() : null;
            string role = manager.IsHost ? "HOST" : "CLIENT";
            string text = role + " | FPS " + fps.ToString("F0") + " | frame " + (1000 / Mathf.Max(1, fps)).ToString("F1") + " ms\n"
                + "RTT " + rtt.ToString("F0") + " ms | RTT change " + variation.ToString("F0") + " ms\n";
            if (prediction != null && !manager.IsServer)
                text += "Snapshot age " + (prediction.SnapshotAge * 1000).ToString("F0") + " ms\nCorrection "
                    + prediction.LastCorrection.ToString("F3") + " m | total " + prediction.CorrectionCount
                    + " | pending " + prediction.PendingInputCount + "\n";
            text += "F8: hide diagnostics";
            Rect safe = Screen.safeArea;
            GUI.Box(new Rect(safe.xMin + 12, Screen.height - safe.yMax + 12, 380, 120), text);
        }
    }
}
#endif
