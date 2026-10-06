using UnityEngine;
using Player.Movement.Core.Events;

namespace Player.Movement.Components
{
    // This script's only job is to manage the running/sprinting state.
    // It listens to SprintInputEvent from PlayerBus and modifies PlayerWalk's SpeedMultiplier.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerBus))]
    [RequireComponent(typeof(PlayerWalk))]
    public class PlayerRun : MonoBehaviour
    {
        [Tooltip("Speed multiplier applied to PlayerWalk while sprinting (e.g. 1.6 = 60% faster).")]
        [SerializeField] private float _sprintMultiplier = 1.6f;

        [Tooltip("If true, the character always runs whenever moving, ignoring sprint input.")]
        [SerializeField] private bool _alwaysRun = false;

        [Tooltip("If true, pressing sprint toggles running on/off. If false, sprint button must be held down.")]
        [SerializeField] private bool _toggleSprint = false;

        [Tooltip("If true, sprinting automatically cancels when the player stops moving.")]
        [SerializeField] private bool _cancelWhenStopped = true;

        private PlayerBus _playerBus;
        private PlayerWalk _playerWalk;

        private bool _isSprinting;
        private bool _sprintInputHeld;

        // Public readouts for animation controllers and other gameplay mechanics
        public bool IsSprinting => _isSprinting;
        public float SprintMultiplier => _sprintMultiplier;
        public bool AlwaysRun
        {
            get => _alwaysRun;
            set => _alwaysRun = value;
        }

        private void Awake()
        {
            _playerBus = GetComponent<PlayerBus>();
            _playerWalk = GetComponent<PlayerWalk>();
        }

        private void OnEnable()
        {
            if (_playerBus != null)
            {
                _playerBus.Subscribe<SprintInputEvent>(OnSprintInput);
                _playerBus.Subscribe<MovementInputEvent>(OnMovementInput);
            }
        }

        private void OnDisable()
        {
            if (_playerBus != null)
            {
                _playerBus.Unsubscribe<SprintInputEvent>(OnSprintInput);
                _playerBus.Unsubscribe<MovementInputEvent>(OnMovementInput);
            }

            _sprintInputHeld = false;
            SetSprinting(false);
        }

        private void OnSprintInput(SprintInputEvent eventData)
        {
            if (_alwaysRun)
            {
                return;
            }

            _sprintInputHeld = eventData.IsSprinting;

            if (_toggleSprint)
            {
                // When toggled, pressing the button flips the sprint state
                if (_sprintInputHeld)
                {
                    SetSprinting(!_isSprinting);
                }
            }
            else
            {
                // When hold mode, sprint is active as long as the button is held
                SetSprinting(_sprintInputHeld);
            }
        }

        private void OnMovementInput(MovementInputEvent eventData)
        {
            bool hasMovement = eventData.Direction.sqrMagnitude >= 0.01f;

            if (_alwaysRun)
            {
                SetSprinting(hasMovement);
                return;
            }

            if (hasMovement)
            {
                // Resume or activate sprint if sprint button was already held down when movement started
                if (!_toggleSprint && _sprintInputHeld && !_isSprinting)
                {
                    SetSprinting(true);
                }
            }
            else if (_cancelWhenStopped && _isSprinting)
            {
                // Automatically cancel sprint when player stops moving
                SetSprinting(false);
            }
        }

        private void SetSprinting(bool sprinting)
        {
            if (_isSprinting == sprinting)
            {
                return;
            }

            _isSprinting = sprinting;

            if (_playerWalk != null)
            {
                _playerWalk.SpeedMultiplier = _isSprinting ? _sprintMultiplier : 1f;
            }

            if (_playerBus != null)
            {
                _playerBus.Publish(new PlayerRunningEvent(_isSprinting));
            }
        }
    }
}
