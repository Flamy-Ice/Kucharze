using System.Runtime.CompilerServices;

// Let the custom inspectors share runtime helpers without exposing them as public API.
[assembly: InternalsVisibleTo("PlayerMovementSystem3D.Editor")]
