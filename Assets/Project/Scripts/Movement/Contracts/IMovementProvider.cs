namespace ModularMovement
{
    // A shared set of rules for modules that offer horizontal movement, such as walking.
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
        // The 'in' keyword passes the step as read-only data, so this method cannot change it.
        MovementIntent Evaluate(in MovementStep step);
    }
}
