using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularMovement
{
    // Gives menu controls a small hover, focus and press response without changing what they do.
    [DisallowMultipleComponent, RequireComponent(typeof(Selectable))]
    public sealed class MenuSelectableScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1.045f;
        [SerializeField, Range(0.9f, 1f)] private float pressedScale = 0.975f;
        [SerializeField, Min(0.01f)] private float responseSeconds = 0.035f;
        private Selectable control;
        private Vector3 restingScale;
        private bool hovered;
        private bool pressed;
        private bool focused = true;

        private void Awake()
        {
            control = GetComponent<Selectable>();
            restingScale = transform.localScale;
        }

        private void Update()
        {
            bool available = focused && control != null && control.IsActive() && control.IsInteractable();
            bool mobile = MenuPlatform.IsMobile;
            if (mobile) hovered = false;
            bool selected = !mobile && MenuInputMode.UsingNavigation && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject;
            float factor = !available ? 1f : pressed ? pressedScale : hovered || selected ? hoverScale : 1f;
            float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, responseSeconds));
            transform.localScale = Vector3.Lerp(transform.localScale, restingScale * factor, blend);
            if (!available) hovered = pressed = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            MenuInputMode.UsePointer();
            if (!MenuPlatform.IsMobile) hovered = true;
        }
        public void OnPointerExit(PointerEventData eventData) => hovered = pressed = false;
        public void OnPointerDown(PointerEventData eventData)
        {
            MenuInputMode.UsePointer();
            if (eventData.button == PointerEventData.InputButton.Left) pressed = true;
        }
        public void OnPointerUp(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pressed = false; }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) hovered = pressed = false; }

        private void OnDisable()
        {
            hovered = pressed = false;
            if (control != null) transform.localScale = restingScale;
        }
    }
}
