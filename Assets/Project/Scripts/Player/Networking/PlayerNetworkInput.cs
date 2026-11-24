#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Sends the owner's controls to the host and stops old controls after a lost connection.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(PlayerMovementResolver))]
    public sealed class PlayerNetworkInput : NetworkBehaviour, IPlayerInputSource
    {
        private PlayerInputActions input;
        private PlayerMovementResolver resolver;
        private PlayerInputSnapshot latest;
        private float receivedAt;
        private float nextSend;
        private uint resetOffset;
        private bool timedOut;
        private bool originalInputEnabled;
        private bool originalSimulationEnabled;
        private uint runBaseline;
        private uint dashBaseline;

        private void Awake()
        {
            input = GetComponent<PlayerInputActions>();
            resolver = GetComponent<PlayerMovementResolver>();
            originalInputEnabled = input.InputEnabled;
            originalSimulationEnabled = resolver.SimulationEnabled;
            // Online prefabs wait for ownership before any movement can run.
            resolver.SimulationEnabled = false;
            input.SetInputEnabled(false);
        }

        public override void OnNetworkSpawn()
        {
            resolver.SimulationEnabled = IsServer && originalSimulationEnabled;
            input.SetInputEnabled(IsOwner && originalInputEnabled);
            if (IsServer && !IsOwner) input.SetExternalSource(this);
            receivedAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || IsServer || Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + 1f / 30f;
            PlayerInputSnapshot snapshot = input.CaptureSnapshot();
            SubmitInputRpc(snapshot.Move, snapshot.RunHeld, snapshot.RunPressCount, snapshot.DashPressCount,
                snapshot.ResetVersion, snapshot.DashResetVersion, snapshot.DashAvailable);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitInputRpc(Vector2 move, bool runHeld, uint runPresses, uint dashPresses,
            uint reset, uint dashReset, bool dashAvailable, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId
                || !IsFinite(move.x) || !IsFinite(move.y)) return;
            if (reset != latest.ResetVersion) runBaseline = 0;
            if (dashReset != latest.DashResetVersion) dashBaseline = 0;
            latest = new PlayerInputSnapshot
            {
                Move = Vector2.ClampMagnitude(move, 1f), RunHeld = runHeld,
                RunPressCount = runPresses, DashPressCount = dashPresses,
                ResetVersion = reset, DashResetVersion = dashReset, DashAvailable = dashAvailable
            };
            receivedAt = Time.unscaledTime;
            timedOut = false;
        }

        public PlayerInputSnapshot ReadInput()
        {
            if (Time.unscaledTime - receivedAt > 0.5f)
            {
                if (!timedOut)
                {
                    unchecked { resetOffset++; }
                    // A delayed packet must not replay a press already seen before the timeout.
                    runBaseline = latest.RunPressCount;
                    dashBaseline = latest.DashPressCount;
                    timedOut = true;
                }
                return new PlayerInputSnapshot
                {
                    ResetVersion = unchecked(latest.ResetVersion + resetOffset),
                    DashResetVersion = unchecked(latest.DashResetVersion + resetOffset)
                };
            }
            PlayerInputSnapshot snapshot = latest;
            snapshot.RunPressCount = unchecked(snapshot.RunPressCount - runBaseline);
            snapshot.DashPressCount = unchecked(snapshot.DashPressCount - dashBaseline);
            snapshot.ResetVersion = unchecked(snapshot.ResetVersion + resetOffset);
            snapshot.DashResetVersion = unchecked(snapshot.DashResetVersion + resetOffset);
            return snapshot;
        }

        public override void OnNetworkDespawn()
        {
            input.SetExternalSource(null);
            input.SetInputEnabled(false);
            resolver.SimulationEnabled = false;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
#endif
