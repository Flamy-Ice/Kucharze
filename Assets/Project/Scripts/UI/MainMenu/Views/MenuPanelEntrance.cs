using UnityEngine;

namespace ModularMovement
{
    // Welcomes the player with a short panel entrance, then restores the authored layout exactly.
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class MenuPanelEntrance : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float durationSeconds = 0.55f;
        [SerializeField, Range(0.25f, 2f)] private float intensity = 1f;
        [SerializeField] private Vector2 startingOffset = new Vector2(0, -150);
        [SerializeField, Range(0.5f, 1f)] private float startingScale = 0.76f;
        private RectTransform panel;
        private CanvasGroup visibility;
        private Vector2 restingPosition;
        private Vector3 restingScale;
        private Quaternion restingRotation;
        private float restingAlpha;
        private bool restingInteraction, restingRaycasts;
        private float elapsed;
        private bool playing, firstFrame;
        public bool IsPlaying => playing;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (panel != null) return;
            panel = GetComponent<RectTransform>();
            visibility = GetComponent<CanvasGroup>();
            restingPosition = panel.anchoredPosition;
            restingScale = panel.localScale;
            restingRotation = panel.localRotation;
            restingAlpha = visibility.alpha;
            restingInteraction = visibility.interactable;
            restingRaycasts = visibility.blocksRaycasts;
        }

        private void OnEnable() => Play();

        // Navigation can restart an already active Home screen after startup has finished loading.
        public void Play()
        {
            Initialize();
            elapsed = 0;
            firstFrame = true;
            playing = true;
            visibility.interactable = visibility.blocksRaycasts = false;
            Apply(0);
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        // The Inspector preview uses the same timing and motion as the game.
        public void Advance(float deltaSeconds)
        {
            if (!IsPlaying) return;
            // Give the hidden starting pose a rendered frame; a long loading frame must not skip the entrance.
            if (firstFrame) { firstFrame = false; return; }
            elapsed += Mathf.Clamp(deltaSeconds, 0, 1f / 30f);
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, durationSeconds));
            Apply(progress);
            // Let the player act as soon as the panel is visible, before the motion finishes.
            if (elapsed >= Mathf.Min(0.14f, durationSeconds)) RestoreInteraction();
            if (progress >= 1) Complete();
        }

        private void Apply(float progress)
        {
            float inverse = 1 - progress;
            float travel = inverse * inverse * inverse;
            float shifted = progress - 1;
            float spring = 1 + 3.6f * shifted * shifted * shifted + 2.6f * shifted * shifted;
            float initialScale = Mathf.Clamp(1 - (1 - startingScale) * intensity, 0.1f, 1);
            panel.anchoredPosition = restingPosition + startingOffset * (0.3f * travel * intensity);
            panel.localScale = restingScale * Mathf.LerpUnclamped(initialScale, 1, spring);
            visibility.alpha = restingAlpha * Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress / 0.6f));
        }

        private void RestoreInteraction()
        {
            visibility.interactable = restingInteraction;
            visibility.blocksRaycasts = restingRaycasts;
        }

        public void Complete()
        {
            playing = false;
            if (panel == null || visibility == null) return;
            panel.anchoredPosition = restingPosition;
            panel.localScale = restingScale;
            panel.localRotation = restingRotation;
            visibility.alpha = restingAlpha;
            RestoreInteraction();
        }

        private void OnDisable()
        {
            Complete();
        }

        // Let the next editor preview capture any layout edits made after this preview ends.
        public void ReleasePreview()
        {
            Complete();
            panel = null;
            visibility = null;
        }
    }
}
