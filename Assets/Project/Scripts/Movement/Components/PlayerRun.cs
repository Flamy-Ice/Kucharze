using UnityEngine;
using UnityEngine.Serialization;

namespace ModularMovement
{
    // Hold needs a held button. Toggle switches running on and off with each press.
    public enum RunActivationMode { Hold, Toggle }

    // Offers running movement. Only the motor moves the player.
    [DisallowMultipleComponent]
    [AddComponentMenu("Movement/Player Run")]
    [RequireComponent(typeof(PlayerMovementResolver))]
    public sealed class PlayerRun : MonoBehaviour, IMovementProvider
    {
        [SerializeField] private bool alwaysRun;
        [SerializeField] private RunActivationMode activationMode = RunActivationMode.Hold;
        [SerializeField] private bool alwaysFullRunSpeed;
        [SerializeField, Min(0f)] private float runSpeed = 8f;
        // Keeps older scenes working by moving their saved settings to the shared components.
        // A value of -1 means there is nothing left to move.
        [SerializeField, HideInInspector, FormerlySerializedAs("acceleration")] private float legacyAcceleration = -1f;
        [SerializeField, HideInInspector, FormerlySerializedAs("deceleration")] private float legacyDeceleration = -1f;
        [SerializeField, HideInInspector, FormerlySerializedAs("inputDeadzone")] private float legacyInputDeadzone = -1f;
        // Finds the foundation on this player, without using another player's components.
        private PlayerContext context;
        // Remembers which button presses and input resets have already been handled.
        private uint lastPressCount;
        private uint lastResetVersion;
        // Toggle keeps this choice even after the button or movement direction is released.
        private bool toggledOn;
        private bool observedAlwaysRun;
        private RunActivationMode observedActivationMode;

        // Runs without a button. Stick strength still follows Always Full Run Speed.
        public bool AlwaysRun
        {
            get => alwaysRun;
            set
            {
                if (alwaysRun == value) return;
                alwaysRun = value;
                ResetActivation();
            }
        }

        // Changing the control mode clears the old running choice to avoid an unexpected run.
        public RunActivationMode ActivationMode
        {
            get => activationMode;
            set
            {
                RunActivationMode next = value == RunActivationMode.Toggle ? value : RunActivationMode.Hold;
                if (activationMode == next) return;
                activationMode = next;
                ResetActivation();
            }
        }
        public bool AlwaysFullRunSpeed { get => alwaysFullRunSpeed; set => alwaysFullRunSpeed = value; }
        public float RunSpeed { get => runSpeed; set => runSpeed = NonNegative(value, 8f); }
        public string SourceId => "Run";
        // A higher number wins when two abilities offer movement. Walk uses zero.
        public int Priority => 10;
        // Keep checking while enabled so Toggle can notice the press that turns running off.
        public bool IsEligible => isActiveAndEnabled;
        // This is the running choice, not proof of movement. A wall can still stop the player.
        public bool IsRunRequested { get; private set; }

        // Adding Run also supplies any missing foundation components.
        private void Awake() => PlayerMovementDependencies.EnsureDependencies(gameObject);

        private void OnEnable()
        {
            // Check again when re-enabled, then let the resolver consider this ability.
            PlayerMovementDependencies.EnsureDependencies(gameObject);
            context = GetComponent<PlayerContext>();
            MigrateLegacySettings();
            ResetActivation();
            if (context != null && context.ValidateDependencies())
                context.Resolver.Register(this);
        }

        private void OnDisable()
        {
            // Stop offering running movement without switching off walking or gravity.
            if (context != null && context.Resolver != null)
                context.Resolver.Unregister(this);
            ResetActivation();
        }

        public MovementIntent Evaluate(in MovementStep step)
        {
            // Offer no movement when this ability cannot safely calculate a target.
            if (!IsEligible || context == null || context.Input == null
                || step.DeltaTime <= 0f || float.IsNaN(step.DeltaTime) || float.IsInfinity(step.DeltaTime))
                return MovementIntent.None;

            // All abilities use the input already read for this step; do not read the device again.
            PlayerInputActions input = context.Input;
            // Inspector edits can change activation settings without using the properties above.
            if (observedActivationMode != activationMode || observedAlwaysRun != alwaysRun)
                ResetActivation();
            // Losing focus or blocking input must not restore an old Toggle choice later.
            if (lastResetVersion != input.InputResetVersion)
            {
                toggledOn = false;
                lastPressCount = 0;
                lastResetVersion = input.InputResetVersion;
            }
            // Count only new presses, even if several movement steps happen in one frame.
            uint presses = unchecked(input.RunPressCount - lastPressCount);
            lastPressCount = input.RunPressCount;
            // Two presses cancel each other out; an odd number changes the Toggle choice once.
            if (!alwaysRun && activationMode == RunActivationMode.Toggle && (presses & 1u) != 0)
                toggledOn = !toggledOn;
            // Automatic running changes activation only; zero direction still means no movement.
            IsRunRequested = alwaysRun || (activationMode == RunActivationMode.Toggle ? toggledOn : input.RunHeld);
            if (!IsRunRequested)
                return MovementIntent.None;

            Vector2 direction = step.MoveInput;
            // Full speed ignores stick strength but keeps its direction. Deadzone is handled by input.
            if (alwaysFullRunSpeed && direction.sqrMagnitude > 0f)
                direction /= direction.magnitude;
            // Input up/down becomes world Z. Gravity owns world Y, so this target stays flat.
            Vector3 target = new Vector3(direction.x, 0f, direction.y) * runSpeed;
            // Offer a target only. Shared smoothing sets the pace, and the motor moves the player.
            return new MovementIntent(SourceId, target, Priority);
        }

        private void ResetActivation()
        {
            // Clear the old Toggle choice so changing settings cannot bring it back later.
            observedAlwaysRun = alwaysRun;
            observedActivationMode = activationMode;
            toggledOn = IsRunRequested = false;
            lastPressCount = context != null && context.Input != null ? context.Input.RunPressCount : 0;
            lastResetVersion = context != null && context.Input != null ? context.Input.InputResetVersion : 0;
        }

        internal void MigrateLegacySettings()
        {
            // Preserve older Inspector values without keeping separate running acceleration settings.
            PlayerMovementSettingsMigration.Apply(gameObject, Priority,
                ref legacyAcceleration, ref legacyDeceleration, ref legacyInputDeadzone);
        }

        private void OnValidate()
        {
            // Unity calls this after Inspector edits. Keep saved settings safe to use.
            RunSpeed = runSpeed;

            if (activationMode != RunActivationMode.Toggle) activationMode = RunActivationMode.Hold;
            if (observedActivationMode != activationMode || observedAlwaysRun != alwaysRun)
                ResetActivation();
        }

        // Replace unusable numbers with a default and keep speed at zero or above.
        private static float NonNegative(float value, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}
