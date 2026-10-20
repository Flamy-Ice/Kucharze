using UnityEngine;

namespace ModularMovement
{
    // A snapshot of input and player state supplied by the resolver for one movement step.
    // Every movement module receives the same snapshot, so they work with the same information.
    // 'readonly' keeps these values unchanged after the snapshot is created.
    public readonly struct MovementStep
    {
        // Time for this movement step, in seconds. Used to make speed changes independent of frame rate.
        public float DeltaTime { get; }
        // Directional input: X is left/right and Y is forward/backward on the world XZ plane.
        // Its length is at most one; a smaller value allows slower walking with an analog stick.
        public Vector2 MoveInput { get; }
        // Actual direction and speed from the last completed move, in meters per second.
        // This includes vertical movement and the effects of collisions.
        public Vector3 ActualVelocity { get; }
        // True means the last completed move detected ground beneath the player.
        public bool IsGrounded { get; }

        // Stores the supplied information. This snapshot does not read input or move the player.
        public MovementStep(float deltaTime, Vector2 moveInput, Vector3 actualVelocity, bool isGrounded)
        {
            DeltaTime = deltaTime;
            MoveInput = moveInput;
            ActualVelocity = actualVelocity;
            IsGrounded = isGrounded;
        }
    }
}
