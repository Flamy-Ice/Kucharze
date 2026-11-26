using UnityEngine;

namespace ModularMovement
{
    // Uses the simulated device in the editor and the real device in a built game.
    public static class MenuPlatform
    {
        public static bool IsMobile => UnityEngine.Device.Application.isMobilePlatform;
        public static bool SupportsDisplayOptions => !IsMobile && (Application.isEditor
            || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer
            || Application.platform == RuntimePlatform.OSXPlayer);
    }
}
