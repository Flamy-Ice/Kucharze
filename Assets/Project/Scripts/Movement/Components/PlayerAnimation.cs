using UnityEngine;

namespace ModularMovement
{
    // Chooses the model's animation from completed movement. Only PlayerMotor moves the player.
    // A component is a script attached to an object; allow only one copy of this one per player.
    [DisallowMultipleComponent, AddComponentMenu("Movement/Player Animation")]
    // Order 100 runs after the resolver's usual order of 0, before Unity updates the Animator.
    [DefaultExecutionOrder(100)]
    // Unity adds the movement readings and event source this view needs on the same object.
    [RequireComponent(typeof(PlayerStates), typeof(PlayerBus))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        // These names must match the fields called parameters inside the Animator Controller.
        public const string StateParameterName = "MovementState";
        public const string DashProgressParameterName = "DashProgress";
        public const string RunSpeedParameterName = "RunPlaybackSpeed";
        // The controller uses these numbers to choose standing, moving or dashing.
        public const int IdleState = 0;
        public const int RunState = 1;
        public const int DashState = 2;
        // Convert names to numeric IDs once, so they do not need converting every frame.
        private static readonly int StateParameter = Animator.StringToHash(StateParameterName);
        private static readonly int DashProgressParameter = Animator.StringToHash(DashProgressParameterName);
        private static readonly int RunSpeedParameter = Animator.StringToHash(RunSpeedParameterName);

        // The Animator plays clips on the visual model. This choice is saved in the Inspector.
        [SerializeField] private Animator modelAnimator;
        // This asset contains animation states and the rules for changing between them.
        [SerializeField] private RuntimeAnimatorController defaultController;
        // Readings and notifications belong to this player, so players do not share their movement.
        private PlayerStates states;
        private PlayerBus bus;
        // Keep the automatically found model, instead of searching for it from scratch each time.
        private Animator automaticAnimator;
        // Remember which Animator and controller were checked for compatible parameters.
        private Animator boundAnimator;
        private RuntimeAnimatorController boundController;
        private bool hasStateParameter;
        private bool hasDashProgressParameter;
        private bool hasRunSpeedParameter;
        // These abilities are optional. Read their progress or speeds without changing their rules.
        private PlayerDash dash;
        private PlayerRun run;
        private PlayerWalk walk;

        // An explicit choice is kept, but another player's Animator is never accepted.
        public Animator ModelAnimator { get => modelAnimator; set => modelAnimator = value; }
        public RuntimeAnimatorController DefaultController { get => defaultController; set => defaultController = value; }
        // Other scripts can read the chosen animation number, but cannot overwrite it here.
        public int CurrentState { get; private set; }

        // Use the saved choice, or find the only safe Animator under this player.
        public Animator ResolvedAnimator
        {
            get
            {
                if (states == null) states = GetComponent<PlayerStates>();
                if (modelAnimator != null) return IsLocalAnimator(modelAnimator) ? modelAnimator : null;
                if (automaticAnimator != null && IsLocalAnimator(automaticAnimator)) return automaticAnimator;
                Animator candidate = null;
                // Include inactive models, so they can be found before they are shown.
                foreach (Animator animator in GetComponentsInChildren<Animator>(true))
                {
                    if (!IsLocalAnimator(animator)) continue;
                    // Several Animators need an explicit choice instead of an arbitrary first match.
                    if (candidate != null) return null;
                    candidate = animator;
                }
                automaticAnimator = candidate;
                return candidate;
            }
        }

        // Unity calls this when the component is added or Reset is chosen in its menu.
        private void Reset() => AssignEditorDefaults();

        // Start listening when this component is turned on, including at the start of the game.
        private void OnEnable()
        {
            states = GetComponent<PlayerStates>();
            bus = GetComponent<PlayerBus>();
            automaticAnimator = null;
            boundAnimator = null;
            // Events tell us when movement starts, stops or switches to another ability.
            if (bus != null)
            {
                bus.MovementStarted += OnMovementChanged;
                bus.MovementStopped += OnMovementChanged;
                bus.MovementSourceChanged += OnSourceChanged;
            }
            SynchronizeAnimation();
        }

        // Stop listening when turned off, so this view cannot react while disabled.
        private void OnDisable()
        {
            if (bus != null)
            {
                bus.MovementStarted -= OnMovementChanged;
                bus.MovementStopped -= OnMovementChanged;
                bus.MovementSourceChanged -= OnSourceChanged;
            }
            // Clear our last signal without disabling the Animator or changing locomotion.
            CurrentState = IdleState;
            if (boundAnimator != null && IsLocalAnimator(boundAnimator) && boundAnimator.isActiveAndEnabled
                && boundController != null && boundAnimator.runtimeAnimatorController == boundController && hasStateParameter)
                boundAnimator.SetInteger(StateParameter, IdleState);
            boundAnimator = null;
            boundController = null;
            hasStateParameter = false;
            hasDashProgressParameter = false;
            hasRunSpeedParameter = false;
        }

        // Update runs once per frame, meaning once for each new image of the game.
        // Progress changes on every dash step, even when no start, stop or source event is sent.
        private void Update() => SynchronizeAnimation();
        // Event arguments describe the change; read the complete current state from PlayerStates.
        private void OnMovementChanged(string source, Vector3 velocity) => SynchronizeAnimation();
        private void OnSourceChanged(string previousSource, string source) => SynchronizeAnimation();
        // Check again after Update in case another script changed the model or controller.
        private void LateUpdate() => SynchronizeAnimation();
        // Forget an automatic choice when direct children are added, removed or moved.
        private void OnTransformChildrenChanged() => automaticAnimator = null;

        // Update the visual presentation without changing input, speed settings or the player's position.
        internal void SynchronizeAnimation()
        {
            if (!isActiveAndEnabled) return;
            if (states == null) states = GetComponent<PlayerStates>();
            // Dash has priority. Otherwise, show Run while moving and Idle while standing still.
            // The selected source includes the final partial dash step, even after its timer expires.
            CurrentState = states == null ? IdleState : states.ActiveMovementSource == "Dash"
                ? DashState : states.IsMoving ? RunState : IdleState;
            // An unfinished model setup must not stop the rest of the movement system.
            Animator animator = ResolvedAnimator;
            if (animator == null) return;
            // Keep an existing controller; use the default only when the model has none.
            if (animator.runtimeAnimatorController == null && defaultController != null)
                animator.runtimeAnimatorController = defaultController;
            // Root motion lets a clip move its object. Turn it off because the motor owns movement.
            if (animator.applyRootMotion) animator.applyRootMotion = false;
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            // Recheck parameter names and types when a different Animator or controller is selected.
            if (boundAnimator != animator || boundController != controller)
            {
                boundAnimator = animator;
                boundController = controller;
                hasStateParameter = HasStateParameter(animator);
                hasDashProgressParameter = HasParameter(animator, DashProgressParameter, AnimatorControllerParameterType.Float);
                hasRunSpeedParameter = HasParameter(animator, RunSpeedParameter, AnimatorControllerParameterType.Float);
            }
            // Missing assets, disabled visuals and incompatible controllers fail without Console logs.
            if (controller == null || !animator.isActiveAndEnabled || !hasStateParameter) return;
            if (hasDashProgressParameter)
            {
                if (dash == null) dash = GetComponent<PlayerDash>();
                // Progress 0 shows the clip's beginning; 1 shows its end, whatever the dash duration.
                animator.SetFloat(DashProgressParameter, CurrentState == DashState && dash != null ? dash.DashProgress : 0f);
            }
            // A multiplier of 1 plays Run normally; 0.5 plays it at half speed.
            // Only the Run state uses this value, so Idle and Dash keep their own timing.
            if (hasRunSpeedParameter)
            {
                float rate = CurrentState == RunState ? GetRunPlaybackRate() : 1f;
                animator.SetFloat(RunSpeedParameter, IsFinite(rate) ? Mathf.Max(0f, rate) : 1f);
            }
            // The controller's transitions use this number to choose which animation to show.
            animator.SetInteger(StateParameter, CurrentState);
        }

        // Compare actual speed after collisions with the speed set on a local movement ability.
        private float GetRunPlaybackRate()
        {
            if (run == null) run = GetComponent<PlayerRun>();
            if (walk == null) walk = GetComponent<PlayerWalk>();
            // Prefer Run's speed for the shared clip, so slower walking also has a slower animation.
            // If Run is unavailable, use Walk's speed instead.
            float referenceSpeed = run != null && run.isActiveAndEnabled && IsPositiveFinite(run.RunSpeed)
                ? run.RunSpeed
                : walk != null && walk.isActiveAndEnabled && IsPositiveFinite(walk.WalkSpeed) ? walk.WalkSpeed : 0f;
            // Custom movement can still use this view without adding Walk or Run.
            return referenceSpeed > 0f ? states.PlanarSpeed / referenceSpeed : 1f;
        }

        // Reject the player root, other players and branches that contain a movement controller.
        private bool IsLocalAnimator(Animator animator)
        {
            return animator != null && animator.transform != transform && animator.transform.IsChildOf(transform)
                && animator.GetComponentInParent<PlayerStates>(true) == states
                && animator.GetComponentInChildren<CharacterController>(true) == null;
        }

        internal static bool HasStateParameter(Animator animator)
            => HasParameter(animator, StateParameter, AnimatorControllerParameterType.Int);

        // Check both the name and value type before sending a number to the controller.
        private static bool HasParameter(Animator animator, int hash, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == hash && parameter.type == type)
                    return true;
            return false;
        }

        // Unity calls this in the Editor when the script loads or an Inspector value changes.
        private void OnValidate()
        {
            automaticAnimator = null;
            boundAnimator = null;
            AssignEditorDefaults();
        }

        // Fill missing references during setup, while keeping choices already made by the developer.
        internal void AssignEditorDefaults()
        {
            // This block runs only in the Unity Editor and is left out of a built game.
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            if (modelAnimator == null) modelAnimator = ResolvedAnimator;
            if (defaultController != null) return;
            // Use this script's current folder, so moving the whole package keeps setup working.
            string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(this));
            defaultController = FindDefaultController(scriptPath);
#endif
        }

#if UNITY_EDITOR
        // Search only known relative locations, so another player's controller is never chosen by name.
        internal static RuntimeAnimatorController FindDefaultController(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath)) return null;
            string componentsFolder = System.IO.Path.GetDirectoryName(scriptPath);
            if (string.IsNullOrEmpty(componentsFolder)) return null;
            string movementFolder = System.IO.Path.GetDirectoryName(componentsFolder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(movementFolder)) return null;
            string scriptsFolder = System.IO.Path.GetDirectoryName(movementFolder);
            string projectFolder = string.IsNullOrEmpty(scriptsFolder) ? null
                : System.IO.Path.GetDirectoryName(scriptsFolder)?.Replace('\\', '/');
            RuntimeAnimatorController controller = string.IsNullOrEmpty(projectFolder) ? null
                : UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    projectFolder + "/Animations/Animators/Player.controller");
            // Older exported copies may still keep their controller inside the Movement folder.
            return controller != null ? controller
                : UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    movementFolder + "/Animation/PlayerMovement.controller");
        }
#endif

        // Reject zero, negative speeds and invalid numbers before dividing by a speed.
        private static bool IsPositiveFinite(float value) => IsFinite(value) && value > 0f;
        // NaN means "not a number"; infinity is also not a usable animation value.
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
