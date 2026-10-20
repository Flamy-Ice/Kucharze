using UnityEngine;

namespace ModularMovement
{
    // Calculates vertical speed for one player. Only PlayerMotor moves the player.
    // World Y is vertical: negative speed goes down and positive speed goes up.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayerGravity : MonoBehaviour
    {
        // These Inspector settings control falling and contact with the ground.
        [SerializeField, Tooltip("Downward acceleration in meters per second squared.")]
        private float gravityAcceleration = -25f;
        [SerializeField, Tooltip("Keeps the player close to the ground over small bumps.")]
        private float groundStickVelocity = -2f;
        [SerializeField, Min(0.001f)]
        private float maxFallSpeed = 30f;

        // Properties let other scripts change settings while keeping signs and numbers safe.
        public float GravityAcceleration
        {
            get => gravityAcceleration;
            set => gravityAcceleration = NegativeFinite(value, -25f);
        }

        public float GroundStickVelocity
        {
            get => groundStickVelocity;
            set => groundStickVelocity = NegativeFinite(value, -2f);
        }

        public float MaxFallSpeed
        {
            get => maxFallSpeed;
            set => maxFallSpeed = PositiveFinite(value, 30f);
        }

        // Calculated vertical speed in meters per second; collisions may limit actual movement.
        public float VerticalVelocity { get; private set; }
        // The motor updates ground contact after each completed move.
        public bool IsGrounded { get; private set; }

        // The motor calls this before moving, using the step's elapsed time in seconds.
        internal float CalculateVelocity(float deltaTime)
        {
            // Keep the previous speed if the supplied time cannot be used safely.
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
            {
                return VerticalVelocity;
            }

            if (IsGrounded)
            {
                // A small downward speed keeps contact over bumps without building up fall speed.
                VerticalVelocity = Mathf.Max(groundStickVelocity, -maxFallSpeed);
            }
            else
            {
                // Fall faster over time, but never exceed the configured downward speed limit.
                VerticalVelocity = Mathf.Max(VerticalVelocity + gravityAcceleration * deltaTime, -maxFallSpeed);
            }

            return VerticalVelocity;
        }

        // Use the motor's collision result to prepare vertical speed for the next step.
        internal void ApplyCollision(bool isGrounded, CollisionFlags flags)
        {
            IsGrounded = isGrounded;
            if (IsGrounded)
            {
                // Landing replaces accumulated fall speed with the gentle ground-contact speed.
                VerticalVelocity = Mathf.Max(groundStickVelocity, -maxFallSpeed);
            }
            else if ((flags & CollisionFlags.Above) != 0 && VerticalVelocity > 0f)
            {
                // Stop upward speed at a ceiling. The current walking system does not create upward speed.
                VerticalVelocity = 0f;
            }
        }

        // Forget the previous motion and ground contact so they are not reused after disabling.
        internal void ResetVelocity()
        {
            VerticalVelocity = 0f;
            IsGrounded = false;
        }

        private void Awake()
        {
            // Check saved settings when Unity starts this component.
            ValidateSettings();
        }

        private void OnValidate()
        {
            // Unity calls this when Inspector settings change.
            ValidateSettings();
        }

        private void OnDisable()
        {
            // Turning off gravity must not leave an old fall speed stored.
            ResetVelocity();
        }

        private void ValidateSettings()
        {
            // Use the property checks for values edited directly in the Inspector.
            GravityAcceleration = gravityAcceleration;
            GroundStickVelocity = groundStickVelocity;
            MaxFallSpeed = maxFallSpeed;
        }

        private static float NegativeFinite(float value, float fallback)
        {
            // Keep downward settings negative. Invalid numbers use a safe default.
            // Zero becomes a small negative value, so it does not turn gravity or ground contact off.
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : -Mathf.Max(0.001f, Mathf.Abs(value));
        }

        private static float PositiveFinite(float value, float fallback)
        {
            // The fall-speed limit is a positive amount. Zero becomes a small positive limit.
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0.001f, Mathf.Abs(value));
        }
    }
}
