#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Sends the menu's host and join requests to the existing Relay connection.
    public sealed class MainMenuOnlinePanel : MonoBehaviour
    {
        [SerializeField] private GameObject connectionPrefab;
        [SerializeField] private string lobbyScene;
        [SerializeField] private string menuScene;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private InputField joinCode;
        [SerializeField] private Text statusLabel;
        [SerializeField] private MenuPlayerNickname nickname;
        private RelayConnection connection;
        private NetworkManager manager;

        private void Awake()
        {
            manager = NetworkManager.Singleton;
            if (manager == null && connectionPrefab != null)
                manager = Instantiate(connectionPrefab).GetComponent<NetworkManager>();
            if (manager == null) return;
            connection = manager.GetComponent<RelayConnection>();
            if (manager.TryGetComponent(out LobbySessionBootstrap bootstrap)) bootstrap.EntryScene = menuScene;
            if (manager.TryGetComponent(out LobbyConnectionView view)) view.HiddenScene = menuScene;
        }

        public void Host()
        {
            if (connection != null && nickname != null && nickname.TryCommit()) connection.HostInScene(lobbyScene);
        }

        public void Join()
        {
            if (connection != null && nickname != null && nickname.TryCommit()) connection.Join(joinCode.text);
        }

        public void Cancel()
        {
            if (connection != null) connection.Disconnect();
        }

        private void Update()
        {
            bool available = connection != null && !connection.IsBusy && !connection.IsListening
                && manager != null && !manager.ShutdownInProgress;
            bool hasNickname = nickname != null && nickname.HasValidNickname;
            hostButton.interactable = available && hasNickname;
            joinButton.interactable = available && hasNickname && !string.IsNullOrWhiteSpace(joinCode.text);
            joinCode.interactable = available;
            if (nickname != null) nickname.SetEditable(available);
            cancelButton.gameObject.SetActive(connection != null && connection.IsBusy);
            statusLabel.text = available && !hasNickname ? "Enter your nickname (2-16 characters) to host or join."
                : connection != null ? connection.Status : "Online connection is unavailable.";
        }
    }
}
#endif
