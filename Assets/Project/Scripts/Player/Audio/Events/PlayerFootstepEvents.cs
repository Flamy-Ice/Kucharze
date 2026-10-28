using System;
using UnityEngine;

namespace ModularMovement
{
    // Tells PlayerAudio when an animation marker says a foot touches the ground.
    [DisallowMultipleComponent, AddComponentMenu("")]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerFootstepEvents : MonoBehaviour
    {
        internal event Action Footstep;

        // Select this function for both left-foot and right-foot Animation Events.
        public void OnFootstep()
        {
            if (Application.isPlaying && isActiveAndEnabled) Footstep?.Invoke();
        }
    }
}
