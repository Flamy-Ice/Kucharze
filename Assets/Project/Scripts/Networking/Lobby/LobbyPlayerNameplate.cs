#if KUCHARZE_ONLINE
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Faces a nickname label toward the lobby camera without rotating the player.
    [DisallowMultipleComponent, RequireComponent(typeof(LobbyPlayerNickname))]
    public sealed class LobbyPlayerNameplate : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;
        [SerializeField] private string lobbyScene;
        [SerializeField] private Vector3 offset = new Vector3(0, 2.3f, 0);
        private LobbyPlayerNickname nickname;
        private Camera viewCamera;

        private void Awake() => nickname = GetComponent<LobbyPlayerNickname>();

        private void LateUpdate()
        {
            if (label == null) return;
            bool visible = nickname != null && nickname.IsSpawned && SceneManager.GetActiveScene().path == lobbyScene;
            if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
            if (!visible) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;
            label.transform.SetPositionAndRotation(transform.position + offset, viewCamera.transform.rotation);
            string text = nickname.Nickname;
            if (label.text != text) label.text = text;
        }
    }
}
#endif
