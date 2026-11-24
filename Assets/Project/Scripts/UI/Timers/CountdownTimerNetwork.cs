#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Sends the shared countdown to clients without giving them control over its clock.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(CountdownTimer))]
    public sealed class CountdownTimerNetwork : NetworkBehaviour
    {
        private readonly NetworkVariable<float> remaining = new NetworkVariable<float>();
        private readonly NetworkVariable<bool> running = new NetworkVariable<bool>();
        private CountdownTimer timer;
        private float nextPublish;

        private void Awake()
        {
            timer = GetComponent<CountdownTimer>();
            timer.CanControlTimer = false;
        }

        public override void OnNetworkSpawn()
        {
            timer.CanControlTimer = IsServer;
            if (IsServer)
            {
                timer.RestartTimer();
                Publish();
            }
            else timer.ApplyReplicatedTime(remaining.Value, running.Value);
        }

        private void LateUpdate()
        {
            if (!IsSpawned) return;
            if (!IsServer)
            {
                timer.ApplyReplicatedTime(remaining.Value, running.Value);
                return;
            }
            if (Time.unscaledTime >= nextPublish || timer.IsRunning != running.Value)
                Publish();
        }

        private void Publish()
        {
            nextPublish = Time.unscaledTime + 0.1f;
            remaining.Value = timer.RemainingSeconds;
            running.Value = timer.IsRunning;
        }

        public override void OnNetworkDespawn() => timer.CanControlTimer = false;
    }
}
#endif
