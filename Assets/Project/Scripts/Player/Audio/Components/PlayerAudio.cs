using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ModularMovement
{
    // Plays this player's footsteps and dash sound without changing movement.
    [DisallowMultipleComponent, AddComponentMenu("Audio/Player Audio")]
    [RequireComponent(typeof(PlayerStates), typeof(PlayerBus))]
    public sealed class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField] private AudioClip[] walkClips = System.Array.Empty<AudioClip>();
        // Keep the old saved field only to move existing Walk assignments into the list.
        [SerializeField, HideInInspector] private AudioClip walkClip;
        [SerializeField] private AudioClip dashClip;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.7f;
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.08f;
        [SerializeField, Range(0f, 1f)] private float dashVolume = 0.8f;

        private PlayerStates states;
        private PlayerBus bus;
        // Choose random clips and pitches separately for each player, without changing other random game events.
        private readonly System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());
        // Separate playback sources let each overlapping sound keep its own pitch.
        private readonly AudioSource[] footstepSources = new AudioSource[4];
        private readonly AudioSource[] dashSources = new AudioSource[4];
        private AudioMixerGroup appliedMixerGroup;
        private GameObject audioObject;
        private int nextFootstepSource;
        private int nextDashSource;
        private float previousPitch = 1f;
        private Vector3 previousPosition;
        private bool pendingAnimationFootstep;
        private const float ReferenceRefreshInterval = 0.5f;
        private readonly List<Animator> animatorCandidates = new List<Animator>();
        private PlayerAnimation playerAnimation;
        private Animator configuredModelAnimator;
        private Animator resolvedFootstepAnimator;
        private Animator receiverAnimator;
        private bool hadPlayerAnimation;
        private bool hadResolvedAnimator;
        private bool referencesDirty = true;
        private float nextReferenceRefresh;
        private PlayerFootstepEvents footstepEvents;

        // Use the model chosen in PlayerAnimation, or the player's only Animator if there is no such component.
        internal Animator ResolvedFootstepAnimator
        {
            get
            {
                RefreshFootstepReferences();
                return resolvedFootstepAnimator;
            }
        }

        public AudioClip[] WalkClips
        {
            get => (AudioClip[])walkClips.Clone();
            set => walkClips = value == null ? System.Array.Empty<AudioClip>() : (AudioClip[])value.Clone();
        }
        internal void GetPlayingFootstepClips(List<AudioClip> clips) => GetPlayingClips(footstepSources, clips);

        internal void GetPlayingDashClips(List<AudioClip> clips) => GetPlayingClips(dashSources, clips);

        private static void GetPlayingClips(AudioSource[] sources, List<AudioClip> clips)
        {
            clips.Clear();
            foreach (AudioSource source in sources)
                if (source != null && source.isActiveAndEnabled && source.isPlaying
                    && source.clip != null && !clips.Contains(source.clip))
                    clips.Add(source.clip);
        }
        public AudioClip DashClip { get => dashClip; set => dashClip = value; }
        public float PitchVariation
        {
            get => pitchVariation;
            set => pitchVariation = IsFinite(value) ? Mathf.Clamp(value, 0f, 0.3f) : 0.08f;
        }
        public float FootstepVolume
        {
            get => footstepVolume;
            set => footstepVolume = Volume(value, 0.7f);
        }
        public float DashVolume
        {
            get => dashVolume;
            set => dashVolume = Volume(value, 0.8f);
        }

        private void OnEnable()
        {
            MigrateWalkClip();
            states = GetComponent<PlayerStates>();
            bus = GetComponent<PlayerBus>();
            previousPosition = transform.position;
            pendingAnimationFootstep = false;
            referencesDirty = true;
            RefreshFootstepReceiver();
            if (bus != null) bus.DashStarted += OnDashStarted;
        }

        private void OnDisable()
        {
            if (bus != null) bus.DashStarted -= OnDashStarted;
            pendingAnimationFootstep = false;
            BindFootstepReceiver(null);
            receiverAnimator = null;
            // These sources belong only to this component; other player sounds keep playing.
            foreach (AudioSource source in footstepSources)
                if (source != null) source.Stop();
            foreach (AudioSource source in dashSources)
                if (source != null) source.Stop();
        }

        private void OnDestroy()
        {
            if (audioObject != null) Destroy(audioObject);
        }

        // Check markers after the motor finishes this frame's movement.
        private void LateUpdate() => StepAudio(Time.deltaTime);

        private void OnTransformChildrenChanged() => referencesDirty = true;

        internal void StepAudio(float deltaTime)
        {
            RefreshMixerRouting();
            Vector3 position = transform.position;
            Vector3 displacement = position - previousPosition;
            // Remember the current position even when silent, so old movement is not counted later.
            previousPosition = position;
            RefreshFootstepReceiver();
            bool animationStep = pendingAnimationFootstep;
            pendingAnimationFootstep = false;
            if (!animationStep || !isActiveAndEnabled || !IsFinite(deltaTime) || deltaTime <= 0f
                || states == null || !states.isActiveAndEnabled || !states.IsGrounded || !states.IsMoving)
                return;

            string source = states.ActiveMovementSource;
            if (source != "Walk" && source != "Run")
                return;
            // A blocked player must stay silent even if the animation still reaches a marker.
            if (!IsFinite(displacement.x) || !IsFinite(displacement.z)
                || (displacement.x == 0f && displacement.z == 0f)) return;
            PlayFootstep();
        }

        private bool IsLocalAnimator(Animator animator)
            => states != null && animator != null && animator.GetComponentInParent<PlayerStates>(true) == states;

        private bool RefreshFootstepReferences()
        {
            bool selectionChanged = playerAnimation != null && playerAnimation.ModelAnimator != configuredModelAnimator;
            bool referenceDestroyed = (hadPlayerAnimation && playerAnimation == null)
                || (hadResolvedAnimator && resolvedFootstepAnimator == null);
            if (Application.isPlaying && !referencesDirty && !selectionChanged && !referenceDestroyed
                && Time.unscaledTime < nextReferenceRefresh) return false;

            referencesDirty = false;
            nextReferenceRefresh = Time.unscaledTime + ReferenceRefreshInterval;
            if (states == null) states = GetComponent<PlayerStates>();
            playerAnimation = GetComponent<PlayerAnimation>();
            hadPlayerAnimation = playerAnimation != null;
            configuredModelAnimator = playerAnimation != null ? playerAnimation.ModelAnimator : null;
            resolvedFootstepAnimator = null;
            if (playerAnimation != null)
            {
                Animator selected = playerAnimation.ResolvedAnimator;
                if (IsLocalAnimator(selected)) resolvedFootstepAnimator = selected;
            }
            else
            {
                // Reuse the list, including when no model or several models can be found.
                GetComponentsInChildren(true, animatorCandidates);
                foreach (Animator animator in animatorCandidates)
                {
                    if (!IsLocalAnimator(animator)) continue;
                    if (resolvedFootstepAnimator != null)
                    {
                        resolvedFootstepAnimator = null;
                        break;
                    }
                    resolvedFootstepAnimator = animator;
                }
                animatorCandidates.Clear();
            }
            hadResolvedAnimator = resolvedFootstepAnimator != null;
            return true;
        }

        private void RefreshFootstepReceiver()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                BindFootstepReceiver(null);
                return;
            }
            bool refreshed = RefreshFootstepReferences();
            Animator animator = resolvedFootstepAnimator;
            if (!refreshed && receiverAnimator == animator && (animator == null || footstepEvents != null)) return;
            PlayerFootstepEvents receiver = animator == null ? null : animator.GetComponent<PlayerFootstepEvents>();
            if (animator != null && receiver == null)
                receiver = animator.gameObject.AddComponent<PlayerFootstepEvents>();
            BindFootstepReceiver(receiver);
            receiverAnimator = animator;
        }

        private void BindFootstepReceiver(PlayerFootstepEvents receiver)
        {
            if (footstepEvents == receiver) return;
            if (footstepEvents != null) footstepEvents.Footstep -= QueueAnimationFootstep;
            footstepEvents = receiver;
            pendingAnimationFootstep = false;
            if (footstepEvents != null) footstepEvents.Footstep += QueueAnimationFootstep;
        }

        private void QueueAnimationFootstep()
        {
            if (!isActiveAndEnabled
                || footstepEvents == null || !footstepEvents.isActiveAndEnabled
                || receiverAnimator != ResolvedFootstepAnimator || !IsLocalAnimator(receiverAnimator)) return;
            // Wait until movement is finished and play at most one footstep for markers received this frame.
            pendingAnimationFootstep = true;
        }

        private void OnDashStarted()
        {
            // A selected dash counts even when a wall prevents it from travelling.
            if (!isActiveAndEnabled) return;
            RefreshMixerRouting();
            pendingAnimationFootstep = false;
            // A dash cuts off this player's footstep tails, even when its own sound is muted.
            foreach (AudioSource footstepSource in footstepSources)
                if (footstepSource != null) footstepSource.Stop();
            if (!IsFinite(Time.deltaTime) || Time.deltaTime <= 0f || dashClip == null || dashVolume <= 0f) return;
            AudioSource source = GetSource(dashSources, ref nextDashSource);
            if (source.isActiveAndEnabled) PlaySound(source, dashClip, dashVolume);
        }

        private void PlayFootstep()
        {
            if (footstepVolume <= 0f) return;
            AudioClip clip = ChooseFootstep();
            if (clip == null) return;
            AudioSource source = GetSource(footstepSources, ref nextFootstepSource);
            if (!source.isActiveAndEnabled) return;
            PlaySound(source, clip, footstepVolume);
        }

        private void PlaySound(AudioSource source, AudioClip clip, float volume)
        {
            float pitch = 1f + ((float)random.NextDouble() * 2f - 1f) * pitchVariation;
            // Even a single recording gets a different pitch on the next sound by default.
            if (pitchVariation > 0f && pitch == previousPitch)
                pitch = pitch <= 1f ? 1f + pitchVariation : 1f - pitchVariation;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
            previousPitch = pitch;
        }

        private AudioClip ChooseFootstep()
        {
            AudioClip selected = null;
            int candidates = 0;
            foreach (AudioClip clip in walkClips)
            {
                if (clip == null) continue;
                // Include every valid entry. Excluding the last clip forces two clips to alternate.
                if (random.Next(++candidates) == 0) selected = clip;
            }
            return selected;
        }

        private AudioSource GetSource(AudioSource[] sources, ref int nextSource)
        {
            for (int offset = 0; offset < sources.Length; offset++)
            {
                int index = (nextSource + offset) % sources.Length;
                AudioSource source = sources[index];
                if (source != null && source.isPlaying) continue;
                if (source == null) source = sources[index] = CreateSource();
                nextSource = (index + 1) % sources.Length;
                return source;
            }
            // If all four sources are busy, stop and reuse the next one instead of adding more.
            AudioSource oldest = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            return oldest;
        }

        private AudioSource CreateSource()
        {
            if (audioObject == null)
            {
                audioObject = new GameObject("PlayerMovementAudio");
                audioObject.transform.SetParent(transform, false);
            }
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 25f;
            source.outputAudioMixerGroup = outputMixerGroup;
            return source;
        }

        private void RefreshMixerRouting()
        {
            if (appliedMixerGroup == outputMixerGroup) return;
            // Send already created sound sources to the new Mixer group when its assignment changes.
            appliedMixerGroup = outputMixerGroup;
            foreach (AudioSource source in footstepSources)
                if (source != null) source.outputAudioMixerGroup = outputMixerGroup;
            foreach (AudioSource source in dashSources)
                if (source != null) source.outputAudioMixerGroup = outputMixerGroup;
        }

        private void OnValidate()
        {
            referencesDirty = true;
            PitchVariation = pitchVariation;
            FootstepVolume = footstepVolume;
            DashVolume = dashVolume;
            MigrateWalkClip();
        }

        private void MigrateWalkClip()
        {
            if (walkClips == null) walkClips = System.Array.Empty<AudioClip>();
            if (walkClips.Length == 0 && walkClip != null) walkClips = new[] { walkClip };
            walkClip = null;
        }

        private static float Volume(float value, float fallback)
            => IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
