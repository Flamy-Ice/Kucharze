using System;
using UnityEngine;

namespace ModularMovement
{
    // Shares footstep animation markers with this player's audio and VFX.
    [DisallowMultipleComponent, AddComponentMenu("")]
    // Unity sends clip markers to the Animator's own object, so the receiver belongs beside it.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerFootstepEvents : MonoBehaviour
    {
        internal event Action Footstep;

        // Place an OnFootstep marker where each foot touches the ground in the clip.
        // Audio and VFX both listen here, so the clip needs only one marker per contact.
        public void OnFootstep()
        {
            // Ignore animation previews in the Editor and a disabled receiver.
            if (Application.isPlaying && isActiveAndEnabled) Footstep?.Invoke();
        }
    }
}
