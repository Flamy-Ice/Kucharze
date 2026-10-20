using UnityEngine;

namespace ModularMovement
{
    // Performs the player's movement and reports what actually happened after collisions.
    // Other movement modules propose velocity; only this component moves the controller.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [RequireComponent(typeof(CharacterController), typeof(PlayerStates), typeof(PlayerGravity))]
    [RequireComponent(typeof(PlayerContext), typeof(PlayerBus))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Counts horizontal speed above this value as moving. Does not change walking speed.")]
        [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.05f;
        private PlayerContext context;

        // Keep the detection threshold nonnegative, including when changed by another script.
        public float MovingSpeedThreshold
        {
            get => movingSpeedThreshold;
            set => movingSpeedThreshold = IsFinite(value) ? Mathf.Max(0f, value) : 0.05f;
        }

        // The last completed move, including actual displacement, velocity and ground contact.
        public MotorResult LastResult { get; private set; }

        // The resolver checks that required components are available before requesting a move.
        internal bool CanMove()
        {
            if (context == null)
                context = GetComponent<PlayerContext>();

            // The controller, motor and gravity must be enabled to perform a complete movement step.
            return isActiveAndEnabled && context != null &&
                context.CharacterController != null && context.CharacterController.enabled &&
                context.States != null && context.Bus != null &&
                context.Gravity != null && context.Gravity.isActiveAndEnabled;

        }

        // The resolver supplies horizontal velocity and a positive, valid elapsed time in seconds.
        internal void Move(Vector3 planarVelocity, float deltaTime, string previousSource)
        {
            PlayerStates states = context.States;
            PlayerGravity gravity = context.Gravity;
            CharacterController controller = context.CharacterController;
            // Remember the old state so notifications are sent only when something changes.
            bool wasMoving = states.IsMoving;
            bool wasGrounded = states.IsGrounded;
            Vector3 previousVelocity = states.ActualVelocity;
            // Gravity supplies vertical speed. Combine it with the selected horizontal movement.
            float verticalVelocity = gravity.CalculateVelocity(deltaTime);
            Vector3 velocity = new Vector3(planarVelocity.x, verticalVelocity, planarVelocity.z);
            Vector3 startPosition = transform.position;

            // The controller needs a distance, so convert meters per second using the step's time.
            // One move combines walking and gravity, so the player is not moved twice.
            CollisionFlags flags = controller.Move(velocity * deltaTime);
            // Measure the result after collisions instead of assuming the requested move succeeded.
            Vector3 displacement = transform.position - startPosition;
            bool grounded = (flags & CollisionFlags.Below) != 0 || controller.isGrounded;
            LastResult = new MotorResult(displacement, displacement / deltaTime, grounded, flags);
            // Prepare gravity for the next step, then update state before notifying other scripts.
            gravity.ApplyCollision(grounded, flags);
            MotorResult result = LastResult;
            states.ApplyMotorResult(this, in result, movingSpeedThreshold);

            // Starting and stopping refer to actual horizontal movement, not pressed keys.
            if (wasMoving != states.IsMoving)
            {
                if (states.IsMoving)
                    context.Bus.PublishMovementStarted(states.ActiveMovementSource, states.ActualVelocity);
                else
                    context.Bus.PublishMovementStopped(previousSource, previousVelocity);
            }

            // Report ground contact only when it is gained or lost.
            if (wasGrounded != grounded)
                context.Bus.PublishGroundedChanged(grounded);

        }


        private void OnValidate()
        {
            // Unity calls this when Inspector values change. Keep the threshold safe.
            MovingSpeedThreshold = movingSpeedThreshold;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}


