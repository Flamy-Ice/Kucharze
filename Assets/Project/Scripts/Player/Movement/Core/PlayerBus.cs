using System;
using UnityEngine;

namespace ModularMovement
{
    // Tells other scripts when something changes for this player, such as starting a dash.
    // PlayerStates stores the current state; this bus tells listeners when something changes.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayerBus : MonoBehaviour
    {
        // Other scripts can ask to be told about changes and stop listening when they turn off.
        // Each notification can include details, such as the new movement speed.

        // Reports the movement module's name and actual velocity when horizontal movement starts.
        public event Action<string, Vector3> MovementStarted;
        // Reports the previous module's name and velocity when horizontal movement stops.
        public event Action<string, Vector3> MovementStopped;
        // True means the player is grounded; false means ground contact was lost.
        public event Action<bool> GroundedChanged;
        // Reports the old and new module names when a different movement source takes over.
        public event Action<string, string> MovementSourceChanged;
        // Reports each accepted dash, even when the previous movement source was also Dash.
        public event Action DashStarted;

        // These internal methods let the movement system send notifications after updating state.
        internal void PublishMovementStarted(string sourceId, Vector3 actualVelocity)
        {
            Publish(MovementStarted, sourceId, actualVelocity);
        }

        internal void PublishMovementStopped(string previousSourceId, Vector3 actualVelocity)
        {
            Publish(MovementStopped, previousSourceId, actualVelocity);
        }

        internal void PublishGroundedChanged(bool isGrounded)
        {
            Publish(GroundedChanged, isGrounded);
        }

        internal void PublishMovementSourceChanged(string previousSourceId, string sourceId)
        {
            Publish(MovementSourceChanged, previousSourceId, sourceId);
        }

        internal void PublishDashStarted() => Publish(DashStarted);

        private void Publish(Action listeners)
        {
            if (listeners == null) return;
            foreach (Action listener in listeners.GetInvocationList())
            {
                try { listener(); }
                catch (Exception)
                {
                    // One faulty listener must not stop movement or other listeners.
                }
            }
        }

        // Send the same reading to every script listening for this change.
        private void Publish<T>(Action<T> listeners, T value)
        {
            if (listeners == null) return;

            // Use the current list of listeners, even if one of them stops listening during delivery.
            foreach (Action<T> listener in listeners.GetInvocationList())
            {
                try
                {
                    listener(value);
                }
                catch (Exception)
                {
                    // One faulty listener must not stop movement or other listeners.
                    continue;
                }
            }
        }

        // The same delivery process for events carrying two values, such as a name and velocity.
        private void Publish<TFirst, TSecond>(Action<TFirst, TSecond> listeners, TFirst first, TSecond second)
        {
            if (listeners == null) return;

            foreach (Action<TFirst, TSecond> listener in listeners.GetInvocationList())
            {
                try
                {
                    listener(first, second);
                }
                catch (Exception)
                {
                    // One faulty listener must not stop movement or other listeners.
                    continue;
                }
            }
        }
    }
}
