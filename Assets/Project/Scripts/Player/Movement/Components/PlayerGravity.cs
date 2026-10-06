using UnityEngine;

namespace Player.Movement.Components
{
    // This script's only job is to apply vertical downward gravity to the character in 3D.
    // It queries PlayerGroundDetector to know if the player is grounded,
    // keeping them snapped to slopes when on the ground and accelerating them downwards when in the air.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerGroundDetector))]
    public class PlayerGravity : MonoBehaviour
    {
        [Tooltip("Downward gravity acceleration in m/s^2. Keep negative (e.g. -9.81).")]
        [SerializeField] private float _gravity = -9.81f;

        [Tooltip("Downward push while grounded to stay snapped to slopes and stairs.")]
        [SerializeField] private float _groundedStickForce = -2f;

        [Tooltip("Maximum downward falling speed to prevent tunneling through ground on long falls.")]
        [SerializeField] private float _terminalVelocity = -50f;

        // Automatically found on Awake via GetComponent
        private CharacterController _characterController;
        private PlayerGroundDetector _groundDetector;

        // Current downward falling speed
        private float _verticalVelocity;

        // Public property to read current vertical speed (useful for jump/falling checks)
        public float VerticalVelocity => _verticalVelocity;

        // Public method to override vertical velocity (e.g. called by a PlayerJump component)
        public void SetVerticalVelocity(float velocity)
        {
            _verticalVelocity = velocity;
        }

        // Runs once when the game starts or object spawns
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _groundDetector = GetComponent<PlayerGroundDetector>();
        }

        // Runs when the script is disabled or component turned off
        private void OnDisable()
        {
            _verticalVelocity = 0f;
        }

        // Runs every frame (60+ times per second) to update vertical position
        private void Update()
        {
            ApplyGravity();
        }

        // Calculates gravity and moves the character downwards
        private void ApplyGravity()
        {
            bool isGrounded = _groundDetector != null && _groundDetector.IsGrounded;

            if (isGrounded && _verticalVelocity < 0f)
            {
                // When already on the ground, apply a tiny downward push so the player smoothly walks down slopes
                _verticalVelocity = _groundedStickForce;
            }
            else
            {
                // In the air: accelerate downwards over time, capped at terminal velocity
                _verticalVelocity = Mathf.Max(_verticalVelocity + _gravity * Time.deltaTime, _terminalVelocity);
            }

            // Move the character controller vertically
            Vector3 verticalMotion = new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f);
            CollisionFlags collisionFlags = _characterController.Move(verticalMotion);

            // Cancel upward momentum immediately if the player hits a ceiling
            if ((collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }
        }
    }
}
