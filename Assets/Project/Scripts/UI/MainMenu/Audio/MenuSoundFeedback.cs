using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularMovement
{
    // Sends control interactions to the optional menu sound player without owning any clips.
    [DisallowMultipleComponent, RequireComponent(typeof(Selectable))]
    public sealed class MenuSoundFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler,
        ISelectHandler, ISubmitHandler
    {
        private Selectable control;
        private Button button;
        private Toggle toggle;
        private Dropdown dropdown;

        private void Awake()
        {
            control = GetComponent<Selectable>();
            button = GetComponent<Button>();
            toggle = GetComponent<Toggle>();
            dropdown = GetComponent<Dropdown>();
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(PlayClick);
            if (toggle != null) toggle.onValueChanged.AddListener(OnToggle);
            if (dropdown != null) dropdown.onValueChanged.AddListener(OnDropdown);
        }

        private bool Available => control != null && control.IsActive() && control.IsInteractable();
        // A button can hide its screen before this listener runs, so keep its accepted click audible.
        private void PlayClick()
        {
            if (control != null && control.IsInteractable() && MenuUISounds.Instance != null) MenuUISounds.Instance.PlayClick();
        }
        private void OnToggle(bool value) => PlayClick();
        private void OnDropdown(int value) => PlayClick();

        public void OnPointerEnter(PointerEventData eventData)
        {
            MenuInputMode.UsePointer();
            if (button != null && !MenuPlatform.IsMobile && Available && MenuUISounds.Instance != null) MenuUISounds.Instance.PlayHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dropdown != null && eventData.button == PointerEventData.InputButton.Left) PlayClick();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (button != null && MenuInputMode.UsingNavigation && !MenuPlatform.IsMobile && Available && MenuUISounds.Instance != null)
                MenuUISounds.Instance.PlayHover();
        }

        public void OnSubmit(BaseEventData eventData) { if (dropdown != null) PlayClick(); }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayClick);
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggle);
            if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnDropdown);
        }
    }
}
