using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Connects the two options sliders to the saved music and sound levels.
    public sealed class MenuAudioOptions : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider soundsSlider;
        [SerializeField] private Text musicValue;
        [SerializeField] private Text soundsValue;

        private void OnEnable()
        {
            MenuAudioSettings settings = MenuAudioSettings.Instance;
            if (settings == null) return;
            musicSlider.SetValueWithoutNotify(settings.MusicVolume);
            soundsSlider.SetValueWithoutNotify(settings.SoundsVolume);
            UpdateLabels();
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            soundsSlider.onValueChanged.AddListener(OnSoundsChanged);
        }

        private void OnMusicChanged(float value)
        {
            if (MenuAudioSettings.Instance != null) MenuAudioSettings.Instance.SetMusicVolume(value);
            UpdateLabels();
        }

        private void OnSoundsChanged(float value)
        {
            if (MenuAudioSettings.Instance != null) MenuAudioSettings.Instance.SetSoundsVolume(value);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            musicValue.text = Mathf.RoundToInt(musicSlider.value * 100) + "%";
            soundsValue.text = Mathf.RoundToInt(soundsSlider.value * 100) + "%";
        }

        private void OnDisable()
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            soundsSlider.onValueChanged.RemoveListener(OnSoundsChanged);
            if (MenuAudioSettings.Instance != null) MenuAudioSettings.Instance.Save();
        }
    }
}
