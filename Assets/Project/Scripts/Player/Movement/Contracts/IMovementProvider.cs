namespace ModularMovement
{
    // The common information an ability must supply to request movement along the ground.
    // The resolver chooses an offer. Only PlayerMotor actually moves the player.
    public interface IMovementProvider
    {
        // A unique name that identifies which module offered the movement.
        string SourceId { get; }
        // Higher numbers take precedence when several modules offer movement.
        int Priority { get; }
        // True means this module is currently allowed to offer movement.
        bool IsEligible { get; }

        // Uses this step's input and elapsed time to calculate a proposed direction and speed.
        // Read the shared controls and previous movement without changing them.
        MovementIntent Evaluate(in MovementStep step);
    }
}
