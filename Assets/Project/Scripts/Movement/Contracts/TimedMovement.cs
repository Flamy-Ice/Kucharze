using UnityEngine;

namespace ModularMovement
{
    // A small, read-only package describing the part of a frame used by a short movement.
    // It carries information only; the resolver and motor decide how to use it.
    public readonly struct TimedMovement
    {
        // Direction and average speed in m/s. Multiplying by Duration gives the distance to move.
        public Vector3 AverageVelocity { get; }
        // Speed at the end can differ from the average. Braking starts from this speed.
        public Vector3 ExitVelocity { get; }
        // Used time in seconds. Any time left in the frame can continue ordinary movement.
        public float Duration { get; }

        // Keep these three readings together so they always describe the same piece of movement.
        public TimedMovement(Vector3 averageVelocity, Vector3 exitVelocity, float duration)
        {
            AverageVelocity = averageVelocity;
            ExitVelocity = exitVelocity;
            Duration = duration;
        }
    }
}
