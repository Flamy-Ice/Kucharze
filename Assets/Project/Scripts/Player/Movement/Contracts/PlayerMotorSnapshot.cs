using UnityEngine;

namespace ModularMovement
{
    // Records the motor state needed to resume a corrected movement step.
    public struct PlayerMotorSnapshot
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float VerticalVelocity;
        public bool Grounded;
        public string Source;
    }
}
