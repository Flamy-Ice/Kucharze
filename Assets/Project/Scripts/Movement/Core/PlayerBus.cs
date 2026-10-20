using System;
using UnityEngine;

namespace ModularMovement
{
    // Sends movement notifications for one player. Each player has its own bus.
    // PlayerStates stores the current state; this bus tells listeners when something changes.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayerBus : MonoBehaviour
    {
        // Other scripts listen with += and stop listening with -= when disabled or destroyed.
        // An Action describes the data passed to each listening method.

        // Reports the movement module's name and actual velocity when horizontal movement starts.
        public event Action<string, Vector3> MovementStarted;
        // Reports the previous module's name and velocity when horizontal movement stops.
        public event Action<string, Vector3> MovementStopped;
        // True means the player is grounded; false means ground contact was lost.
        public event Action<bool> GroundedChanged;
        // Reports the old and new module names when a different movement source takes over.
        public event Action<string, string> MovementSourceChanged;

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

        // Sends one value to every listening method. T stands for the value's type, such as bool.
        private void Publish<T>(Action<T> listeners, T value)
        {
            if (listeners == null) return;

            // Take a snapshot of listeners, so changing subscriptions does not alter this loop.
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
