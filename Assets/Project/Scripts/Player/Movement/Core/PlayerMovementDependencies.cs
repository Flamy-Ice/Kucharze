using UnityEngine;

namespace ModularMovement
{
    // A helper called by movement modules. It is not a component attached to the player.
    // Adds missing basic movement components, without choosing optional abilities such as walking.
    public static class PlayerMovementDependencies
    {
        // Keep existing components and add only the missing ones on this player's GameObject.
        public static void EnsureDependencies(GameObject player)
        {
            // A GameObject must be supplied before any components can be checked or added.
            if (player == null)
                throw new System.ArgumentNullException(nameof(player));

            // These components handle collisions, events, state, input and movement execution.
            GetOrAdd<CharacterController>(player);
            GetOrAdd<PlayerBus>(player);
            GetOrAdd<PlayerStates>(player);
            GetOrAdd<PlayerGravity>(player);
            GetOrAdd<PlayerHorizontalMovement>(player);
            GetOrAdd<PlayerContext>(player);
            GetOrAdd<PlayerInputActions>(player);
            GetOrAdd<PlayerMotor>(player);
            GetOrAdd<PlayerMovementResolver>(player);
            // Tell PlayerContext to look again after new components have been added.
            player.GetComponent<PlayerContext>().RefreshReferences();
        }

        // Use the same check for each required component.
        private static void GetOrAdd<T>(GameObject player) where T : Component
        {
            // TryGetComponent checks for an existing component, preventing duplicates.
            if (!player.TryGetComponent<T>(out _))
                player.AddComponent<T>();
        }
    }
}
