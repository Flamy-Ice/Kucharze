using System.Collections.Generic;
using UnityEngine;

namespace ModularMovement
{
    // Connects VFX to the model's existing footstep markers, even without PlayerAudio.
    internal sealed class PlayerVFXFootsteps
    {
        private readonly PlayerVFX owner;
        private readonly List<Animator> candidates = new List<Animator>();
        private PlayerFootstepEvents receiver;
        private Animator receiverAnimator;
        // Remember a foot contact until this frame's movement is finished.
        private bool pending;

        internal PlayerVFXFootsteps(PlayerVFX owner) => this.owner = owner;

        // Use the chosen model, or the player's only Animator when no model adapter is present.
        internal Animator ResolveAnimator()
        {
            PlayerStates states = owner.GetComponent<PlayerStates>();
            if (states == null) return null;
            PlayerAnimation animation = owner.GetComponent<PlayerAnimation>();
            if (animation != null)
            {
                Animator selected = animation.ResolvedAnimator;
                return IsLocal(selected, states) ? selected : null;
            }
            owner.GetComponentsInChildren(true, candidates);
            Animator match = null;
            foreach (Animator animator in candidates)
            {
                if (!IsLocal(animator, states)) continue;
                // With several models, guessing could read the wrong animation.
                if (match != null) { match = null; break; }
                match = animator;
            }
            candidates.Clear();
            return match;
        }

        // A nested player keeps its own animation markers.
        private static bool IsLocal(Animator animator, PlayerStates states)
            => animator != null && animator.GetComponentInParent<PlayerStates>(true) == states;

        // Reuse the model's marker receiver; Audio can listen to the same receiver too.
        internal void Refresh()
        {
            if (!Application.isPlaying || !owner.isActiveAndEnabled) { Unbind(); return; }
            Animator animator = ResolveAnimator();
            if (receiverAnimator == animator && (animator == null || receiver != null)) return;
            // A model change must not replay a marker received from the old model.
            Unbind();
            receiverAnimator = animator;
            if (animator == null) return;
            receiver = animator.GetComponent<PlayerFootstepEvents>();
            if (receiver == null) receiver = animator.gameObject.AddComponent<PlayerFootstepEvents>();
            // Ask to be told when the animation reports a foot contact.
            receiver.Footstep += Queue;
        }

        // Several markers in one frame count as one step, just like Audio.
        private void Queue()
        {
            if (owner.isActiveAndEnabled && receiver != null && receiver.isActiveAndEnabled
                && receiverAnimator == ResolveAnimator()) pending = true;
        }

        // Take the saved marker once, then forget it so the effect cannot repeat next frame.
        internal bool Consume()
        {
            bool result = pending;
            pending = false;
            return result;
        }

        // A dash discards a waiting step so the two effects do not start together.
        internal void Discard() => pending = false;

        // Remove only our listener; Audio may still need this receiver.
        internal void Unbind()
        {
            if (receiver != null) receiver.Footstep -= Queue;
            receiver = null;
            receiverAnimator = null;
            pending = false;
        }
    }
}
