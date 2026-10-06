namespace Player.Movement.Core.Events
{
    // A message sent whenever the player touches the ground or leaves the ground.
    // Useful for animations (landing vs falling) and audio (landing thud).
    public readonly struct PlayerGroundedEvent
    {
        // True if the player is standing on the ground, false if falling or jumping
        public readonly bool IsGrounded;

        public PlayerGroundedEvent(bool isGrounded)
        {
            IsGrounded = isGrounded;
        }
    }
}
