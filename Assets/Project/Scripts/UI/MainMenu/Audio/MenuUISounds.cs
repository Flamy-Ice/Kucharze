using UnityEngine;
using UnityEngine.Audio;

namespace ModularMovement
{
    // Plays optional menu sounds from one source that survives screen and scene changes.
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class MenuUISounds : MonoBehaviour
    {
        private static MenuUISounds instance;
        [SerializeField, Tooltip("Sound played when the pointer enters an available control.")]
        private AudioClip hoverClip;
        [SerializeField, Tooltip("Sound played when an available control is activated.")]
        private AudioClip clickClip;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
        [SerializeField, Min(0f)] private float minimumHoverInterval = 0.04f;
        private AudioSource source;
        private float nextHoverTime;

        public static MenuUISounds Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (hoverClip != null) instance.hoverClip = hoverClip;
                if (clickClip != null) instance.clickClip = clickClip;
                Destroy(gameObject);
                return;
            }
            instance = this;
            // Leave the editor's folder so this service can survive scene changes on its own.
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = outputMixerGroup;
        }

        public void PlayHover()
        {
            if (Time.unscaledTime < nextHoverTime || hoverClip == null) return;
            nextHoverTime = Time.unscaledTime + Mathf.Max(0f, minimumHoverInterval);
            Play(hoverClip);
        }

        public void PlayClick() => Play(clickClip);

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null && isActiveAndEnabled && source.isActiveAndEnabled)
                source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
