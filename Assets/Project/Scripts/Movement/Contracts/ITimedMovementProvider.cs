namespace ModularMovement
{
    // A contract lists what a movement module must provide. It does not move the player itself.
    // This version lets a short movement give unused time in the frame to the next movement offer.
    public interface ITimedMovementProvider : IMovementProvider
    {
        // The resolver supplies the next offer, such as walking after a burst.
        // Return the burst's speed and used time. The motor will perform the combined movement.
        // 'in' lets this method read the offer without replacing it.
        TimedMovement GetTimedMovement(in MovementIntent continuation);
    }
}
