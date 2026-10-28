using UnityEngine;

namespace ModularMovement
{
    // Helps this player's scripts find the other components attached to the same player.
    // Each player has its own context. It does not decide or perform movement.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayerContext : MonoBehaviour
    {
        // Remember found components so they do not need to be searched for on every access.
        private CharacterController characterController;
        private PlayerBus bus;
        private PlayerStates states;
        private PlayerInputActions input;
        private PlayerMovementResolver resolver;
        private PlayerMotor motor;
        private PlayerGravity gravity;
        private PlayerHorizontalMovement horizontalMovement;

        // Other scripts read these properties to get this player's components without manual wiring.
        public CharacterController CharacterController => ResolveLocal(ref characterController);
        public PlayerBus Bus => ResolveLocal(ref bus);
        public PlayerStates States => ResolveLocal(ref states);
        public PlayerInputActions Input => ResolveLocal(ref input);
        public PlayerMovementResolver Resolver => ResolveLocal(ref resolver);
        public PlayerMotor Motor => ResolveLocal(ref motor);
        public PlayerGravity Gravity => ResolveLocal(ref gravity);
        public PlayerHorizontalMovement HorizontalMovement => ResolveLocal(ref horizontalMovement);

        // Search again after components have been added, removed or replaced.
        public void RefreshReferences()
        {
            characterController = GetComponent<CharacterController>();
            bus = GetComponent<PlayerBus>();
            states = GetComponent<PlayerStates>();
            input = GetComponent<PlayerInputActions>();
            resolver = GetComponent<PlayerMovementResolver>();
            motor = GetComponent<PlayerMotor>();
            gravity = GetComponent<PlayerGravity>();
            horizontalMovement = GetComponent<PlayerHorizontalMovement>();
        }

        // Check that the required components exist. This does not add or enable them.
        public bool ValidateDependencies()
        {
            return CharacterController != null && Bus != null && States != null &&
                Input != null && Resolver != null && Motor != null && Gravity != null && HorizontalMovement != null;
        }

        // Reuse a component already found; look on this player again if it was removed.
        private T ResolveLocal<T>(ref T reference) where T : Component
        {
            if (reference == null)
            {
                reference = GetComponent<T>();
            }

            return reference;
        }
    }
}
