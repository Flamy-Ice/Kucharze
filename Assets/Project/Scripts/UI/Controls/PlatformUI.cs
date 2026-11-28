using UnityEngine;

namespace ModularMovement
{
    // Put this component on HUD and assign the entire Mobile section.
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Platform UI")]
    public sealed class PlatformUI : MonoBehaviour
    {
        [SerializeField, Tooltip("The Mobile parent object. All its children follow its visibility.")]
        private GameObject mobileControls;

#if UNITY_EDITOR
        private enum PreviewMode
        {
            Automatic,
            Mobile,
            Desktop
        }

        [SerializeField, Tooltip("Editor only. Choose Mobile or Desktop before entering Play mode to test the UI.")]
        private PreviewMode editorPreview = PreviewMode.Automatic;
#endif

        private void OnEnable() => Refresh();
        private void Update() => Refresh();

        public void Refresh()
        {
            if (mobileControls == null)
            {
                Debug.LogWarning("PlatformUI: Assign the Mobile object to Mobile Controls.", this);
                return;
            }

            // The controller must stay outside the section it can disable.
            if (transform.IsChildOf(mobileControls.transform))
            {
                Debug.LogWarning("PlatformUI: Place this component on HUD, outside the Mobile section.", this);
                return;
            }

            // Device.Application also supports Unity's Device Simulator in the editor.
            bool isMobile = UnityEngine.Device.Application.isMobilePlatform;

#if UNITY_EDITOR
            if (editorPreview != PreviewMode.Automatic)
                isMobile = editorPreview == PreviewMode.Mobile;
#endif

            if (mobileControls.activeSelf != isMobile) mobileControls.SetActive(isMobile);
        }
    }
}
