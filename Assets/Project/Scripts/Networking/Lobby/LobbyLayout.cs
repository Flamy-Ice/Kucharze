using UnityEngine;

namespace ModularMovement
{
    // Keeps movable scene markers together without deciding when a game starts.
    [DisallowMultipleComponent]
    public sealed class LobbyLayout : MonoBehaviour
    {
        [SerializeField] private Transform[] roomSpawns = new Transform[4];
        [SerializeField] private BoxCollider startZone;

        public bool TryGetSpawn(int slot, out Vector3 position)
        {
            position = default;
            if (slot < 0 || slot >= roomSpawns.Length || roomSpawns[slot] == null) return false;
            position = roomSpawns[slot].position;
            return true;
        }

        public bool Contains(Vector3 position)
        {
            if (startZone == null || !startZone.gameObject.activeInHierarchy) return false;
            Vector3 local = startZone.transform.InverseTransformPoint(position) - startZone.center;
            Vector3 half = startZone.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        public bool TryGetStartCenter(out Vector3 position)
        {
            position = default;
            if (startZone == null) return false;
            position = startZone.transform.TransformPoint(startZone.center);
            return true;
        }
    }
}
