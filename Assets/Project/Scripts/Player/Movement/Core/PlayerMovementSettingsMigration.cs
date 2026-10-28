using UnityEngine;

namespace ModularMovement
{
    // Copies older acceleration and input settings to the components that now use them.
    internal static class PlayerMovementSettingsMigration
    {
        internal static void Apply(GameObject player, int priority,
            ref float acceleration, ref float deceleration, ref float deadzone)
        {
            // Negative values mean no old setting is waiting to be copied.
            if (acceleration < 0f && deceleration < 0f && deadzone < 0f) return;
            PlayerHorizontalMovement horizontal = player.GetComponent<PlayerHorizontalMovement>();
            PlayerInputActions input = player.GetComponent<PlayerInputActions>();
            if (horizontal == null || input == null) return;
            horizontal.ApplyLegacySettings(priority, acceleration, deceleration);
            input.ApplyLegacyDeadzone(priority, deadzone);
            // Mark these values as used so re-enabling an ability cannot overwrite newer settings.
            acceleration = deceleration = deadzone = -1f;
        }
    }
}
