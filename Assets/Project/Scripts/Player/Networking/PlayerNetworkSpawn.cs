#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Places new players in separate starting slots before normal server movement begins.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerNetworkSpawn : NetworkBehaviour
    {
        [SerializeField, Min(1f), Tooltip("Distance between starting slots in meters.")]
        private float spacing = 2f;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            CharacterController controller = GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            // The prefab position is the origin; each connection receives its own grid slot.
            ulong slot = OwnerClientId;
            transform.position += new Vector3((float)(slot % 4) * spacing, 0f, (float)(slot / 4) * spacing);
            if (controller != null) controller.enabled = wasEnabled;
        }
    }
}
#endif
