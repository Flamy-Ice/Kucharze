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
        [SerializeField, Min(1f), Tooltip("Maximum handle travel in base-local units, limited by the base and handle sizes.")]
        private float movementRadius = 120f;
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
            joystickBody.pivot = new Vector2(0.5f, 0.5f);
            joystickMovingPart.anchorMin = joystickMovingPart.anchorMax = new Vector2(0.5f, 0.5f);
            joystickMovingPart.pivot = new Vector2(0.5f, 0.5f);
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
            // Reserve space for the half-handle that can extend past the base.
            Rect area = touchArea.rect;
            Vector2 halfBody = Vector2.Scale(joystickBody.rect.size * 0.5f,
                new Vector2(Mathf.Abs(joystickBody.localScale.x), Mathf.Abs(joystickBody.localScale.y)));
            halfBody += Vector2.Scale(joystickMovingPart.rect.size * 0.5f,
                new Vector2(Mathf.Abs(joystickBody.localScale.x * joystickMovingPart.localScale.x),
                    Mathf.Abs(joystickBody.localScale.y * joystickMovingPart.localScale.y)));
            origin.x = area.width >= halfBody.x * 2f
                ? Mathf.Clamp(origin.x, area.xMin + halfBody.x, area.xMax - halfBody.x) : area.center.x;
            origin.y = area.height >= halfBody.y * 2f
                ? Mathf.Clamp(origin.y, area.yMin + halfBody.y, area.yMax - halfBody.y) : area.center.y;
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
                    joystickBody, eventData.position, eventData.pressEventCamera, out Vector2 position))
                return;

            // Travel is measured in the base's local units, so canvas scaling cannot stretch it.
            float baseRadius = Mathf.Min(joystickBody.rect.width, joystickBody.rect.height) * 0.5f;
            // The handle's center may reach the rim, leaving half of it outside the circle.
            float radius = Mathf.Max(0f, Mathf.Min(movementRadius, baseRadius));
            Vector2 offset = Vector2.ClampMagnitude(position, radius);
            joystickMovingPart.anchoredPosition = offset;
            if (control != null)
                SendValueToControl(radius > 0f ? offset / radius : Vector2.zero);
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
