#if KUCHARZE_ONLINE
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Shows connection controls and shared readiness without changing the lobby rules.
    [RequireComponent(typeof(RelayConnection), typeof(NetworkManager))]
    public sealed class LobbyConnectionView : MonoBehaviour
    {
        private RelayConnection connection;
        private NetworkManager manager;
        private LobbySession session;
        private string enteredCode = "";
        private GUIStyle titleStyle, detailStyle, captionStyle, codeStyle, buttonStyle, inputStyle, badgeStyle, centeredCaption;
        private Texture2D cardTexture, shadowTexture, badgeTexture;
        private GUIStyle cardStyle, shadowStyle, badgeBackground;
        private readonly List<Texture2D> textures = new List<Texture2D>();

        private void Awake()
        {
            connection = GetComponent<RelayConnection>();
            manager = GetComponent<NetworkManager>();
        }

        private void Update()
        {
            if (!manager.IsListening || session != null || manager.SpawnManager == null) return;
            foreach (NetworkObject instance in manager.SpawnManager.SpawnedObjects.Values)
                if (instance.TryGetComponent(out LobbySession found)) { session = found; break; }
        }

        private void OnGUI()
        {
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1000f, Screen.height / 720f), 0.5f, 1.6f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            try
            {
                bool lobby = session == null || session.IsLobby;
                if (lobby || (session != null && session.IsLoading)) DrawReadiness(width);
                if (lobby) DrawConnection(width, height);
                else if (GUI.Button(new Rect(width - 100, 18, 82, 34), "Leave", buttonStyle)) connection.Disconnect();
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.enabled = previousEnabled;
            }
        }

        private void DrawReadiness(float width)
        {
            float cardWidth = Mathf.Min(620, width - 32);
            var rect = new Rect((width - cardWidth) * 0.5f, 16, cardWidth, 72);
            DrawCard(rect);
            string title = "Welcome to the common room";
            string detail = "Host a private lobby or join your crew below.";
            string count = "2-4";
            string countLabel = "PLAYERS";
            if (session != null)
            {
                if (session.IsLoading)
                {
                    title = "Your next shift is loading";
                    detail = "Taking the whole crew together...";
                }
                else if (session.Error.Length > 0)
                {
                    title = "We couldn't start the game";
                    detail = session.Error;
                }
                else if (session.PlayerCount < 2)
                {
                    title = "Waiting for your crew";
                    detail = "Invite at least one more player, then head to the green room.";
                    count = session.PlayerCount + "/4";
                    countLabel = "CONNECTED";
                }
                else
                {
                    title = session.SecondsRemaining > 0 ? $"Starting in {session.SecondsRemaining}..." : "Meet in the green room";
                    detail = session.SecondsRemaining > 0 ? "Stay inside together. Leaving cancels the countdown."
                        : "Everyone needs to be inside to start the next shift.";
                    count = session.ReadyCount + "/" + session.PlayerCount;
                    countLabel = "READY";
                }
            }
            GUI.Label(new Rect(rect.x + 20, rect.y + 9, cardWidth - 150, 14), "THE COMMON ROOM", captionStyle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 23, cardWidth - 150, 24), title, titleStyle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 50, cardWidth - 140, 18), detail, detailStyle);
            var badge = new Rect(rect.xMax - 111, rect.y + 8, 96, 56);
            GUI.Box(badge, GUIContent.none, badgeBackground);
            GUI.Label(new Rect(badge.x, badge.y + 4, badge.width, 29), count, badgeStyle);
            GUI.Label(new Rect(badge.x, badge.y + 32, badge.width, 16), countLabel, centeredCaption);
        }

        private void DrawConnection(float width, float height)
        {
            float cardWidth = Mathf.Min(620, width - 32);
            float cardHeight = manager.IsListening ? 56 : 112;
            var rect = new Rect((width - cardWidth) * 0.5f, height - cardHeight - 16, cardWidth, cardHeight);
            DrawCard(rect);
            if (manager.IsListening)
            {
                string code = session != null ? session.JoinCode : connection.JoinCode;
                GUI.Label(new Rect(rect.x + 20, rect.y + 7, 260, 15), "LOBBY CODE  /  INVITE YOUR CREW", captionStyle);
                GUI.Label(new Rect(rect.x + 20, rect.y + 20, 300, 30), code.Length > 0 ? code : "Connecting...", codeStyle);
                GUI.enabled = code.Length > 0;
                if (GUI.Button(new Rect(rect.xMax - 205, rect.y + 11, 96, 34), "Copy code", buttonStyle)) GUIUtility.systemCopyBuffer = code;
                GUI.enabled = true;
                if (GUI.Button(new Rect(rect.xMax - 97, rect.y + 11, 78, 34), "Leave", buttonStyle)) connection.Disconnect();
                return;
            }
            GUI.Label(new Rect(rect.x + 20, rect.y + 10, cardWidth - 40, 18), "BRING YOUR CREW", captionStyle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 30, cardWidth - 40, 20), connection.Status, detailStyle);
            GUI.enabled = !connection.IsBusy;
            float fieldWidth = cardWidth - 350;
            if (GUI.Button(new Rect(rect.x + 20, rect.y + 59, 140, 36), "Host lobby", buttonStyle)) connection.Host();
            enteredCode = GUI.TextField(new Rect(rect.x + 174, rect.y + 59, fieldWidth, 36), enteredCode, 16, inputStyle);
            if (enteredCode.Length == 0)
                GUI.Label(new Rect(rect.x + 186, rect.y + 66, fieldWidth - 24, 22), "Join code", detailStyle);
            if (GUI.Button(new Rect(rect.x + 184 + fieldWidth, rect.y + 59, 76, 36), "Join", buttonStyle)) connection.Join(enteredCode);
            GUI.enabled = connection.IsBusy;
            if (GUI.Button(new Rect(rect.xMax - 82, rect.y + 59, 62, 36), "Cancel", buttonStyle)) connection.Disconnect();
            GUI.enabled = true;
        }

        private void DrawCard(Rect rect)
        {
            GUI.Box(new Rect(rect.x, rect.y + 4, rect.width, rect.height), GUIContent.none, shadowStyle);
            GUI.Box(rect, GUIContent.none, cardStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            Color paper = new Color(0.96f, 0.93f, 0.83f);
            Color muted = new Color(0.69f, 0.76f, 0.72f);
            cardTexture = Rounded(new Color(0.09f, 0.15f, 0.17f, 0.97f));
            shadowTexture = Rounded(new Color(0.02f, 0.04f, 0.05f, 0.35f));
            badgeTexture = Rounded(new Color(0.20f, 0.36f, 0.31f));
            cardStyle = Surface(cardTexture);
            shadowStyle = Surface(shadowTexture);
            badgeBackground = Surface(badgeTexture);
            titleStyle = TextStyle(21, paper, FontStyle.Bold);
            detailStyle = TextStyle(12, muted);
            captionStyle = TextStyle(10, muted, FontStyle.Bold);
            codeStyle = TextStyle(26, paper, FontStyle.Bold);
            badgeStyle = TextStyle(24, paper, FontStyle.Bold);
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            centeredCaption = new GUIStyle(captionStyle) { alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, border = new RectOffset(10, 10, 10, 10) };
            buttonStyle.normal.background = Rounded(new Color(0.20f, 0.29f, 0.30f));
            buttonStyle.hover.background = Rounded(new Color(0.28f, 0.40f, 0.38f));
            buttonStyle.focused.background = buttonStyle.hover.background;
            buttonStyle.active.background = Rounded(new Color(0.26f, 0.48f, 0.38f));
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = paper;
            buttonStyle.focused.textColor = paper;
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 16, padding = new RectOffset(12, 12, 8, 8), border = new RectOffset(10, 10, 10, 10) };
            inputStyle.normal.background = inputStyle.focused.background = Rounded(new Color(0.14f, 0.22f, 0.24f));
            inputStyle.normal.textColor = inputStyle.focused.textColor = paper;
        }

        private static GUIStyle TextStyle(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, clipping = TextClipping.Clip };
            style.normal.textColor = color;
            return style;
        }

        private static GUIStyle Surface(Texture2D texture)
        {
            var style = new GUIStyle { border = new RectOffset(10, 10, 10, 10) };
            style.normal.background = texture;
            return style;
        }

        private Texture2D Rounded(Color color)
        {
            const int size = 32;
            const float radius = 8;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - Mathf.Clamp(x, radius, size - 1 - radius);
                    float dy = y - Mathf.Clamp(y, radius, size - 1 - radius);
                    pixels[y * size + x] = dx * dx + dy * dy <= radius * radius ? color : Color.clear;
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixels(pixels);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in textures) if (texture != null) Destroy(texture);
        }
    }
}
#endif
