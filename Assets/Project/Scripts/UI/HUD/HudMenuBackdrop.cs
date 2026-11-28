using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace ModularMovement
{
    // Owns the live menu texture without changing the camera or relying on a global scene texture.
    [DisallowMultipleComponent]
    public sealed class HudMenuBackdrop : MonoBehaviour
    {
        private Camera sourceCamera;
        private RawImage blurImage;
        private Texture previousTexture;
        private Material previousMaterial;
        private Material captureMaterial;
        private RTHandle capture;

        private void OnEnable()
        {
            sourceCamera = Camera.main;
            blurImage = GetComponentInChildren<RawImage>(true);
            if (sourceCamera == null || blurImage == null) return;
            previousTexture = blurImage.texture;
            previousMaterial = blurImage.material;
            captureMaterial = new Material(previousMaterial);
            blurImage.material = captureMaterial;
            RefreshCapture();
        }

        private void LateUpdate() => RefreshCapture();

        public void RefreshCapture()
        {
            if (sourceCamera == null || blurImage == null) return;
            int width = Mathf.Max(1, (sourceCamera.pixelWidth + 1) / 2);
            int height = Mathf.Max(1, (sourceCamera.pixelHeight + 1) / 2);
            if (capture != null && capture.rt.width == width && capture.rt.height == height) return;
            ReleaseCapture();
            capture = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                filterMode: FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "Pause Menu Scene");
            HudMenuCaptureFeature.Register(sourceCamera, capture);
            blurImage.texture = capture.rt;
            captureMaterial.mainTexture = capture.rt;
        }

        private void ReleaseCapture()
        {
            if (capture == null) return;
            HudMenuCaptureFeature.Unregister(sourceCamera, capture);
            capture.Release();
            capture = null;
        }

        private void OnDisable()
        {
            if (blurImage != null)
            {
                blurImage.texture = previousTexture;
                blurImage.material = previousMaterial;
            }
            ReleaseCapture();
            if (captureMaterial != null) Destroy(captureMaterial);
            captureMaterial = null;
            previousMaterial = null;
            blurImage = null;
            previousTexture = null;
            sourceCamera = null;
        }
    }
}
