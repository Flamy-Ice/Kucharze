using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Movement.Core.Events
{
    // Think of this script as a private radio station on the player.
    // Any script on the player can tune in (Subscribe), stop listening (Unsubscribe),
    // or broadcast a message (Publish) so other scripts can hear it without talking directly to each other.
    [DisallowMultipleComponent]
    public class PlayerBus : MonoBehaviour
    {
        // A list of all listeners organized by the type of message they want to hear.
        private readonly Dictionary<Type, Delegate> _subscribers = new();

        // Tune in to hear a specific type of message.
        // Example: "Call my method every time someone sends a MovementInputEvent."
        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            // Do nothing if no method was provided
            if (handler == null) return;

            var eventType = typeof(T);

            // If other scripts are already listening to this message type, add this new listener to the list.
            if (_subscribers.TryGetValue(eventType, out var existingDelegate))
            {
                _subscribers[eventType] = Delegate.Combine(existingDelegate, handler);
            }
            else
            {
                // Otherwise, start a brand new list of listeners for this message type.
                _subscribers[eventType] = handler;
            }
        }

        // Stop listening to this message type (important to avoid errors when a script turns off).
        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;

            var eventType = typeof(T);

            // Find the list of listeners for this message type and remove this specific method from it.
            if (_subscribers.TryGetValue(eventType, out var existingDelegate))
            {
                var updatedDelegate = Delegate.Remove(existingDelegate, handler);

                // If nobody is listening anymore, remove the entry completely to keep things tidy.
                if (updatedDelegate == null)
                {
                    _subscribers.Remove(eventType);
                }
                else
                {
                    _subscribers[eventType] = updatedDelegate;
                }
            }
        }

        // Broadcast a message to every script currently listening for this type of event.
        public void Publish<T>(T eventData) where T : struct
        {
            var eventType = typeof(T);

            // Look up who is listening to this message and call their methods with the new data.
            if (_subscribers.TryGetValue(eventType, out var existingDelegate) && existingDelegate is Action<T> action)
            {
                action.Invoke(eventData);
            }
        }

        // Runs automatically when the player object is deleted from the game.
        private void OnDestroy()
        {
            // Clear out all listeners to make sure no memory is wasted.
            _subscribers.Clear();
        }
    }
}
