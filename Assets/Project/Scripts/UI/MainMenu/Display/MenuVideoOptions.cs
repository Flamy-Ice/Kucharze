using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Connects the platform's display controls to the saved video preferences.
    public sealed class MenuVideoOptions : MonoBehaviour
    {
        [SerializeField] private Dropdown frameLimit;
        [SerializeField] private Toggle verticalSync;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private Text hint;
        [SerializeField] private Dropdown resolution;
        [SerializeField] private Text resolutionLabel;
        private int[] limits;
        private Vector2Int[] resolutions;
        private MenuVideoSettings settings;

        private void OnEnable()
        {
            settings = MenuVideoSettings.Instance;
            if (settings == null || frameLimit == null || verticalSync == null || fullscreen == null) return;
            settings.RefreshPlatform();
            settings.PlatformChanged += RefreshControls;
            frameLimit.onValueChanged.AddListener(OnFrameLimit);
            verticalSync.onValueChanged.AddListener(OnVSync);
            fullscreen.onValueChanged.AddListener(OnFullscreen);
            if (resolution != null)
            {
                if (resolution.GetComponent<MenuDropdownSelectionScroll>() == null)
                    resolution.gameObject.AddComponent<MenuDropdownSelectionScroll>();
                resolution.onValueChanged.AddListener(OnResolution);
            }
            RefreshControls();
        }

        private void RefreshControls()
        {
            if (settings == null) return;
            frameLimit.Hide();
            limits = MenuVideoSettings.FrameLimits;
            frameLimit.ClearOptions();
            frameLimit.AddOptions(limits.Select(value => value < 0 ? "Unlimited" : value + " FPS").ToList());
            frameLimit.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(limits, settings.FrameLimit)));
            verticalSync.SetIsOnWithoutNotify(settings.VSyncEnabled);
            fullscreen.SetIsOnWithoutNotify(settings.Fullscreen);
            verticalSync.gameObject.SetActive(MenuVideoSettings.SupportsDisplayOptions);
            fullscreen.gameObject.SetActive(MenuVideoSettings.SupportsDisplayOptions);
            if (resolution != null)
            {
                resolution.Hide();
                resolution.gameObject.SetActive(MenuVideoSettings.SupportsDisplayOptions);
                if (MenuVideoSettings.SupportsDisplayOptions)
                {
                    resolutions = settings.AvailableResolutions;
                    resolution.ClearOptions();
                    resolution.AddOptions(resolutions.Select(value => value.x + " x " + value.y).ToList());
                    resolution.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(resolutions, settings.DisplayResolution)));
                }
            }
            if (resolutionLabel != null) resolutionLabel.gameObject.SetActive(MenuVideoSettings.SupportsDisplayOptions);
            UpdateAvailability();
        }

        private void OnFrameLimit(int index)
        {
            if (MenuVideoSettings.Instance != null && limits != null && index >= 0 && index < limits.Length)
                MenuVideoSettings.Instance.SetFrameLimit(limits[index]);
        }

        private void OnVSync(bool value)
        {
            if (MenuVideoSettings.Instance != null) MenuVideoSettings.Instance.SetVSync(value);
            UpdateAvailability();
        }

        private void OnFullscreen(bool value)
        {
            if (MenuVideoSettings.Instance != null) MenuVideoSettings.Instance.SetFullscreen(value);
        }

        private void OnResolution(int index)
        {
            if (settings != null && resolutions != null && index >= 0 && index < resolutions.Length)
                settings.SetResolution(resolutions[index]);
        }

        private void UpdateAvailability()
        {
            bool synchronized = MenuVideoSettings.SupportsDisplayOptions && verticalSync.isOn;
            frameLimit.interactable = !synchronized;
            if (hint != null) hint.text = "Your settings are saved automatically.";
        }

        private void OnDisable()
        {
            if (settings != null) settings.PlatformChanged -= RefreshControls;
            settings = null;
            if (frameLimit != null) frameLimit.onValueChanged.RemoveListener(OnFrameLimit);
            if (verticalSync != null) verticalSync.onValueChanged.RemoveListener(OnVSync);
            if (fullscreen != null) fullscreen.onValueChanged.RemoveListener(OnFullscreen);
            if (resolution != null) { resolution.Hide(); resolution.onValueChanged.RemoveListener(OnResolution); }
            if (MenuVideoSettings.Instance != null) MenuVideoSettings.Instance.Save();
        }
    }
}
