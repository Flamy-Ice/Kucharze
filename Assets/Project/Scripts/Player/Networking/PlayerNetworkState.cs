#if KUCHARZE_ONLINE
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace ModularMovement
{
    // Presents remote movement on NetworkTransform's render clock before animation is evaluated.
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(PlayerNetworkInput))]
    public sealed class PlayerNetworkState : NetworkBehaviour
    {
        private readonly NetworkVariable<PlayerPresentationFrame> frame = new NetworkVariable<PlayerPresentationFrame>();
        private readonly PlayerPresentationTimeline timeline = new PlayerPresentationTimeline();
        private PlayerStates states;
        private PlayerBus bus;
        private PlayerRotation rotation;
        private PlayerDash dash;
        private NetworkTransform networkTransform;
        private bool rotationEnabled;
        private uint dashId, presentedDashId;

        public override void OnNetworkSpawn()
        {
            states = GetComponent<PlayerStates>();
            bus = GetComponent<PlayerBus>();
            rotation = GetComponent<PlayerRotation>();
            dash = GetComponent<PlayerDash>();
            networkTransform = GetComponent<NetworkTransform>();
            rotationEnabled = rotation != null && rotation.enabled;
            timeline.Clear();
            if (IsServer)
            {
                if (bus != null) bus.DashStarted += OnDashStarted;
                PublishState();
            }
            else if (!IsOwner)
            {
                if (rotation != null) rotation.enabled = false;
                frame.OnValueChanged += OnFrame;
                timeline.Add(frame.Value.Sample);
                ApplyState();
            }
        }

        private void OnFrame(PlayerPresentationFrame previous, PlayerPresentationFrame next) => timeline.Add(next.Sample);
        private void Update()
        {
            if (IsSpawned && !IsServer && !IsOwner) ApplyState();
        }

        private void LateUpdate()
        {
            if (IsSpawned && IsServer) PublishState();
        }

        private void PublishState()
        {
            frame.Value = new PlayerPresentationFrame { Sample = new PlayerPresentationTimeline.Sample
            {
                Time = NetworkManager.ServerTime.Time,
                Velocity = states.ActualVelocity, Moving = states.IsMoving, Grounded = states.IsGrounded,
                Source = states.ActiveMovementSource, DashProgress = dash != null ? dash.DashProgress : 0f,
                DashDuration = dash != null ? dash.CaptureSimulation().Duration : 0f, DashId = dashId,
                Facing = rotation != null && rotation.ResolvedModel != null ? rotation.ResolvedModel.rotation : Quaternion.identity
            }};
        }

        private void ApplyState()
        {
            // Match the interpolation mode used by the authoritative position stream.
            int ticks = networkTransform == null || networkTransform.PositionInterpolationType == NetworkTransform.InterpolationTypes.LegacyLerp
                ? 1 + NetworkTransform.InterpolationBufferTickOffset
                : Mathf.Max(1, NetworkManager.NetworkTimeSystem.TickLatency + NetworkTransform.InterpolationBufferTickOffset);
            double renderTime = NetworkManager.ServerTime.TimeTicksAgo(ticks).Time;
            if (!timeline.Evaluate(renderTime, out var sample)) return;
            states.ApplyReplica(sample.Velocity, sample.Moving, sample.Grounded, sample.Source, sample.DashProgress);
            if (rotation != null && rotation.ResolvedModel != null) rotation.ResolvedModel.rotation = sample.Facing;
            if (sample.Source == "Dash" && sample.DashId != presentedDashId)
            {
                presentedDashId = sample.DashId;
                if (bus != null) bus.ReceiveReplicaDash();
            }
        }

        private void OnDashStarted() => dashId++;

        public override void OnNetworkDespawn()
        {
            frame.OnValueChanged -= OnFrame;
            if (bus != null) bus.DashStarted -= OnDashStarted;
            if (rotation != null) rotation.enabled = rotationEnabled;
            timeline.Clear();
        }
    }
}
#endif
