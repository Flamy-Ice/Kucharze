#if KUCHARZE_ONLINE
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace ModularMovement
{
    // Transports owner controls and server checkpoints while the prediction adapter runs the motor.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(PlayerMovementResolver))]
    public sealed class PlayerNetworkInput : NetworkBehaviour, IPlayerInputSource
    {
        private PlayerInputActions input;
        private PlayerMovementResolver resolver;
        private PlayerNetworkPrediction prediction;
        private NetworkTransform networkTransform;
        private bool originalInputEnabled, originalSimulationEnabled;
        private bool syncX, syncY, syncZ;
        public PlayerNetworkPrediction Prediction => prediction;

        private void Awake()
        {
            input = GetComponent<PlayerInputActions>();
            resolver = GetComponent<PlayerMovementResolver>();
            networkTransform = GetComponent<NetworkTransform>();
            originalInputEnabled = input.InputEnabled;
            originalSimulationEnabled = resolver.SimulationEnabled;
            resolver.SimulationEnabled = false;
            input.SetInputEnabled(false);
            prediction = GetComponent<PlayerNetworkPrediction>();
            if (prediction == null) prediction = gameObject.AddComponent<PlayerNetworkPrediction>();
        }

        public override void OnNetworkSpawn()
        {
            resolver.SimulationEnabled = (IsServer || IsOwner) && originalSimulationEnabled;
            input.SetInputEnabled(IsOwner && originalInputEnabled);
            if (networkTransform != null)
            {
                syncX = networkTransform.SyncPositionX;
                syncY = networkTransform.SyncPositionY;
                syncZ = networkTransform.SyncPositionZ;
                networkTransform.UseUnreliableDeltas = true;
                networkTransform.PositionMaxInterpolationTime = 0.05f;
                // Timeline interpolation already smooths motion; another lerp adds a sliding tail.
                networkTransform.PositionLerpSmoothing = false;
                if (IsOwner && !IsServer)
                {
                    // The owner's checkpoint/replay path replaces delayed position interpolation.
                    networkTransform.SyncPositionX = networkTransform.SyncPositionY = networkTransform.SyncPositionZ = false;
                }
            }
            prediction.Initialize(this);
        }

        private void Update()
        {
            if (IsSpawned) prediction.Tick(Time.deltaTime);
        }

        internal void SendControls(PlayerInputFrame newest, PlayerInputFrame previous, PlayerInputFrame oldest)
            => SubmitInputRpc(newest, previous, oldest);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void SubmitInputRpc(PlayerInputFrame newest, PlayerInputFrame previous, PlayerInputFrame oldest, RpcParams rpcParams = default)
        {
            if (!IsServer || rpcParams.Receive.SenderClientId != OwnerClientId) return;
            prediction.Receive(oldest);
            prediction.Receive(previous);
            prediction.Receive(newest);
        }

        internal void SendCheckpoint(PlayerServerFrame state) => CheckpointRpc(state);

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
        private void CheckpointRpc(PlayerServerFrame state, RpcParams rpcParams = default)
        {
            if (!IsServer && IsOwner && rpcParams.Receive.SenderClientId == Unity.Netcode.NetworkManager.ServerClientId) prediction.Receive(state);
        }

        public PlayerInputSnapshot ReadInput() => input != null ? input.CaptureSnapshot() : default;

        public override void OnNetworkDespawn()
        {
            prediction.Stop();
            input.SetExternalSource(null);
            input.SetInputEnabled(false);
            resolver.SimulationEnabled = false;
            if (networkTransform != null)
            {
                networkTransform.SyncPositionX = syncX;
                networkTransform.SyncPositionY = syncY;
                networkTransform.SyncPositionZ = syncZ;
            }
        }
    }
}
#endif
