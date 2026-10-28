namespace ModularMovement
{
    // Lets the chosen ability react after PlayerMotor has carried out its movement.
    public interface IMovementSelectionListener
    {
        // Runs after every movement step that uses this ability, not only when the ability starts.
        void OnMovementSelected();
    }
}
