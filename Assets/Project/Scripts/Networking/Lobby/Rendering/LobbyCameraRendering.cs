using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Applies the lobby's lighting settings only while its own camera draws.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class LobbyCameraRendering : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Shadow coverage for this lobby camera, in meters.")]
        private float shadowDistance = 110f;
        private Camera localCamera;
        private bool previousBatching;
        private bool rendering;
        private UniversalRenderPipelineAsset pipeline;
        private float previousShadowDistance;
        private Camera renderingCamera;

        private void OnEnable()
        {
            localCamera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (rendering || !Handles(camera)) return;
            renderingCamera = camera;
            previousBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
            GraphicsSettings.useScriptableRenderPipelineBatching = false;
            pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                previousShadowDistance = pipeline.shadowDistance;
                pipeline.shadowDistance = Mathf.Max(previousShadowDistance, shadowDistance, localCamera.farClipPlane);
            }
            rendering = true;
        }

        private void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == renderingCamera) Restore();
        }

        private bool Handles(Camera camera)
        {
            if (camera == localCamera) return true;
#if UNITY_EDITOR
            return camera.cameraType == CameraType.SceneView && SceneManager.GetActiveScene() == gameObject.scene;
#else
            return false;
#endif
        }

        private void Restore()
        {
            if (!rendering) return;
            GraphicsSettings.useScriptableRenderPipelineBatching = previousBatching;
            if (pipeline != null) pipeline.shadowDistance = previousShadowDistance;
            pipeline = null;
            renderingCamera = null;
            rendering = false;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            Restore();
        }
    }
}
