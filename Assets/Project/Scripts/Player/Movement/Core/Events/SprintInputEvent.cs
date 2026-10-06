namespace Player.Movement.Core.Events
{
    // Strongly-typed struct event dispatched when the sprint button is pressed or released.
    public readonly struct SprintInputEvent
    {
        public readonly bool IsSprinting;

        public SprintInputEvent(bool isSprinting)
        {
            IsSprinting = isSprinting;
        }
    }

    // Dispatched when the player starts or stops running/sprinting.
    // Useful for animators (e.g. blend tree triggers), audio (faster footsteps), and VFX.
    public readonly struct PlayerRunningEvent
    {
        public readonly bool IsRunning;

        public PlayerRunningEvent(bool isRunning)
        {
            IsRunning = isRunning;
        }
    }
}
