using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ModularMovement
{
    // Reads controls for one player and provides a movement vector. It does not move the player.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayerInputActions : MonoBehaviour
    {
        private const string DefaultActionMapName = "Player";
        private const string DefaultMoveActionName = "Move";

        [Tooltip("Defines input actions and control bindings for the player's movement abilities.")]
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField, Range(0f, 1f)] private float inputDeadzone = 0.1f;
        [SerializeField, HideInInspector] private int legacyDeadzonePriority = int.MaxValue;
        public float InputDeadzone
        {
            get => inputDeadzone;
            set => inputDeadzone = float.IsNaN(value) || float.IsInfinity(value) ? 0.1f : Mathf.Clamp01(value);
        }

        // Each player uses a private copy, so enabling input does not affect the shared asset.
        private InputActionAsset runtimeActions;
        private InputActionAsset initializedAsset;
        private InputActionMap moveMap;
        private InputAction moveAction;
        private InputAction runAction;
        private InputAction dashAction;
        private uint pendingDashPressCount;
        private uint pendingRunPressCount;
        private InputDevice[] assignedDevices;
        // Input is allowed only when this switch, window focus and application pause all permit it.
        private bool inputEnabled = true;
        private bool hasFocus = true;
        private bool isPaused;
        // Remember when settings need checking and when a previous setup attempt failed.
        private bool configurationDirty = true;
        private bool initializationFailed;

        // The latest reading: X is left/right and Y is forward/backward input.
        // PlayerWalk maps these values onto the ground; this component does not choose world directions.
        public Vector2 MoveInput { get; private set; }
        // These readings belong to the same movement step as MoveInput.
        public bool RunHeld { get; private set; }
        public uint RunPressCount { get; private set; }
        // Reset numbers tell abilities when earlier button presses must be forgotten.
        public uint InputResetVersion { get; private set; }
        public uint DashPressCount { get; private set; }
        public uint DashInputResetVersion { get; private set; }
        public bool DashInputAvailable { get; private set; }
        public bool HasMoveInput => MoveInput.sqrMagnitude > 0f;
        public InputActionAsset RuntimeActions => runtimeActions;
        public bool InputEnabled => inputEnabled;
        // A local reader can use the same device selection without changing it.
        public ReadOnlyArray<InputDevice>? AssignedDevices => assignedDevices == null
            ? (ReadOnlyArray<InputDevice>?)null : new ReadOnlyArray<InputDevice>(assignedDevices);

        // Changing the source through this property also refreshes the player's private copy.
        public InputActionAsset InputActionAsset
        {
            get => inputActionAsset;
            set
            {
                if (inputActionAsset == value)
                    return;
                inputActionAsset = value;
                RefreshConfiguration();
            }
        }

        public string ActionMapName => DefaultActionMapName;
        public string MoveActionName => DefaultMoveActionName;

        private void Reset()
        {
            // Unity calls this when the component is added in the editor or reset there.
            AssignDefaultAssetIfMissing();
        }

        private void OnValidate()
        {
            InputDeadzone = inputDeadzone;
            AssignDefaultAssetIfMissing();
            // Inspector edits are checked later, before input is read.
            configurationDirty = true;
        }

        // Save this reference with the player so builds do not need an editor asset search.
        private void AssignDefaultAssetIfMissing()
        {
#if UNITY_EDITOR
            if (inputActionAsset != null)
                return;

            // Look beside this script so moving the package does not break automatic setup.
            string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(this));
            if (string.IsNullOrEmpty(scriptPath))
                return;

            string folderPath = System.IO.Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folderPath))
                inputActionAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(folderPath + "/PlayerControls.inputactions");
#endif
        }

        private void OnEnable()
        {
            // Listen for device changes and prepare this player's actions when enabled.
            InputSystem.onDeviceChange += OnDeviceChange;
            EnsureInitialized();
        }

        private void OnDisable()
        {
            // Stop listening and discard held input when the component is turned off.
            InputSystem.onDeviceChange -= OnDeviceChange;
            ReleaseActions();
            configurationDirty = true;
        }

        private void OnDestroy()
        {
            // Cleanup is safe even if OnDisable has already released the actions.
            InputSystem.onDeviceChange -= OnDeviceChange;
            ReleaseActions();
        }

        private void OnApplicationFocus(bool focused)
        {
            // Leaving the game window must not leave the player walking with old input.
            hasFocus = focused;
            UpdateMapState();
        }

        private void OnApplicationPause(bool paused)
        {
            // Pause input together with the application, then restore it when allowed.
            isPaused = paused;
            UpdateMapState();
        }

        // Lets another script temporarily block this player's input without changing the bindings.
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            UpdateMapState();
        }

        // Choose which devices this player may use. This does not reserve them for this player.
        // A null list accepts all devices. An empty list accepts none.
        public void AssignDevices(params InputDevice[] devices)
        {
            if (devices != null)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i] == null)
                        throw new ArgumentException("Assigned devices cannot contain null.", nameof(devices));
                }
            }

            // Copy the list so changes to the caller's array do not change our device selection.
            assignedDevices = devices == null ? null : (InputDevice[])devices.Clone();
            ResetInputButtons();
            MoveInput = Vector2.zero;
            ApplyDeviceFilter();
        }

        // The resolver reads once per step, then gives all movement modules the same input.
        internal Vector2 ReadSnapshot()
        {
            if (!isActiveAndEnabled || !inputEnabled || !hasFocus || isPaused)
                return ClearInput();

            if (!EnsureInitialized() || !moveAction.enabled)
                return ClearInput();

            // Read the action rather than a specific keyboard or gamepad.
            Vector2 value = moveAction.ReadValue<Vector2>();
            // Reject invalid numbers before they can reach movement calculations.
            if (float.IsNaN(value.x) || float.IsNaN(value.y)
                || float.IsInfinity(value.x) || float.IsInfinity(value.y))
                return ClearInput();

            // Limit diagonal input to length one, while keeping smaller analog-stick readings.
            MoveInput = Vector2.ClampMagnitude(value, 1f);
            if (MoveInput.magnitude <= inputDeadzone)
                MoveInput = Vector2.zero;
            if (runAction == null || !runAction.enabled)
                ResetRunInput();
            RunHeld = runAction != null && runAction.enabled && runAction.IsPressed();
            RunPressCount = pendingRunPressCount;
            DashInputAvailable = dashAction != null && dashAction.enabled;
            if (!DashInputAvailable)
                ResetDashInput();
            DashPressCount = pendingDashPressCount;
            return MoveInput;
        }

        // Prepare actions only when needed; existing actions are reused between readings.
        private bool EnsureInitialized()
        {
            if (configurationDirty)
            {
                // Try the default asset when no source has been assigned.
                AssignDefaultAssetIfMissing();
                configurationDirty = false;
                // Keep held input when editor validation does not change the settings.
                bool configurationUnchanged = runtimeActions != null
                    && initializedAsset == inputActionAsset;
                if (!configurationUnchanged)
                {
                    ReleaseActions();
                    initializationFailed = false;
                }
            }

            if (runtimeActions != null)
                return true;
            // Retry a failed setup only after a configuration change or re-enable.
            if (initializationFailed)
                return false;

            if (inputActionAsset == null)
                return FailInitialization();

            // Work on a copy, then find the required map (action group) and movement action.
            runtimeActions = Instantiate(inputActionAsset);
            runtimeActions.name = inputActionAsset.name + " (" + gameObject.name + ")";
            runtimeActions.Disable();
            moveMap = runtimeActions.FindActionMap(DefaultActionMapName, false);
            if (moveMap == null)
                return FailInitialization();

            // Movement needs two numbers for direction, supplied by a Value action with Vector2 type.
            moveAction = moveMap.FindAction(DefaultMoveActionName, false);
            if (moveAction == null || moveAction.type != InputActionType.Value
                || moveAction.expectedControlType != "Vector2")
                return FailInitialization();

            // Older assets without Run still provide normal walking input.
            runAction = moveMap.FindAction("Run", false);
            if (runAction != null && runAction.type != InputActionType.Button)
                runAction = null;
            if (runAction != null)
                runAction.performed += OnRunPerformed;
            // Optional ability buttons must not make older Move-only assets invalid.
            dashAction = moveMap.FindAction("Dash", false);
            if (dashAction != null && dashAction.type != InputActionType.Button)
                dashAction = null;
            if (dashAction != null)
                dashAction.performed += OnDashPerformed;
            initializedAsset = inputActionAsset;
            ApplyDeviceFilter();
            moveAction.canceled += OnMoveCanceled;
            UpdateMapState();
            return true;
        }

        private bool FailInitialization()
        {
            // A broken setup produces zero input instead of partial controls.
            ReleaseActions();
            initializationFailed = true;
            return false;
        }

        private void RefreshConfiguration()
        {
            // Discard the old reading before checking a changed input asset.
            configurationDirty = true;
            MoveInput = Vector2.zero;
            if (Application.isPlaying && isActiveAndEnabled)
                EnsureInitialized();
        }

        private void UpdateMapState()
        {
            ResetInputButtons();
            // Any change to input availability clears the previous reading immediately.
            MoveInput = Vector2.zero;
            if (moveMap == null)
                return;

            if (isActiveAndEnabled && inputEnabled && hasFocus && !isPaused)
                moveMap.Enable();
            else
                moveMap.Disable();
        }

        private void ApplyDeviceFilter()
        {
            // Filter only this player's copy. Other players keep their own device selection.
            if (runtimeActions == null || moveMap == null)
                return;

            ReadOnlyArray<InputDevice>? devices = assignedDevices == null
                ? (ReadOnlyArray<InputDevice>?)null
                : new ReadOnlyArray<InputDevice>(assignedDevices);
            runtimeActions.devices = devices;
            moveMap.devices = devices;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if ((assignedDevices == null || Array.IndexOf(assignedDevices, device) >= 0)
                && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected))
                ResetInputButtons();
            if (assignedDevices != null && Array.IndexOf(assignedDevices, device) >= 0
                && (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected))
            {
                // Unity removes disconnected devices from action filters.
                ApplyDeviceFilter();
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            // A released or interrupted action must not leave an old direction stored.
            MoveInput = Vector2.zero;
        }

        private Vector2 ClearInput()
        {
            ResetInputButtons();
            // Return a neutral reading whenever controls cannot be used safely.
            MoveInput = Vector2.zero;
            return MoveInput;
        }

        private void ReleaseActions()
        {
            if (dashAction != null)
                dashAction.performed -= OnDashPerformed;
            ResetInputButtons();
            if (runAction != null)
                runAction.performed -= OnRunPerformed;
            // Remove the callback before disabling and destroying this player's action copy.
            MoveInput = Vector2.zero;
            if (moveAction != null)
                moveAction.canceled -= OnMoveCanceled;
            if (runtimeActions != null)
            {
                runtimeActions.Disable();
                if (Application.isPlaying)
                    Destroy(runtimeActions);
                else
                    DestroyImmediate(runtimeActions);
            }

            runtimeActions = null;
            initializedAsset = null;
            moveMap = null;
            moveAction = null;
            runAction = null;
            dashAction = null;
        }

        private void OnRunPerformed(InputAction.CallbackContext context)
        {
            // Count presses between movement steps, including a quick press and release.
            unchecked { pendingRunPressCount++; }
        }

        private void ResetRunInput()
        {
            RunHeld = false;
            RunPressCount = pendingRunPressCount = 0;
            unchecked { InputResetVersion++; }
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            // Remember quick taps between movement steps without repeating a held button.
            unchecked { pendingDashPressCount++; }
        }

        private void ResetDashInput()
        {
            DashInputAvailable = false;
            DashPressCount = pendingDashPressCount = 0;
            unchecked { DashInputResetVersion++; }
        }

        private void ResetInputButtons()
        {
            ResetRunInput();
            ResetDashInput();
        }

        internal void ApplyLegacyDeadzone(int priority, float value)
        {
            if (value < 0f || priority >= legacyDeadzonePriority) return;
            InputDeadzone = value;
            legacyDeadzonePriority = priority;
        }
    }
}
