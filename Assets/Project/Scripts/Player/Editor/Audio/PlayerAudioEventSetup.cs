using UnityEditor;
using UnityEngine;

namespace ModularMovement.Editor
{
    // Adds the footstep-marker receiver to the model, with support for Undo and saved player changes.
    internal static class PlayerAudioEventSetup
    {
        internal static string EnsureReceiver(PlayerAudio audio, bool provision)
        {
            Animator animator = audio.ResolvedFootstepAnimator;
            if (animator == null)
                return "Select a local Model Animator in Player Animation, or add one unambiguous Animator under this player.";
            if (!Application.isPlaying && animator.GetComponent<PlayerFootstepEvents>() == null)
            {
                if (EditorUtility.IsPersistent(animator) || !animator.gameObject.scene.IsValid()
                    || !animator.gameObject.scene.isLoaded || (animator.hideFlags & HideFlags.NotEditable) != 0
                    || (animator.gameObject.hideFlags & HideFlags.NotEditable) != 0)
                    return "Open an editable player instance or Prefab Mode to add the footstep event receiver.";
                if (provision) Undo.AddComponent<PlayerFootstepEvents>(animator.gameObject);
            }
            return animator.GetComponent<PlayerFootstepEvents>() == null
                ? "Reopen the Player Audio Inspector to prepare OnFootstep. The receiver is also added when Play Mode starts."
                : null;
        }
    }
}
