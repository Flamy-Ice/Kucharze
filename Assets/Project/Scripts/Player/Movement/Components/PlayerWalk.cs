using UnityEngine;
using UnityEngine.Serialization;

namespace ModularMovement
{
    // Calculates how the player wants to walk. PlayerMotor performs the actual movement.
    [DisallowMultipleComponent]
    [AddComponentMenu("Movement/Player Walk")]
    [RequireComponent(typeof(PlayerMovementResolver))]
    public sealed class PlayerWalk : MonoBehaviour, IMovementProvider
    {
        // These settings control walking. They can be changed in the Inspector.
        [SerializeField, Min(0f)] private float walkSpeed = 5f;
        [SerializeField, Tooltip("Uses Walk Speed for any input above Input Deadzone. Acceleration and braking still apply.")]
        private bool alwaysFullWalkSpeed = false;
        // Copy settings from older players into the components that now use them.
        [SerializeField, HideInInspector, FormerlySerializedAs("acceleration")] private float legacyAcceleration = -1f;
        [SerializeField, HideInInspector, FormerlySerializedAs("deceleration")] private float legacyDeceleration = -1f;
        [SerializeField, HideInInspector, FormerlySerializedAs("inputDeadzone")] private float legacyInputDeadzone = -1f;
        private PlayerContext context;

        // Check values supplied by other scripts before accepting them.
        public float WalkSpeed { get => walkSpeed; set => walkSpeed = NonNegative(value, 5f); }
        public bool AlwaysFullWalkSpeed { get => alwaysFullWalkSpeed; set => alwaysFullWalkSpeed = value; }
        public string SourceId => "Walk";
        // Other movement modules can take over by using a higher priority.
        public int Priority => 0;
        public bool IsEligible => isActiveAndEnabled;

        private void Awake()
        {
            // Unity calls this when the player starts. Add any missing movement components.
            PlayerMovementDependencies.EnsureDependencies(gameObject);
        }

        private void OnEnable()
        {
            PlayerMovementDependencies.EnsureDependencies(gameObject);
            context = GetComponent<PlayerContext>();
            MigrateLegacySettings();
            if (context.ValidateDependencies())
                // Register means the resolver can ask this module for a walking direction and speed.
                context.Resolver.Register(this);
        }

        private void OnDisable()
        {
            // Stop offering walking movement when this component is turned off.
            if (context != null && context.Resolver != null)
                context.Resolver.Unregister(this);
        }

        public MovementIntent Evaluate(in MovementStep step)
        {
            if (!IsEligible)
                return MovementIntent.None;
            // PlayerInputActions has already ignored tiny, accidental stick movements.
            Vector2 direction = step.MoveInput;
            if (alwaysFullWalkSpeed && direction.sqrMagnitude > 0f)
                direction /= direction.magnitude;
            Vector3 target = new Vector3(direction.x, 0f, direction.y) * walkSpeed;
            return new MovementIntent(SourceId, target, Priority);
        }

        internal void MigrateLegacySettings()
        {
            PlayerMovementSettingsMigration.Apply(gameObject, Priority,
                ref legacyAcceleration, ref legacyDeceleration, ref legacyInputDeadzone);
        }

        private void OnValidate()
        {
            // Unity calls this when Inspector values change. Check them before they are used.
            WalkSpeed = walkSpeed;
        }

        private static float NonNegative(float value, float fallback)
        {
            // Replace invalid numbers with a safe default and keep negative settings at zero.
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
        }
    }
}
