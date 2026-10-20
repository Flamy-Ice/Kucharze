using UnityEngine;

namespace ModularMovement
{
    // Turns the visual model toward actual movement. It never moves or rotates the player root.
    [DisallowMultipleComponent, AddComponentMenu("Movement/Player Rotation")]
    // Unity adds the shared movement readings this component needs, without adding movement abilities.
    [RequireComponent(typeof(PlayerStates))]
    public sealed class PlayerRotation : MonoBehaviour
    {
        // This is the visual child to turn, not the main object that handles collisions.
        [SerializeField] private Transform modelTransform;
        // Measured in degrees per second. Zero means turn instantly instead of gradually.
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField] private bool finishRotationWhenStopped;
        // Read this player's movement after collisions, rather than guessing from button presses.
        private PlayerStates states;
        // Keep the found model so we do not search all visual children every frame.
        private Transform automaticModel;
        // Remember which model owns an unfinished turn, so a replacement does not inherit it.
        private Transform turningModel;
        // The last movement direction expressed as a turn around the vertical axis.
        private float targetYaw;
        private bool hasPendingTurn;

        // Leave empty to find PlayerModel, or the only child branch containing a renderer.
        public Transform ModelTransform { get => modelTransform; set => modelTransform = value; }
        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = IsFinite(value) ? Mathf.Max(0f, value) : 720f;
        }
        // Turning this off cancels any turn still being completed after movement stopped.
        public bool FinishRotationWhenStopped
        {
            get => finishRotationWhenStopped;
            set
            {
                finishRotationWhenStopped = value;
                if (!value) hasPendingTurn = false;
            }
        }

        // A saved choice takes precedence; otherwise find a safe visual child automatically.
        public Transform ResolvedModel
        {
            get
            {
                if (states == null) states = GetComponent<PlayerStates>();
                if (modelTransform != null) return IsLocalModel(modelTransform) ? modelTransform : null;
                if (automaticModel == null || !IsLocalModel(automaticModel))
                    automaticModel = FindModel();
                return automaticModel;
            }
        }

        private void Reset() => AssignDefaultModelIfMissing();

        // Fill the saved field in the editor, while keeping any model chosen by the developer.
        private void AssignDefaultModelIfMissing()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && modelTransform == null)
                modelTransform = ResolvedModel;
#endif
        }

        private void OnEnable()
        {
            // Start fresh when enabled, without resuming a turn remembered before being disabled.
            states = GetComponent<PlayerStates>();
            automaticModel = null;
            hasPendingTurn = false;
            turningModel = null;
        }

        // Search again if visual children are added, removed or moved to another parent.
        private void OnTransformChildrenChanged() => automaticModel = null;

        // All movement steps finish in Update; LateUpdate reads their latest actual result.
        // Delta time is the elapsed time in seconds, keeping turn speed steady across frame rates.
        private void LateUpdate() => StepRotation(Time.deltaTime);

        internal void StepRotation(float deltaTime)
        {
            // Paused time, disabled components and unusable numbers must not change the model.
            if (!isActiveAndEnabled || !IsFinite(deltaTime) || deltaTime <= 0f)
                return;
            if (states == null) states = GetComponent<PlayerStates>();
            if (states == null)
            {
                hasPendingTurn = false;
                return;
            }
            if (states.IsMoving)
            {
                Vector3 velocity = states.ActualVelocity;
                if (!IsFinite(velocity.x) || !IsFinite(velocity.z)) return;
                // Only actual movement supplies a new direction; stopping keeps this last target.
                // X and Z describe the ground plane. Falling along Y does not choose a new facing.
                targetYaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                hasPendingTurn = true;
            }
            else if (!finishRotationWhenStopped || !hasPendingTurn)
            {
                // Idle players with no unfinished turn still skip all model hierarchy checks.
                hasPendingTurn = false;
                return;
            }
            Transform model = ResolvedModel;
            // Replacing the model must not apply a turn remembered for the old one during a stop.
            if (model == null || (!states.IsMoving && model != turningModel))
            {
                hasPendingTurn = false;
                return;
            }
            turningModel = model;
            Vector3 forward = model.forward;
            // A model pointing straight up or down has no useful ground-facing arrow; use its Y angle.
            float currentYaw = forward.x * forward.x + forward.z * forward.z > 0.000001f
                ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg : model.eulerAngles.y;
            // Zero gives an instant turn. Positive speed takes the shortest route at degrees per second.
            float nextYaw = rotationSpeed == 0f ? targetYaw
                : Mathf.MoveTowardsAngle(currentYaw, targetYaw, rotationSpeed * deltaTime);
            // Rotate around world up only, keeping existing model tilt and local position intact.
            model.rotation = Quaternion.AngleAxis(Mathf.DeltaAngle(currentYaw, nextYaw), Vector3.up) * model.rotation;
            // Treat a tiny angle as finished, so rounding does not keep checking an idle model forever.
            hasPendingTurn = Mathf.Abs(Mathf.DeltaAngle(nextYaw, targetYaw)) > 0.001f;
        }

        private Transform FindModel()
        {
            // Prefer the project's usual model name, even if its artwork is not loaded yet.
            Transform named = transform.Find("PlayerModel");
            if (IsLocalModel(named)) return named;
            Transform candidate = null;
            // Other names work too when exactly one safe child branch contains visible artwork.
            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child = transform.GetChild(index);
                if (!IsLocalModel(child) || child.GetComponentInChildren<Renderer>(true) == null) continue;
                // Several visual branches are ambiguous: let the developer choose explicitly.
                if (candidate != null) return null;
                candidate = child;
            }
            return candidate;
        }

        private bool IsLocalModel(Transform model)
        {
            // Never turn a parent, another player's model, or a branch holding a movement controller.
            return model != null && model != transform && model.IsChildOf(transform)
                && model.GetComponentInParent<PlayerStates>(true) == states
                && model.GetComponentInChildren<CharacterController>(true) == null;
        }

        private void OnValidate()
        {
            // Inspector edits follow the same speed limits and cancellation rule as changes from code.
            RotationSpeed = rotationSpeed;
            if (!finishRotationWhenStopped) hasPendingTurn = false;
            automaticModel = null;
            AssignDefaultModelIfMissing();
        }

        // Reject invalid math results that could otherwise damage the model's rotation.
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
