using UnityEngine;

namespace Player.Movement.Core.Events
{
    // A simple letter/package that holds the player's movement input direction.
    // 'readonly struct' means this data cannot be accidentally changed and does not slow down the game's memory.
    public readonly struct MovementInputEvent
    {
        // Stores horizontal (X) and vertical (Y) input, like pressing W, S, A, D or moving an analog stick.
        public readonly Vector2 Direction;

        // Creates the package and saves the direction inside it.
        public MovementInputEvent(Vector2 direction)
        {
            Direction = direction;
        }
    }
}
