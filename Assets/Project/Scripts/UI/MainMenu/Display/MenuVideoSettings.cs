using System;
using System.Linq;
using UnityEngine;

namespace ModularMovement
{
    // Keeps this computer's display preferences when moving between the menu and the game.
    [DisallowMultipleComponent]
    public sealed class MenuVideoSettings : MonoBehaviour
    {
        private const string FrameKey = "Kucharze.Video.FrameLimit";
        private const string MobileFrameKey = "Kucharze.Video.MobileFrameLimit";
        private const string SyncKey = "Kucharze.Video.VSync";
        private const string FullscreenKey = "Kucharze.Video.Fullscreen";
        private const string WidthKey = "Kucharze.Video.Width";
        private const string HeightKey = "Kucharze.Video.Height";
        private static readonly int[] DesktopLimits = { 30, 60, 120, 144, -1 };
        private static readonly int[] MobileLimits = { 30, 60 };
        private static readonly Vector2Int[] CommonResolutions =
        {
            new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440),
            new Vector2Int(1280, 800), new Vector2Int(1680, 1050), new Vector2Int(1920, 1200), new Vector2Int(2560, 1600),
            new Vector2Int(2560, 1080), new Vector2Int(3440, 1440), new Vector2Int(5120, 2160),
            new Vector2Int(3840, 1080), new Vector2Int(5120, 1440),
            new Vector2Int(1024, 768), new Vector2Int(1280, 960), new Vector2Int(1600, 1200), new Vector2Int(1920, 1440)
        };
        private static MenuVideoSettings instance;
        private bool desktopMode;
        private bool defaultVSync;
        private bool defaultFullscreen;

        public static MenuVideoSettings Instance => instance;
        public static bool SupportsDisplayOptions => MenuPlatform.SupportsDisplayOptions;
        public static int[] FrameLimits => (int[])(SupportsDisplayOptions ? DesktopLimits : MobileLimits).Clone();
        public int FrameLimit { get; private set; }
        public bool VSyncEnabled { get; private set; }
        public bool Fullscreen { get; private set; }
        public Vector2Int DisplayResolution { get; private set; }
        public Vector2Int[] AvailableResolutions
        {
            get
            {
                Vector2Int monitor = CurrentMonitorResolution;
                float aspect = (float)monitor.x / monitor.y;
                Vector2Int[] supported = Screen.resolutions.Select(value => new Vector2Int(value.width, value.height)).ToArray();
                // Keep a short list matching this monitor, plus its own desktop resolution.
                return CommonResolutions.Where(value => supported.Contains(value) && value.x <= monitor.x && value.y <= monitor.y
                        && Mathf.Abs((float)value.x / value.y - aspect) < 0.04f)
                    .OrderBy(value => (long)value.x * value.y).Take(4).Append(monitor).Distinct()
                    .OrderBy(value => (long)value.x * value.y).ToArray();
            }
        }
        private static Vector2Int CurrentMonitorResolution
        {
            get
            {
                Resolution current = Screen.currentResolution;
                return current.width > 0 && current.height > 0
                    ? new Vector2Int(current.width, current.height) : new Vector2Int(1280, 720);
            }
        }
        public event Action PlatformChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            // Leave the editor's folder so this service can survive scene changes on its own.
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            defaultVSync = QualitySettings.vSyncCount > 0;
            defaultFullscreen = Screen.fullScreen;
            ReadPreferences();
        }

        private void Start() { Apply(); ApplyDisplay(); }
        private void Update() => RefreshPlatform();

        public void RefreshPlatform()
        {
            if (desktopMode == SupportsDisplayOptions) return;
            ReadPreferences();
            Apply();
            ApplyDisplay();
            PlatformChanged?.Invoke();
        }

        private void ReadPreferences()
        {
            desktopMode = SupportsDisplayOptions;
            // Keep the old mobile preference as a fallback, without replacing the desktop frame limit.
            int legacyLimit = PlayerPrefs.GetInt(FrameKey, desktopMode ? -1 : 60);
            FrameLimit = ValidLimit(desktopMode ? legacyLimit : PlayerPrefs.GetInt(MobileFrameKey, legacyLimit));
            VSyncEnabled = desktopMode && PlayerPrefs.GetInt(SyncKey, defaultVSync ? 1 : 0) != 0;
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, defaultFullscreen ? 1 : 0) != 0;
            if (desktopMode)
            {
                Vector2Int current = CurrentMonitorResolution;
                Vector2Int requested = new Vector2Int(PlayerPrefs.GetInt(WidthKey, current.x), PlayerPrefs.GetInt(HeightKey, current.y));
                Vector2Int[] available = AvailableResolutions;
                DisplayResolution = available.Contains(requested) ? requested
                    : available.Contains(current) ? current : available[available.Length - 1];
            }
        }

        public void SetFrameLimit(int value)
        {
            RefreshPlatform();
            FrameLimit = ValidLimit(value);
            PlayerPrefs.SetInt(desktopMode ? FrameKey : MobileFrameKey, FrameLimit);
            Apply();
        }

        public void SetVSync(bool value)
        {
            RefreshPlatform();
            if (!SupportsDisplayOptions) return;
            VSyncEnabled = value;
            PlayerPrefs.SetInt(SyncKey, value ? 1 : 0);
            Apply();
        }

        public void SetFullscreen(bool value)
        {
            RefreshPlatform();
            if (!SupportsDisplayOptions) return;
            Fullscreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            ApplyDisplay();
        }

        public void SetResolution(Vector2Int value)
        {
            RefreshPlatform();
            if (!SupportsDisplayOptions || !AvailableResolutions.Contains(value)) return;
            DisplayResolution = value;
            PlayerPrefs.SetInt(WidthKey, value.x);
            PlayerPrefs.SetInt(HeightKey, value.y);
            ApplyDisplay();
        }

        public void Save() => PlayerPrefs.Save();

        private void Apply()
        {
            QualitySettings.vSyncCount = SupportsDisplayOptions && VSyncEnabled ? 1 : 0;
            Application.targetFrameRate = FrameLimit;
        }

        private void ApplyDisplay()
        {
            // Display changes belong to the built game, not the editor's own windows.
            if (SupportsDisplayOptions && !Application.isEditor && (PlayerPrefs.HasKey(FullscreenKey) || PlayerPrefs.HasKey(WidthKey)))
            {
                FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.SetResolution(DisplayResolution.x, DisplayResolution.y, mode);
            }
        }

        private static int ValidLimit(int value)
            => Array.IndexOf(SupportsDisplayOptions ? DesktopLimits : MobileLimits, value) >= 0 ? value : SupportsDisplayOptions ? -1 : 60;

        private void OnApplicationPause(bool paused) { if (paused && instance == this) Save(); }
        private void OnApplicationQuit() { if (instance == this) Save(); }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
