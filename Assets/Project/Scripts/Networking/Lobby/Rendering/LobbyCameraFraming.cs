using UnityEngine;

namespace ModularMovement
{
    // Fits the fixed lobby board around the HUD when the game window changes shape.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class LobbyCameraFraming : MonoBehaviour
    {
        [SerializeField] private Bounds boardBounds = new Bounds(new Vector3(0, 0.8f, 0), new Vector3(23.2f, 3, 15.6f));
        private Camera localCamera;
        private float previousAspect;
        private float previousFieldOfView;

        private void OnEnable() => FrameNow();
        private void OnValidate() => previousAspect = 0;

        private void LateUpdate()
        {
            if (localCamera == null) localCamera = GetComponent<Camera>();
            if (!Mathf.Approximately(previousAspect, localCamera.aspect) || !Mathf.Approximately(previousFieldOfView, localCamera.fieldOfView))
                FrameNow();
        }

        public void FrameNow()
        {
            if (!gameObject.scene.IsValid()) return;
            if (localCamera == null) localCamera = GetComponent<Camera>();
            if (localCamera == null || localCamera.orthographic) return;
            float tangent = Mathf.Tan(localCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float aspect = Mathf.Max(0.1f, localCamera.aspect);
            Quaternion inverse = Quaternion.Inverse(transform.rotation);
            float distance = 1;
            for (int index = 0; index < 8; index++)
            {
                Vector3 offset = Vector3.Scale(boardBounds.extents,
                    new Vector3((index & 1) == 0 ? -1 : 1, (index & 2) == 0 ? -1 : 1, (index & 4) == 0 ? -1 : 1));
                Vector3 corner = inverse * offset;
                // Leave the top and bottom strips free for the connection cards.
                float verticalLimit = corner.y >= 0 ? 0.78f : 0.80f;
                distance = Mathf.Max(distance, Mathf.Abs(corner.x) / (tangent * aspect * 0.98f) - corner.z,
                    Mathf.Abs(corner.y) / (tangent * verticalLimit) - corner.z);
            }
            distance += 0.5f;
            transform.position = boardBounds.center - transform.forward * distance;
            localCamera.farClipPlane = distance + boardBounds.extents.magnitude + 10;
            previousAspect = localCamera.aspect;
            previousFieldOfView = localCamera.fieldOfView;
        }
    }
}
