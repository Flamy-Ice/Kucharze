#if KUCHARZE_ONLINE
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Shows only connected players, using the same replicated names and colors as their models.
    [DisallowMultipleComponent]
    public sealed class HudPlayerRoster : MonoBehaviour
    {
        [SerializeField] private Image[] backgrounds;
        [SerializeField] private TMP_Text[] nicknames;
        private float nextRefresh;

        private void OnEnable() => Refresh();
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            Refresh();
        }

        public void Refresh()
        {
            if (backgrounds == null || nicknames == null) return;
            var players = FindObjectsByType<PlayerNetworkAppearance>();
            Array.Sort(players, (left, right) =>
            {
                int order = left.PaletteIndex.CompareTo(right.PaletteIndex);
                return order != 0 ? order : left.OwnerClientId.CompareTo(right.OwnerClientId);
            });
            int slot = 0;
            foreach (var player in players)
            {
                if (!player.IsSpawned || slot >= backgrounds.Length || slot >= nicknames.Length) continue;
                Image background = backgrounds[slot];
                TMP_Text label = nicknames[slot++];
                if (background == null || label == null) continue;
                background.gameObject.SetActive(true);
                background.color = LobbyPlayerColors.Get(player.PaletteIndex);
                var profile = player.GetComponent<LobbyPlayerNickname>();
                string nickname = profile != null ? profile.Nickname : "";
                label.text = string.IsNullOrWhiteSpace(nickname) ? LobbyPlayerNickname.LoadingName : nickname;
            }
            for (; slot < backgrounds.Length; slot++)
                if (backgrounds[slot] != null) backgrounds[slot].gameObject.SetActive(false);
        }
    }
}
#endif
