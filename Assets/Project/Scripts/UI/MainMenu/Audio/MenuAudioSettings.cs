using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace ModularMovement
{
    // Saves this computer's volume settings and plays an optional music clip only in the main menu.
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class MenuAudioSettings : MonoBehaviour
    {
        private const string MusicKey = "Kucharze.Audio.Music";
        private const string SoundsKey = "Kucharze.Audio.Sounds";
        private static MenuAudioSettings instance;
        [SerializeField] private AudioMixer mixer;
        [SerializeField, Tooltip("Drag the music file for the main menu here. An empty field means no menu music.")]
        private AudioClip menuMusic;
        [SerializeField] private AudioMixerGroup musicOutput;
        [SerializeField] private string menuScene;
        private AudioSource music;

        public static MenuAudioSettings Instance => instance;
        public float MusicVolume { get; private set; }
        public float SoundsVolume { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                // A return to the menu reuses the music player that survived the scene change.
                if (menuMusic != null) instance.menuMusic = menuMusic;
                instance.RefreshMusic(SceneManager.GetActiveScene());
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            music = GetComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.outputAudioMixerGroup = musicOutput;
            MusicVolume = SavedVolume(MusicKey);
            SoundsVolume = SavedVolume(SoundsKey);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Mixer parameters must be applied after Awake, when Unity's audio system is ready.
        private void Start()
        {
            ApplyVolumes();
            RefreshMusic(SceneManager.GetActiveScene());
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = ValidVolume(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            ApplyVolumes();
        }

        public void SetSoundsVolume(float value)
        {
            SoundsVolume = ValidVolume(value);
            PlayerPrefs.SetFloat(SoundsKey, SoundsVolume);
            ApplyVolumes();
        }

        public void Save() => PlayerPrefs.Save();

        private void ApplyVolumes()
        {
            if (mixer == null) return;
            mixer.SetFloat("MusicVolume", Decibels(MusicVolume));
            mixer.SetFloat("SoundsVolume", Decibels(SoundsVolume));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshMusic(scene);

        private void RefreshMusic(Scene scene)
        {
            if (music == null) return;
            if (scene.path != menuScene || menuMusic == null) { music.Stop(); return; }
            if (music.clip == menuMusic && music.isPlaying) return;
            music.clip = menuMusic;
            music.Play();
        }

        private static float SavedVolume(string key) => ValidVolume(PlayerPrefs.GetFloat(key, 1f));
        private static float ValidVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        private static float Decibels(float value) => value <= 0f ? -80f : Mathf.Log10(value) * 20f;

        private void OnApplicationPause(bool paused) { if (paused && instance == this) Save(); }
        private void OnApplicationQuit() { if (instance == this) Save(); }
        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
}
