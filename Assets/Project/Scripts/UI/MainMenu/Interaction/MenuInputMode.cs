using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace ModularMovement
{
    // Distinguishes mouse interaction from deliberate keyboard or controller navigation.
    public static class MenuInputMode
    {
        public static bool UsingNavigation { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => UsingNavigation = false;

        public static void UsePointer() => UsingNavigation = false;

        public static bool ReadNavigationInput()
        {
            Mouse mouse = Mouse.current;
            if (MenuPlatform.IsMobile || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0f
                || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)))
            {
                UsePointer();
                return false;
            }
            var module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
            var action = module != null && module.move != null ? module.move.action : null;
            bool navigating = action != null && action.enabled && action.ReadValue<Vector2>().sqrMagnitude > 0.01f;
            if (navigating) UsingNavigation = true;
            return navigating;
        }
    }
}
