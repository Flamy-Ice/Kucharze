using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace ModularMovement
{
    // Attach to a transparent Image covering the left half of the screen.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    [AddComponentMenu("UI/Floating Joystick")]
    public sealed class FloatingJoystick : OnScreenControl, IPointerDownHandler,
        IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler
    {
        [SerializeField, Tooltip("Joystick Body, a direct child of this touch area.")]
        private RectTransform joystickBody;
        [SerializeField, Tooltip("Joystick Moving Part, a centered direct child of Joystick Body.")]
        private RectTransform joystickMovingPart;
        [SerializeField, Min(1f), Tooltip("Maximum handle travel in canvas units.")]
        private float movementRadius = 70f;
        [SerializeField, InputControl(layout = "Vector2")]
        private string inputControlPath = "<Gamepad>/leftStick";

        private RectTransform touchArea;
        private Vector2 origin;
        private int activePointerId;
        private bool hasPointer;

        protected override string controlPathInternal
        {
            get => inputControlPath;
            set => inputControlPath = value;
        }

        protected override void OnEnable()
        {
            touchArea = (RectTransform)transform;
            if (joystickBody == null || joystickMovingPart == null ||
                joystickBody.parent != transform || joystickMovingPart.parent != joystickBody)
            {
                Debug.LogWarning("FloatingJoystick: Assign Body directly under the touch area and Moving Part directly under Body.", this);
                enabled = false;
                return;
            }

            base.OnEnable();
            ResetJoystick();
        }

        protected override void OnDisable()
        {
            ResetJoystick();
            base.OnDisable();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || hasPointer || eventData.button != PointerEventData.InputButton.Left)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    touchArea, eventData.position, eventData.pressEventCamera, out origin))
                return;

            hasPointer = true;
            activePointerId = eventData.pointerId;
            joystickBody.localPosition = new Vector3(origin.x, origin.y, 0f);
            joystickMovingPart.anchoredPosition = Vector2.zero;
            joystickBody.gameObject.SetActive(true);
            if (control != null)
                SendValueToControl(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!hasPointer || eventData.pointerId != activePointerId)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    touchArea, eventData.position, eventData.pressEventCamera, out Vector2 position))
                return;

            float radius = Mathf.Max(1f, movementRadius);
            Vector2 offset = Vector2.ClampMagnitude(position - origin, radius);
            joystickMovingPart.anchoredPosition = offset;
            if (control != null)
                SendValueToControl(offset / radius);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (hasPointer && eventData.pointerId == activePointerId)
                ResetJoystick();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                ResetJoystick();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ResetJoystick();
        }

        private void ResetJoystick()
        {
            hasPointer = false;
            if (control != null)
                SendValueToControl(Vector2.zero);
            if (joystickMovingPart != null)
                joystickMovingPart.anchoredPosition = Vector2.zero;
            if (joystickBody != null && joystickBody.parent == transform)
                joystickBody.gameObject.SetActive(false);
        }
    }
}
