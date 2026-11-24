using UnityEngine;

namespace ModularMovement
{
    // Carries one reading of controls without depending on a keyboard or a network package.
    public struct PlayerInputSnapshot
    {
        public Vector2 Move;
        public bool RunHeld;
        public uint RunPressCount;
        public uint DashPressCount;
        public uint ResetVersion;
        public uint DashResetVersion;
        public bool DashAvailable;
    }

    // Lets the input adapter receive controls from another source, such as a remote player.
    public interface IPlayerInputSource
    {
        PlayerInputSnapshot ReadInput();
    }
}
