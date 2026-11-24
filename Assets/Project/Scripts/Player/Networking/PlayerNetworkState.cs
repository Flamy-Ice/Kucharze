#if KUCHARZE_ONLINE
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Copies movement readings and model facing from the server to each player's visual replica.
    [DefaultExecutionOrder(1000), DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(PlayerNetworkInput))]
    public sealed class PlayerNetworkState : NetworkBehaviour
    {
        private readonly NetworkVariable<Vector3> velocity = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<bool> moving = new NetworkVariable<bool>();
        private readonly NetworkVariable<bool> grounded = new NetworkVariable<bool>();
        private readonly NetworkVariable<FixedString64Bytes> source = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<float> dashProgress = new NetworkVariable<float>();
        private readonly NetworkVariable<Quaternion> facing = new NetworkVariable<Quaternion>(Quaternion.identity);
        private PlayerStates states;
        private PlayerBus bus;
        private PlayerRotation rotation;
        private PlayerDash dash;
        private CharacterController controller;
        private bool controllerEnabled;
        private bool rotationEnabled;

        public override void OnNetworkSpawn()
        {
            states = GetComponent<PlayerStates>();
            bus = GetComponent<PlayerBus>();
            rotation = GetComponent<PlayerRotation>();
            dash = GetComponent<PlayerDash>();
            controller = GetComponent<CharacterController>();
            controllerEnabled = controller != null && controller.enabled;
            rotationEnabled = rotation != null && rotation.enabled;
            if (IsServer)
            {
                if (bus != null) bus.DashStarted += OnDashStarted;
                PublishState();
            }
            else
            {
                if (controller != null) controller.enabled = false;
                if (rotation != null) rotation.enabled = false;
                ApplyState();
            }
        }

        private void Update()
        {
            if (IsSpawned && !IsServer) ApplyState();
        }

        private void LateUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer) PublishState();
            else ApplyFacing();
        }

        private void PublishState()
        {
            velocity.Value = states.ActualVelocity;
            moving.Value = states.IsMoving;
            grounded.Value = states.IsGrounded;
            source.Value = new FixedString64Bytes(states.ActiveMovementSource);
            dashProgress.Value = dash != null ? dash.DashProgress : 0f;
            if (rotation != null && rotation.ResolvedModel != null)
                facing.Value = rotation.ResolvedModel.rotation;
        }

        private void ApplyState()
        {
            states.ApplyReplica(velocity.Value, moving.Value, grounded.Value, source.Value.ToString(), dashProgress.Value);
        }

        private void ApplyFacing()
        {
            if (rotation != null && rotation.ResolvedModel != null)
                rotation.ResolvedModel.rotation = facing.Value;
        }

        private void OnDashStarted() => PlayDashRpc();

        [Rpc(SendTo.NotServer)]
        private void PlayDashRpc()
        {
            ApplyState();
            if (bus != null) bus.ReceiveReplicaDash();
        }

        public override void OnNetworkDespawn()
        {
            if (bus != null) bus.DashStarted -= OnDashStarted;
            if (controller != null) controller.enabled = controllerEnabled;
            if (rotation != null) rotation.enabled = rotationEnabled;
        }
    }
}
#endif
