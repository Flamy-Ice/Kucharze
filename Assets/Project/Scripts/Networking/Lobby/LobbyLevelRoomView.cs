#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Shows each level's icon, availability and shared selection in the lobby room.
    [DisallowMultipleComponent]
    public sealed class LobbyLevelRoomView : MonoBehaviour
    {
        [SerializeField] private Renderer[] pads = new Renderer[9];
        [SerializeField] private TextMesh[] titles = new TextMesh[9];
        [SerializeField] private SpriteRenderer[] icons = new SpriteRenderer[9];
        [SerializeField] private TextMesh[] placeholders = new TextMesh[9];
        [SerializeField] private TextMesh statusLabel;
        private LobbyConnectionView view;
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private void LateUpdate()
        {
            // Create native drawing data on the main thread, including after script reloads.
            if (properties == null) properties = new MaterialPropertyBlock();
            if (view == null && NetworkManager.Singleton != null)
                view = NetworkManager.Singleton.GetComponent<LobbyConnectionView>();
            LobbyLevelSelection selection = view != null && view.Session != null ? view.Session.LevelSelection : null;
            for (int index = 0; index < 9; index++)
            {
                bool available = selection != null && selection.IsLevelAvailable(index);
                bool chosen = selection != null && selection.HasSelection && selection.SelectedIndex == index;
                if (pads != null && index < pads.Length && pads[index] != null)
                {
                    properties.Clear();
                    properties.SetColor(BaseColor, chosen ? new Color(0.38f, 0.78f, 0.58f)
                        : available ? new Color(0.64f, 0.72f, 0.78f) : new Color(0.38f, 0.40f, 0.41f));
                    pads[index].SetPropertyBlock(properties);
                }
                if (titles != null && index < titles.Length && titles[index] != null)
                {
                    string title = selection != null ? selection.GetTitle(index) : "Level " + (index + 1);
                    string text = title + (chosen ? "\nSELECTED" : available ? "" : "\nUNAVAILABLE");
                    if (titles[index].text != text) titles[index].text = text;
                }
                Sprite icon = selection != null ? selection.GetIcon(index) : null;
                if (icons != null && index < icons.Length && icons[index] != null && icons[index].sprite != icon)
                {
                    icons[index].sprite = icon;
                    if (icon != null)
                    {
                        Vector2 size = icon.bounds.size;
                        float scale = 0.9f / Mathf.Max(0.001f, Mathf.Max(size.x, size.y));
                        icons[index].transform.localScale = new Vector3(scale, scale, 1);
                    }
                }
                if (placeholders != null && index < placeholders.Length && placeholders[index] != null)
                    placeholders[index].gameObject.SetActive(icon == null);
            }
            if (statusLabel != null)
            {
                string status = selection != null && selection.HasSelection
                    ? "SELECTED: " + selection.GetTitle(selection.SelectedIndex) + "\nMEET ON THE GREEN FLOOR"
                    : "HOST: SELECT A LEVEL\nSTAY ON A PAD TO CONFIRM";
                if (statusLabel.text != status) statusLabel.text = status;
            }
        }
    }
}
#endif
