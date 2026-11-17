using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Connects menu buttons to scene loading and the menu's three panels.
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Main Menu")]
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField, Tooltip("Gameplay scene name or full asset path. Add it to the build scene list.")]
        private string gameplaySceneName;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        private bool isLoading;

        private void Start()
        {
            ShowMainMenu();
        }

        public void StartGame()
        {
            if (isLoading)
                return;

            if (string.IsNullOrWhiteSpace(gameplaySceneName) ||
                !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                Debug.LogWarning("MainMenu: Assign a gameplay scene included in the build scene list.", this);
                return;
            }

            isLoading = true;
            // A previous pause menu may have stopped game time.
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        }

        public void ShowMainMenu()
        {
            ShowPanel(mainPanel);
        }

        public void ShowSettings()
        {
            ShowPanel(settingsPanel);
        }

        public void ShowCredits()
        {
            ShowPanel(creditsPanel);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowPanel(GameObject panel)
        {
            if (isLoading)
                return;

            if (mainPanel == null || settingsPanel == null || creditsPanel == null)
            {
                Debug.LogWarning("MainMenu: Assign Main Panel, Settings Panel and Credits Panel.", this);
                return;
            }

            mainPanel.SetActive(panel == mainPanel);
            settingsPanel.SetActive(panel == settingsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
        }
    }
}
