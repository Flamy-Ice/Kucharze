using System;
using UnityEngine;

namespace ModularMovement
{
    // Stores readable movement state for one player. It does not decide how the player moves.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayerStates : MonoBehaviour
    {
        // Private setters let other scripts read these values without overwriting them directly.
        // Ground contact is measured after the motor completes a move.
        public bool IsGrounded { get; private set; }
        // Based on actual horizontal speed, not on whether a movement key is held.
        public bool IsMoving { get; private set; }
        // Actual direction and speed after collisions, including vertical movement, in meters per second.
        public Vector3 ActualVelocity { get; private set; }
        // Horizontal speed on the XZ ground plane, in meters per second.
        public float PlanarSpeed { get; private set; }
        // The selected module's name. A selected module can still request zero velocity.
        public string ActiveMovementSource { get; private set; } = "None";

        // The resolver records its choice. Only the resolver on this player may write it.
        internal void SetActiveMovementSource(PlayerMovementResolver owner, string sourceId)
        {
            if (owner == null || owner != GetComponent<PlayerMovementResolver>())
            {
                throw new InvalidOperationException("Only the local PlayerMovementResolver can update the movement source.");
            }

            // Use None when no source name was supplied.
            ActiveMovementSource = string.IsNullOrWhiteSpace(sourceId) ? "None" : sourceId;
        }

        // The motor supplies the completed move. 'in' makes the supplied report read-only here.
        internal void ApplyMotorResult(PlayerMotor owner, in MotorResult result, float movingSpeedThreshold)
        {
            // Reject writes from another player's motor or an unrelated caller.
            if (owner == null || owner != GetComponent<PlayerMotor>())
            {
                throw new InvalidOperationException("Only the local PlayerMotor can update movement state.");
            }

            ActualVelocity = result.ActualVelocity;
            // Ignore vertical speed when deciding whether the player is moving along the ground.
            PlanarSpeed = new Vector2(ActualVelocity.x, ActualVelocity.z).magnitude;
            // Tiny horizontal movements below or equal to the motor's threshold count as stopped.
            IsMoving = PlanarSpeed > movingSpeedThreshold;
            IsGrounded = result.IsGrounded;
        }
    }
}
