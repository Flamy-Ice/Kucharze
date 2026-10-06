using UnityEngine;

namespace Player.Movement.Core.Events
{
    // A message sent right after the player finishes moving.
    // Other scripts (like animations or footsteps audio) can listen to this to know what the player is doing.
    public readonly struct PlayerMovedEvent
    {
        // How fast and in which 3D direction the player actually moved.
        public readonly Vector3 Velocity;

        // True if the player is standing on the ground, false if falling or jumping.
        public readonly bool IsGrounded;

        // Creates the message with the current velocity and ground status.
        public PlayerMovedEvent(Vector3 velocity, bool isGrounded)
        {
            Velocity = velocity;
            IsGrounded = isGrounded;
        }
    }
}
