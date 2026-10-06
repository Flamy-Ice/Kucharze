using UnityEngine;
using UnityEngine.InputSystem;
using Player.Movement.Core.Events;

namespace Player.Movement.Components
{
    // This script's only job is to read movement input from an assigned InputActionReference
    // and broadcast that direction through the PlayerBus radio station.
    // It doesn't move the player itself - it just reports the input received.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerBus))]
    public class PlayerInputActions : MonoBehaviour
    {
        [Tooltip("Select or drag your Move action here from your Input Actions asset.")]
        [SerializeField] private InputActionReference _moveAction;

        [Tooltip("Optional sprint/run action. Dispatches SprintInputEvent to PlayerBus.")]
        [SerializeField] private InputActionReference _sprintAction;

        private PlayerBus _playerBus;
        private bool _wasSprinting;

        private void Awake()
        {
            _playerBus = GetComponent<PlayerBus>();
        }

        private void OnEnable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Enable();
            }
            else
            {
                Debug.LogWarning("PlayerInputActions: 'Move Action' reference is missing!", this);
            }

            if (_sprintAction != null && _sprintAction.action != null)
            {
                _sprintAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Disable();
            }

            if (_sprintAction != null && _sprintAction.action != null)
            {
                _sprintAction.action.Disable();
            }

            if (_wasSprinting && _playerBus != null)
            {
                _playerBus.Publish(new SprintInputEvent(false));
            }

            _wasSprinting = false;
        }

        private void Update()
        {
            if (_playerBus == null) return;

            // 1. Read input from the assigned Move action
            Vector2 input = Vector2.zero;
            if (_moveAction != null && _moveAction.action != null)
            {
                input = _moveAction.action.ReadValue<Vector2>();
            }

            _playerBus.Publish(new MovementInputEvent(input));

            // 2. Read sprint input and dispatch event on state change
            if (_sprintAction != null && _sprintAction.action != null)
            {
                bool isSprinting = _sprintAction.action.IsPressed();
                if (isSprinting != _wasSprinting)
                {
                    _wasSprinting = isSprinting;
                    _playerBus.Publish(new SprintInputEvent(isSprinting));
                }
            }
        }
    }
}
