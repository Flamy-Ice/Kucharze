using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ModularMovement
{
    // Switches the menu screens and keeps keyboard and controller focus on a visible button.
    public sealed class MainMenuNavigation : MonoBehaviour
    {
        [SerializeField] private GameObject[] screens;
        [SerializeField] private Selectable[] firstSelections;
        private int currentScreen;

        private void Start() => ShowHome();
        public void ShowHome() => Show(0);
        public void ShowPlay() => Show(1);
        public void ShowOptions() => Show(2);
        public void ShowCredits() => Show(3);

        public void Show(int index)
        {
            if (screens == null || index < 0 || index >= screens.Length) return;
            currentScreen = index;
            for (int screen = 0; screen < screens.Length; screen++)
                if (screens[screen] != null) screens[screen].SetActive(screen == index);
            if (EventSystem.current != null)
            {
                EventSystem.current.firstSelectedGameObject = null;
                EventSystem.current.SetSelectedGameObject(null);
                if (MenuInputMode.UsingNavigation) SelectFirst();
            }
        }

        private void Update()
        {
            if (MenuInputMode.ReadNavigationInput() && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == null) SelectFirst();
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back && currentScreen != 0) ShowHome();
        }

        private void SelectFirst()
        {
            if (EventSystem.current == null || firstSelections == null || currentScreen >= firstSelections.Length) return;
            Selectable target = firstSelections[currentScreen];
            if (target != null && target.IsActive() && target.IsInteractable())
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
