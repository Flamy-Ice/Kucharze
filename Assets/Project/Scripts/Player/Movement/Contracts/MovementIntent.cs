using UnityEngine;

namespace ModularMovement
{
    // An ability's requested direction and speed along the ground, before the player is moved.
    // 'readonly' keeps the proposal unchanged after it is created.
    public readonly struct MovementIntent
    {
        // Means this module offers no movement. It does not compete with other proposals.
        public static MovementIntent None => new MovementIntent("None", Vector3.zero, int.MinValue, false);

        // Identifies the module making the proposal, such as "Walk".
        public string SourceId { get; }
        // Target direction and speed in meters per second on the world XZ plane.
        // Shared horizontal movement applies acceleration after the resolver chooses a target.
        // Y must be zero because gravity is calculated separately.
        public Vector3 PlanarVelocity { get; }
        // Higher numbers take precedence when several modules offer movement.
        public int Priority { get; }
        // False means there is no proposal to consider. The resolver also checks the data.
        // An ability can also request a stop by asking for zero speed.
        public bool IsValid { get; }
        // Short actions such as dash can skip gradual acceleration and use their requested speed.
        public bool UseSharedSmoothing { get; }

        // Stores a proposal without moving the player. It is valid by default.
        public MovementIntent(string sourceId, Vector3 planarVelocity, int priority, bool isValid = true,
            bool useSharedSmoothing = true)
        {
            SourceId = sourceId;
            PlanarVelocity = planarVelocity;
            Priority = priority;
            IsValid = isValid;
            UseSharedSmoothing = useSharedSmoothing;
        }
    }
}
