namespace ModularMovement
{
    // Lets a short action, such as a dash, report how much of the movement step it used.
    // Any time left after the action can be used for ordinary movement.
    public interface ITimedMovementProvider : IMovementProvider
    {
        // The resolver supplies the next offer, such as walking after a burst.
        // Return the burst's speed and used time. The motor will perform the combined movement.
        // The next ability's movement request stays unchanged.
        TimedMovement GetTimedMovement(in MovementIntent continuation);
    }
}
