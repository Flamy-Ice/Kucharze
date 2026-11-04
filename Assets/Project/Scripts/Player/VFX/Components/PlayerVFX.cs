using System.Collections.Generic;
using UnityEngine;

namespace ModularMovement
{
    // Plays this player's step and dash effects without changing movement or audio.
    [DisallowMultipleComponent, AddComponentMenu("VFX/Player VFX")]
    // Unity adds the local movement readings and event source this component needs.
    [RequireComponent(typeof(PlayerStates), typeof(PlayerBus))]
    public sealed class PlayerVFX : MonoBehaviour
    {
        // A prefab is a saved effect. Each point decides where a new copy appears.
        // The Run slot supplies footsteps for both walking and running.
        [SerializeField] private GameObject runPrefab;
        [SerializeField] private Transform runSpawnPoint;
        [SerializeField] private GameObject dashPrefab;
        [SerializeField] private Transform dashSpawnPoint;
        [SerializeField, Min(0.01f)] private float maximumLifetime = 10f;

        private PlayerStates states;
        private PlayerBus bus;
        private PlayerVFXFootsteps footsteps;
        private readonly PlayerVFXInstances instances = new PlayerVFXInstances();
        // Reuse this list when searching, instead of creating a new list each time.
        private readonly List<Transform> pointCandidates = new List<Transform>();
        private Vector3 previousPosition;
        private float nextPointRefresh;

        public GameObject RunPrefab { get => runPrefab; set => runPrefab = value; }
        public GameObject DashPrefab { get => dashPrefab; set => dashPrefab = value; }
        public Transform RunSpawnPoint { get => runSpawnPoint; set => runSpawnPoint = value; }
        public Transform DashSpawnPoint { get => dashSpawnPoint; set => dashSpawnPoint = value; }
        // Reject unusable numbers so every effect has a finite time limit.
        public float MaximumLifetime
        {
            get => maximumLifetime;
            set => maximumLifetime = IsFinite(value) ? Mathf.Max(0.01f, value) : 10f;
        }

        internal Animator ResolvedFootstepAnimator => Footsteps.ResolveAnimator();
        // Let the debug panel count existing copies without creating or removing effects.
        internal void GetActiveInstanceCounts(out int runCount, out int dashCount)
            => instances.GetActiveCounts(out runCount, out dashCount);
        private PlayerVFXFootsteps Footsteps => footsteps ?? (footsteps = new PlayerVFXFootsteps(this));

        // Unity calls this when the component is added or Reset is chosen in its menu.
        private void Reset() => AssignMissingSpawnPoints();

        // Listen only to this player's accepted dashes while this component is on.
        private void OnEnable()
        {
            MaximumLifetime = maximumLifetime;
            states = GetComponent<PlayerStates>();
            bus = GetComponent<PlayerBus>();
            previousPosition = transform.position;
            AssignMissingSpawnPoints();
            Footsteps.Refresh();
            if (bus != null) bus.DashStarted += OnDashStarted;
        }

        // Stop listening and remove our copies when this component is turned off.
        private void OnDisable()
        {
            if (bus != null) bus.DashStarted -= OnDashStarted;
            footsteps?.Unbind();
            instances.Clear();
        }

        // Also clean up if Unity removes the component or its player.
        private void OnDestroy() => instances.Clear();
        // Wait until movement is finished before checking this frame's footstep marker.
        private void LateUpdate() => StepVFX(Time.deltaTime);

        internal void StepVFX(float deltaTime)
        {
            instances.Tick(deltaTime);
            Vector3 displacement = transform.position - previousPosition;
            // Remember even silent frames, so old movement cannot trigger a later step.
            previousPosition = transform.position;
            // Retry missing points twice a second, including while game time is paused.
            if (Time.unscaledTime >= nextPointRefresh)
            {
                AssignMissingSpawnPoints();
                nextPointRefresh = Time.unscaledTime + 0.5f;
            }
            Footsteps.Refresh();
            bool animationStep = Footsteps.Consume();
            // A step effect needs a marker, ground contact and actual movement.
            if (!animationStep || !isActiveAndEnabled || !IsFinite(deltaTime) || deltaTime <= 0f
                || states == null || !states.isActiveAndEnabled || !states.IsGrounded || !states.IsMoving)
                return;
            string source = states.ActiveMovementSource;
            if (source != "Walk" && source != "Run") return;
            // Pressing against a wall must not create footsteps without travel.
            if (!IsFinite(displacement.x) || !IsFinite(displacement.z)
                || (displacement.x == 0f && displacement.z == 0f)) return;
            if (IsLocalPoint(runSpawnPoint))
                instances.Play(runPrefab, runSpawnPoint, true, maximumLifetime);
        }

        // The movement system sends this only after accepting a dash, not for every button press.
        private void OnDashStarted()
        {
            if (!isActiveAndEnabled) return;
            Footsteps.Discard();
            // Like audio, a dash cuts off step effects even when no dash effect is assigned.
            instances.ClearSteps();
            if (!IsFinite(Time.deltaTime) || Time.deltaTime <= 0f) return;
            AssignMissingSpawnPoints();
            if (IsLocalPoint(dashSpawnPoint))
                instances.Play(dashPrefab, dashSpawnPoint, false, maximumLifetime);
        }

        // Reject points belonging to another player, even if that player is nested under this one.
        internal bool IsLocalPoint(Transform point)
        {
            if (states == null) states = GetComponent<PlayerStates>();
            return states != null && point != null && point != transform && point.IsChildOf(transform)
                && point.GetComponentInParent<PlayerStates>(true) == states;
        }

        // Fill empty slots only; keep points already chosen by the person setting up the player.
        internal void AssignMissingSpawnPoints()
        {
            if (runSpawnPoint != null && dashSpawnPoint != null) return;
            GetComponentsInChildren(true, pointCandidates);
            if (runSpawnPoint == null) runSpawnPoint = FindPoint("Run");
            if (dashSpawnPoint == null) dashSpawnPoint = FindPoint("Dash");
            pointCandidates.Clear();
        }

        // Several matching VFX points need a manual choice instead of an arbitrary first match.
        private Transform FindPoint(string pointName)
        {
            Transform match = null;
            foreach (Transform candidate in pointCandidates)
            {
                if (candidate.name != pointName || candidate.parent == null
                    || candidate.parent.name != "VFX" || !IsLocalPoint(candidate)) continue;
                if (match != null) return null;
                match = candidate;
            }
            return match;
        }

        // Keep the lifetime usable when a setting is edited in the Inspector.
        private void OnValidate() => MaximumLifetime = maximumLifetime;
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
