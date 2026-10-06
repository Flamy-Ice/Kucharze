using UnityEngine;
using Player.Movement.Core.Events;

namespace Player.Movement.Components
{
    // Determines coordinate frame used to translate 2D inputs into 3D world movement
    public enum MovementSpace
    {
        World,          // Aligned with world axes (standard for top-down / isometric games like Overcooked)
        CameraRelative, // Aligned with the camera view plane (standard for third-person games)
        PlayerRelative  // Aligned with the player's facing direction (standard for first-person / tank controls)
    }

    // This script's only job is to move the character horizontally in 3D using Unity's CharacterController.
    // It listens to the PlayerBus for direction instructions and applies smooth horizontal planar movement.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerBus))]
    public class PlayerWalk : MonoBehaviour
    {
        [Tooltip("Coordinate frame used to interpret movement inputs:\n- World: Aligned with world axes (Overcooked / top-down).\n- CameraRelative: Aligned with camera view plane.\n- PlayerRelative: Aligned with the player's facing direction (FPP).")]
        [SerializeField] private MovementSpace _movementSpace = MovementSpace.World;

        [Tooltip("Optional camera transform used when MovementSpace is CameraRelative. Defaults to Camera.main if unassigned.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Maximum movement speed in meters per second.")]
        [SerializeField] private float _walkSpeed = 5f;

        [Tooltip("Acceleration rate in m/s^2. Set high (50+) for instant response.")]
        [SerializeField] private float _acceleration = 25f;

        [Tooltip("Deceleration rate in m/s^2 when stopping. Set high (50+) for instant stop.")]
        [SerializeField] private float _deceleration = 30f;

        private CharacterController _characterController;
        private PlayerBus _playerBus;
        private PlayerGroundDetector _groundDetector;

        private Vector2 _inputDirection;
        private Vector3 _currentVelocity;
        private float _speedMultiplier = 1f;

        // Public property to read or modify movement speed multiplier (used by PlayerRun, buffs, carry weights)
        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Max(0f, value);
        }

        // Public readouts for animations and diagnostics
        public float CurrentSpeed => _currentVelocity.magnitude;
        public Vector3 CurrentVelocity => _currentVelocity;
        public float WalkSpeed => _walkSpeed;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerBus = GetComponent<PlayerBus>();
            _groundDetector = GetComponent<PlayerGroundDetector>();
        }

        private void OnEnable()
        {
            if (_playerBus != null)
            {
                _playerBus.Subscribe<MovementInputEvent>(OnMovementInput);
            }
        }

        private void OnDisable()
        {
            if (_playerBus != null)
            {
                _playerBus.Unsubscribe<MovementInputEvent>(OnMovementInput);
            }

            _inputDirection = Vector2.zero;
            _currentVelocity = Vector3.zero;
            _speedMultiplier = 1f;
        }

        private void Update()
        {
            Move();
        }

        private void OnMovementInput(MovementInputEvent eventData)
        {
            _inputDirection = eventData.Direction;
        }

        private void Move()
        {
            if (_characterController == null || !_characterController.enabled)
            {
                return;
            }

            Vector3 targetDirection = CalculateMoveDirection();

            if (targetDirection.sqrMagnitude > 1f)
            {
                targetDirection.Normalize();
            }

            Vector3 targetVelocity = targetDirection * (_walkSpeed * _speedMultiplier);
            float rate = targetVelocity.sqrMagnitude > 0.001f ? _acceleration : _deceleration;

            if (rate <= 0f)
            {
                _currentVelocity = targetVelocity;
            }
            else
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);
            }

            Vector3 motion = _currentVelocity * Time.deltaTime;
            _characterController.Move(motion);

            bool isGrounded = _groundDetector != null ? _groundDetector.IsGrounded : _characterController.isGrounded;
            _playerBus.Publish(new PlayerMovedEvent(_characterController.velocity, isGrounded));
        }

        private Vector3 CalculateMoveDirection()
        {
            switch (_movementSpace)
            {
                case MovementSpace.CameraRelative:
                    Transform cam = _cameraTransform != null ? _cameraTransform : (Camera.main != null ? Camera.main.transform : null);
                    if (cam != null)
                    {
                        Vector3 forward = cam.forward;
                        forward.y = 0f;
                        forward.Normalize();

                        Vector3 right = cam.right;
                        right.y = 0f;
                        right.Normalize();

                        return (right * _inputDirection.x) + (forward * _inputDirection.y);
                    }
                    return new Vector3(_inputDirection.x, 0f, _inputDirection.y);

                case MovementSpace.PlayerRelative:
                    return (transform.right * _inputDirection.x) + (transform.forward * _inputDirection.y);

                case MovementSpace.World:
                default:
                    return new Vector3(_inputDirection.x, 0f, _inputDirection.y);
            }
        }
    }
}
