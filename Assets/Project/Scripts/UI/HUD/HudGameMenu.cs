using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if KUCHARZE_ONLINE
using Unity.Netcode;
#endif

namespace ModularMovement
{
    // Opens a local menu without pausing the shared online game.
    [DisallowMultipleComponent]
    public sealed class HudGameMenu : MonoBehaviour
    {
        [SerializeField] private Button menuButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject[] touchControls;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private GameObject menuContent;
        [SerializeField] private GameObject optionsPanel;
        private bool[] controlsWereActive;
        private PlayerInputActions blockedInput;
        private bool controlsWereBlocked;
        public bool IsOpen => panel != null && panel.activeSelf;

        private void OnEnable()
        {
            menuButton.onClick.AddListener(Open);
            resumeButton.onClick.AddListener(Close);
            leaveButton.onClick.AddListener(Leave);
            if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
            if (optionsBackButton != null) optionsBackButton.onClick.AddListener(BackToMenu);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsOpen && optionsPanel != null && optionsPanel.activeSelf) BackToMenu();
                else if (IsOpen) Close(); else Open();
            }
        }

        public void Open()
        {
            if (panel == null || IsOpen) return;
#if KUCHARZE_ONLINE
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                blockedInput = manager.LocalClient.PlayerObject.GetComponent<PlayerInputActions>();
#endif
            if (blockedInput != null)
            {
                controlsWereBlocked = blockedInput.ControlsBlocked;
                blockedInput.SetControlsBlocked(true);
            }
            controlsWereActive = new bool[touchControls.Length];
            for (int i = 0; i < touchControls.Length; i++)
            {
                if (touchControls[i] == null) continue;
                controlsWereActive[i] = touchControls[i].activeSelf;
                touchControls[i].SetActive(false);
            }
            panel.SetActive(true);
            BackToMenu();
        }

        public void OpenOptions()
        {
            if (!IsOpen || optionsPanel == null) return;
            if (menuContent != null) menuContent.SetActive(false);
            optionsPanel.SetActive(true);
        }

        public void BackToMenu()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (menuContent != null) menuContent.SetActive(true);
        }

        public void Close()
        {
            BackToMenu();
            if (panel != null) panel.SetActive(false);
            if (blockedInput != null) blockedInput.SetControlsBlocked(controlsWereBlocked);
            blockedInput = null;
            if (controlsWereActive == null) return;
            for (int i = 0; i < touchControls.Length; i++)
                if (touchControls[i] != null) touchControls[i].SetActive(controlsWereActive[i]);
            controlsWereActive = null;
        }

        private void Leave()
        {
            Close();
#if KUCHARZE_ONLINE
            var manager = NetworkManager.Singleton;
            if (manager == null) return;
            if (manager.TryGetComponent(out RelayConnection connection)) connection.Disconnect();
            else manager.Shutdown();
#endif
        }

        private void OnDisable()
        {
            menuButton.onClick.RemoveListener(Open);
            resumeButton.onClick.RemoveListener(Close);
            leaveButton.onClick.RemoveListener(Leave);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OpenOptions);
            if (optionsBackButton != null) optionsBackButton.onClick.RemoveListener(BackToMenu);
            Close();
        }
    }
}
