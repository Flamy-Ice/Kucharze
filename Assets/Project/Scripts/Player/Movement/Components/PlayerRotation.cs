using UnityEngine;
using Player.Movement.Core.Events;

namespace Player.Movement.Components
{
    // This script's only job is to rotate the character towards its movement direction in 3D.
    // It listens to PlayerMovedEvent through the PlayerBus and smoothly turns the target transform.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerBus))]
    public class PlayerRotation : MonoBehaviour
    {
        [Tooltip("Rotation speed in degrees per second. Set to 0 or very high for instant turning.")]
        [SerializeField] private float _rotationSpeed = 720f;

        private Transform _targetTransform;
        private PlayerBus _playerBus;
        private Vector3 _targetDirection;

        // Public property to read current facing direction
        public Vector3 TargetDirection => _targetDirection;

        // Public property to access the rotated visual transform
        public Transform TargetTransform => _targetTransform;

        // Allows other systems (e.g. combat, interaction, aiming) to manually steer the rotation
        public void SetTargetDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                _targetDirection = direction.normalized;
            }
        }

        private void Awake()
        {
            _playerBus = GetComponent<PlayerBus>();

            // Automatically find the child model named "PlayerModel", falling back to root if not found
            Transform modelChild = transform.Find("PlayerModel");
            _targetTransform = modelChild != null ? modelChild : transform;

            _targetDirection = _targetTransform.forward;
        }

        private void OnEnable()
        {
            if (_playerBus != null)
            {
                _playerBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
            }
        }

        private void OnDisable()
        {
            if (_playerBus != null)
            {
                _playerBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
            }
        }

        private void Update()
        {
            RotateTowardsTarget();
        }

        private void OnPlayerMoved(PlayerMovedEvent eventData)
        {
            Vector3 horizontalVelocity = new Vector3(eventData.Velocity.x, 0f, eventData.Velocity.z);

            // Only update rotation target if there is noticeable horizontal movement
            if (horizontalVelocity.sqrMagnitude > 0.01f)
            {
                _targetDirection = horizontalVelocity.normalized;
            }
        }

        private void RotateTowardsTarget()
        {
            if (_targetDirection.sqrMagnitude < 0.001f || _targetTransform == null)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(_targetDirection, Vector3.up);

            if (_rotationSpeed <= 0f)
            {
                _targetTransform.rotation = targetRotation;
            }
            else
            {
                _targetTransform.rotation = Quaternion.RotateTowards(
                    _targetTransform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }
    }
}
