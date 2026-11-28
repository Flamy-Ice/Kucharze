#if KUCHARZE_ONLINE
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Shows the nickname assigned to this room, even when its player walks elsewhere.
    [DisallowMultipleComponent, RequireComponent(typeof(TextMeshPro))]
    public sealed class LobbyRoomOccupantSign : MonoBehaviour
    {
        [SerializeField, Range(0, 3)] private int roomIndex;
        private TMP_Text label;
        private string lastText;

        private void Awake() => label = GetComponent<TMP_Text>();

        private void LateUpdate()
        {
            string text = "WAITING\nFOR PLAYER";
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening && manager.SpawnManager != null)
                foreach (NetworkObject player in manager.SpawnManager.SpawnedObjects.Values)
                {
                    if (!player.IsSpawned || !player.IsPlayerObject
                        || !player.TryGetComponent(out PlayerNetworkAppearance appearance)
                        || appearance.PaletteIndex != roomIndex
                        || !player.TryGetComponent(out LobbyPlayerNickname nickname)) continue;
                    text = nickname.Nickname;
                    if (string.IsNullOrWhiteSpace(text)) text = LobbyPlayerNickname.LoadingName;
                    break;
                }
            if (lastText == text) return;
            lastText = text;
            label.text = text;
        }
    }
}
#endif
