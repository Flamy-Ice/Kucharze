using UnityEngine;
using Player.Movement.Core.Events;

namespace Player.Movement.Components
{
    // This script's only job is to detect whether the player is standing on solid ground.
    // It broadcasts a PlayerGroundedEvent through the PlayerBus whenever landing or becoming airborne,
    // and draws a helpful colored sphere in the Scene view so you can visually verify detection.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerBus))]
    public class PlayerGroundDetector : MonoBehaviour
    {
        [Header("Ground Detection")]
        // Which layers count as solid walkable ground (e.g. Default, Ground)
        [SerializeField] private LayerMask _groundLayers = 1;

        // Radius of the ground checking sphere
        [SerializeField] private float _detectionRadius = 0.28f;

        // Local offset relative to the player's position for the sphere check
        [SerializeField] private Vector3 _detectionOffset = new Vector3(0f, -0.9f, 0f);

        // Cached non-allocating buffer for sphere overlap checks
        private readonly Collider[] _hitBuffer = new Collider[4];

        // Reference to the player's radio station, found automatically on Awake
        private PlayerBus _playerBus;

        // Current grounded state
        private bool _isGrounded;

        // Public property so other scripts can query ground status directly
        public bool IsGrounded => _isGrounded;

        // Automatically set up default offset and radius when component is added
        private void Reset()
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                // Place check at the very bottom of the CharacterController capsule
                _detectionOffset = new Vector3(0f, cc.center.y - (cc.height * 0.5f) + (cc.radius * 0.5f), 0f);
                _detectionRadius = cc.radius * 0.9f;
            }
        }

        // Runs once when the game starts or object spawns
        private void Awake()
        {
            _playerBus = GetComponent<PlayerBus>();
        }

        // Runs every frame (60+ times per second) to check ground status
        private void Update()
        {
            CheckGround();
        }

        // Performs a non-allocating sphere check beneath the player's feet
        private void CheckGround()
        {
            // Transform local offset to world position so rotation is taken into account
            Vector3 checkPosition = transform.TransformPoint(_detectionOffset);

            // Check for colliders on ground layers, ignoring trigger colliders
            int hitCount = Physics.OverlapSphereNonAlloc(
                checkPosition,
                _detectionRadius,
                _hitBuffer,
                _groundLayers,
                QueryTriggerInteraction.Ignore
            );

            // Determine if any solid collider that does NOT belong to the player or its children was hit
            bool groundedNow = false;
            for (int i = 0; i < hitCount; i++)
            {
                if (!_hitBuffer[i].transform.IsChildOf(transform))
                {
                    groundedNow = true;
                    break;
                }
            }

            // If grounded status changed since last check, notify the entire system
            if (groundedNow != _isGrounded)
            {
                _isGrounded = groundedNow;
                if (_playerBus != null)
                {
                    _playerBus.Publish(new PlayerGroundedEvent(_isGrounded));
                }
            }
        }

        // Draws a colored wire sphere in the Unity Scene view when the player is selected
        private void OnDrawGizmosSelected()
        {
            Vector3 checkPosition = transform.TransformPoint(_detectionOffset);
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkPosition, _detectionRadius);
        }
    }
}
