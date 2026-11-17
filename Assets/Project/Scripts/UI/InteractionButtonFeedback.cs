using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularMovement
{
    // Makes a UI image darker while a mouse button or finger holds it down.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("UI/Interaction Button Feedback")]
    public sealed class InteractionButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Image brightness while pressed. Zero is black and one keeps the original color.")]
        private float pressedBrightness = 0.65f;

        private Image buttonImage;
        private Color originalColor;
        private int activePointerId;
        private bool isPressed;

        private void Awake()
        {
            buttonImage = GetComponent<Image>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || isPressed || buttonImage == null ||
                eventData.button != PointerEventData.InputButton.Left)
                return;

            isPressed = true;
            activePointerId = eventData.pointerId;
            originalColor = buttonImage.color;

            // Keep the image's transparency and restore its exact color after release.
            float brightness = Mathf.Clamp01(pressedBrightness);
            buttonImage.color = new Color(
                originalColor.r * brightness,
                originalColor.g * brightness,
                originalColor.b * brightness,
                originalColor.a);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // A second finger must not cancel the first finger's press.
            if (isPressed && eventData.pointerId == activePointerId)
                RestoreColor();
        }

        private void OnDisable()
        {
            RestoreColor();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                RestoreColor();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                RestoreColor();
        }

        private void RestoreColor()
        {
            if (!isPressed)
                return;

            if (buttonImage != null)
                buttonImage.color = originalColor;

            isPressed = false;
        }
    }
}
