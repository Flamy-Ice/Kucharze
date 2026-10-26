using UnityEngine;

namespace ModularMovement
{
    // Offers a short, straight burst. The motor still handles collisions and the actual move.
    [DisallowMultipleComponent, AddComponentMenu("Movement/Player Dash")]
    [RequireComponent(typeof(PlayerMovementResolver))]
    public sealed class PlayerDash : MonoBehaviour, ITimedMovementProvider
    {
        // The curve scales this top speed; the player does not keep it for the whole burst.
        [SerializeField, Min(0f)] private float dashSpeed = 18f;
        // Burst time and recovery time are separate, both measured in seconds.
        [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
        [SerializeField, Min(0f)] private float cooldown = 0.8f;
        [SerializeField] private AnimationCurve speedCurve = CreateDefaultSpeedCurve();
        // Keeps all references local, so each player uses only their own movement components.
        private PlayerContext context;
        // Remember where to dash after the stick is released. Before any input, use world forward.
        private Vector3 lastDirection = Vector3.forward;
        // Save the chosen direction so new input cannot turn a burst already in progress.
        private Vector3 dashDirection;
        private float remainingDuration;
        // Keep the starting duration so editing the setting cannot skip ahead in the active curve.
        private float activeDuration;
        // Save this step's slice so the resolver can finish it with the next movement offer.
        private float stepStart;
        private float stepEnd;
        private float stepDuration;
        // Remember handled presses and input resets, so old input cannot start a new burst.
        private uint lastPressCount;
        private uint lastResetVersion;

        public float DashSpeed { get => dashSpeed; set => dashSpeed = NonNegative(value, 18f); }
        public float DashDuration { get => dashDuration; set => dashDuration = Mathf.Max(0.01f, NonNegative(value, 0.15f)); }
        public float Cooldown { get => cooldown; set => cooldown = NonNegative(value, 0.8f); }
        // Time runs from left to right; height controls the fraction of Dash Speed.
        public AnimationCurve SpeedCurve
        {
            get => speedCurve;
            // Copy a supplied curve so editing one player's profile cannot change another's.
            set => speedCurve = value == null ? CreateDefaultSpeedCurve()
                : new AnimationCurve(value.keys) { preWrapMode = value.preWrapMode, postWrapMode = value.postWrapMode };
        }
        // These readings describe this ability. A wall may still stop the player's actual movement.
        public float CooldownRemaining { get; private set; }
        public bool IsDashing => remainingDuration > 0f;
        // Keeps the final step at one until another dash starts or this ability is reset.
        public float DashProgress => activeDuration > 0f
            ? Mathf.Clamp01(1f - remainingDuration / activeDuration) : 0f;
        public string SourceId => "Dash";
        // Dash outranks normal walking and running; the resolver needs no ability names.
        public int Priority => 100;
        // Keep checking while waiting, because this also advances cooldown and remembers direction.
        public bool IsEligible => isActiveAndEnabled;

        // Adding Dash supplies the shared movement foundation, without adding Walk or Run.
        private void Awake() => PlayerMovementDependencies.EnsureDependencies(gameObject);

        private void OnEnable()
        {
            // Check again after re-enabling, then offer this ability to the movement selector.
            PlayerMovementDependencies.EnsureDependencies(gameObject);
            context = GetComponent<PlayerContext>();
            ResetDash();
            if (context != null && context.ValidateDependencies())
                context.Resolver.Register(this);
        }

        private void OnDisable()
        {
            // Removing this ability must leave walking, running and gravity working.
            if (context != null && context.Resolver != null)
                context.Resolver.Unregister(this);
            ResetDash();
        }

        public MovementIntent Evaluate(in MovementStep step)
        {
            stepDuration = 0f;
            // The resolver calls this once per movement step. Unsafe steps offer no movement.
            if (!IsEligible || context == null || context.Input == null || step.DeltaTime <= 0f
                || float.IsNaN(step.DeltaTime) || float.IsInfinity(step.DeltaTime))
                return MovementIntent.None;

            // Use the shared input reading instead of checking the keyboard or gamepad again.
            PlayerInputActions input = context.Input;
            if (lastResetVersion != input.DashInputResetVersion)
            {
                // Losing controls cancels the burst, but keeps its recovery time.
                if (IsDashing) CooldownRemaining = cooldown;
                remainingDuration = 0f;
                lastPressCount = 0;
                lastResetVersion = input.DashInputResetVersion;
            }
            bool pressed = input.DashPressCount != lastPressCount;
            lastPressCount = input.DashPressCount;
            // A press during recovery is discarded, even if recovery ends within this step.
            bool ready = CooldownRemaining <= 0f;
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - step.DeltaTime);

            // Remember accepted directional input, even while waiting for another dash.
            if (step.MoveInput.sqrMagnitude > 0f)
                lastDirection = new Vector3(step.MoveInput.x, 0f, step.MoveInput.y).normalized;

            if (!IsDashing && ready && pressed && input.DashInputAvailable && dashSpeed > 0f)
            {
                dashDirection = lastDirection;
                remainingDuration = dashDuration;
                activeDuration = dashDuration;
            }
            if (!IsDashing)
                return MovementIntent.None;

            // Keep direction fixed. Use only the remaining burst time if this step is longer.
            float usedTime = Mathf.Min(step.DeltaTime, remainingDuration);
            // Curve time runs from 0 at the start to 1 at the end, regardless of burst duration.
            float start = Mathf.Clamp01((activeDuration - remainingDuration) / activeDuration);
            float end = Mathf.Clamp01(start + usedTime / activeDuration);
            stepStart = start;
            stepEnd = end;
            stepDuration = usedTime;
            // Average the curve over this step so a low frame rate does not change burst distance.
            float weightedTime = activeDuration * IntegrateSpeedCurve(start, end);
            remainingDuration = Mathf.Max(0f, remainingDuration - usedTime);
            // Tiny rounding leftovers must not keep the burst alive for an extra step.
            if (remainingDuration < 0.000001f) remainingDuration = 0f;
            // Any time left in this step after the burst already counts toward recovery.
            if (!IsDashing)
                CooldownRemaining = Mathf.Max(0f, cooldown - (step.DeltaTime - usedTime));
            // Spread a partial burst over the whole step so the motor does not move too far.
            Vector3 velocity = dashDirection * (dashSpeed * (weightedTime / step.DeltaTime));
            // Offer movement only. Skip ordinary acceleration because the curve sets the burst pace.
            return new MovementIntent(SourceId, velocity, Priority, useSharedSmoothing: false);
        }

        public TimedMovement GetTimedMovement(in MovementIntent continuation)
        {
            if (stepDuration <= 0f || dashSpeed <= 0f)
                return new TimedMovement(Vector3.zero, Vector3.zero, 0f);
            // Do not slow below the requested ordinary movement while a direction is held.
            // The next offer supplies its own speed; Dash does not need to know Walk or Run.
            float minimumMultiplier = continuation.IsValid
                ? Mathf.Clamp01(continuation.PlanarVelocity.magnitude / dashSpeed) : 0f;
            float area = IntegrateSpeedCurve(stepStart, stepEnd, minimumMultiplier);
            Vector3 average = dashDirection * (dashSpeed * activeDuration * area / stepDuration);
            Vector3 exit = dashDirection * (dashSpeed * Mathf.Max(minimumMultiplier, SafeCurveValue(stepEnd)));
            return new TimedMovement(average, exit, stepDuration);
        }

        private void ResetDash()
        {
            // Re-enabling starts fresh, including recovery. Earlier button presses are not replayed.
            remainingDuration = CooldownRemaining = 0f;
            activeDuration = 0f;
            stepDuration = 0f;
            lastDirection = Vector3.forward;
            lastPressCount = context != null && context.Input != null ? context.Input.DashPressCount : 0;
            lastResetVersion = context != null && context.Input != null ? context.Input.DashInputResetVersion : 0;
        }

        private void OnValidate()
        {
            // Inspector edits must leave usable numbers and a curve the ability can read.
            DashSpeed = dashSpeed;
            DashDuration = dashDuration;
            Cooldown = cooldown;
            if (speedCurve == null || speedCurve.length == 0)
                speedCurve = CreateDefaultSpeedCurve();
        }

        internal static AnimationCurve CreateDefaultSpeedCurve()
        {
            // Reach the peak quickly, then ease back toward normal movement without a full stop.
            return new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1f),
                new Keyframe(0.55f, 1f), new Keyframe(1f, 0.3f));
        }

        // The Inspector reuses this calculation to preview distance without moving the player.
        internal float IntegrateSpeedCurve(float start, float end, float minimumMultiplier = 0f)
        {
            // Without a curve, use full speed throughout the requested part of the burst.
            if (speedCurve == null || speedCurve.length == 0) return end - start;
            // Use the same small time slices at every frame rate, including partial slices.
            const int slices = 128;
            float area = 0f;
            int first = Mathf.Clamp(Mathf.FloorToInt(start * slices), 0, slices - 1);
            int last = Mathf.Clamp(Mathf.CeilToInt(end * slices), 0, slices);
            for (int index = first; index < last; index++)
            {
                float left = (float)index / slices;
                float right = (float)(index + 1) / slices;
                float from = Mathf.Max(start, left);
                float to = Mathf.Min(end, right);
                float leftValue = Mathf.Max(minimumMultiplier, SafeCurveValue(left));
                float rightValue = Mathf.Max(minimumMultiplier, SafeCurveValue(right));
                // Include only the part of each small slice that belongs to this movement step.
                float fromValue = Mathf.Lerp(leftValue, rightValue, (from - left) * slices);
                float toValue = Mathf.Lerp(leftValue, rightValue, (to - left) * slices);
                // Adding slice width times average height estimates the area under the speed curve.
                area += (fromValue + toValue) * 0.5f * Mathf.Max(0f, to - from);
            }
            return area;
        }

        private float SafeCurveValue(float time)
        {
            if (speedCurve == null || speedCurve.length == 0) return 1f;
            float value = speedCurve.Evaluate(time);
            // A curve cannot reverse the dash or exceed Dash Speed.
            return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        }

        // Replace unusable numbers with a safe default and prevent negative speeds or times.
        private static float NonNegative(float value, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}
