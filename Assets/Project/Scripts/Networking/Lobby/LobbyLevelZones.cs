using UnityEngine;

namespace ModularMovement
{
    // Defines the nine walk-on selection areas without changing player movement.
    [DisallowMultipleComponent]
    public sealed class LobbyLevelZones : MonoBehaviour
    {
        [SerializeField] private BoxCollider[] selectionZones = new BoxCollider[9];

        public bool TryGetLevel(Vector3 position, out int index)
        {
            index = -1;
            if (selectionZones == null) return false;
            for (int slot = 0; slot < Mathf.Min(9, selectionZones.Length); slot++)
            {
                BoxCollider zone = selectionZones[slot];
                if (zone == null || !zone.enabled || !zone.gameObject.activeInHierarchy) continue;
                Vector3 local = zone.transform.InverseTransformPoint(position) - zone.center;
                Vector3 half = zone.size * 0.5f;
                if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z) continue;
                index = slot;
                return true;
            }
            return false;
        }

        public bool TryGetCenter(int index, out Vector3 position)
        {
            position = default;
            if (selectionZones == null || index < 0 || index >= selectionZones.Length || selectionZones[index] == null) return false;
            BoxCollider zone = selectionZones[index];
            position = zone.transform.TransformPoint(zone.center);
            return true;
        }
    }
}
