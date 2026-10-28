using UnityEngine;

namespace ModularMovement
{
    // The distance, speed and collisions measured after PlayerMotor moves the player.
    // 'readonly' keeps the recorded values unchanged after this report is created.
    public readonly struct MotorResult
    {
        // How far the player actually moved in each world direction, in meters.
        public Vector3 Displacement { get; }
        // Actual direction and speed in meters per second, including vertical movement.
        // A wall can make this lower than the speed requested by a movement module.
        public Vector3 ActualVelocity { get; }
        // True means the controller detected ground beneath the player after moving.
        public bool IsGrounded { get; }
        // Records contacts below, above or to the sides. Several contacts can occur together.
        public CollisionFlags CollisionFlags { get; }

        // Stores the results supplied by the motor. This report does not move the player.
        public MotorResult(Vector3 displacement, Vector3 actualVelocity, bool isGrounded, CollisionFlags collisionFlags)
        {
            Displacement = displacement;
            ActualVelocity = actualVelocity;
            IsGrounded = isGrounded;
            CollisionFlags = collisionFlags;
        }
    }
}
