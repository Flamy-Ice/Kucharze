using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ModularMovement
{
    // Attach this to Player, alongside its movement components. It only shows their data.
    [DisallowMultipleComponent]
    // Read the visuals after PlayerRotation and PlayerAnimation finish their LateUpdate work.
    [DefaultExecutionOrder(200)]
    [AddComponentMenu("Movement/Player Movement Debug")]
    public sealed class PlayerMovementDebug : MonoBehaviour
    {
        // Choose which screen corner the panel stays near when the window changes size.
        public enum ScreenAnchor
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        // These saved settings control the panel's appearance, not the player's movement.
        [SerializeField, Tooltip("Screen corner where the debug panel appears.")]
        private ScreenAnchor anchor = ScreenAnchor.TopLeft;
        [SerializeField, InspectorName("Screen Offset (px)"), Tooltip("Distance from the selected screen corner, in pixels.")]
        private Vector2 screenOffset = new Vector2(12f, 12f);
        [SerializeField, InspectorName("Panel Width (px)"), Range(320f, 600f), Tooltip("Terminal width in pixels. Fits smaller screens automatically.")]
        private float panelWidth = 380f;
        [SerializeField, InspectorName("Font Size (px)"), Range(10, 20), Tooltip("Text size in pixels.")]
        private int fontSize = 13;
        [SerializeField, InspectorName("Background Opacity (0.2-1)"), Range(0.2f, 1f), Tooltip("Background opacity from 0.2 to 1, without units.")]
        private float backgroundOpacity = 0.9f;
        [SerializeField, Tooltip("Show the terminal when this component first starts.")]
        private bool startVisible = false;

        // Each switch hides or shows one group of information. Missing components are skipped.
        [SerializeField] private bool showTransform = true;
        [SerializeField] private bool showStates = true;
        [SerializeField] private bool showInput = true;
        [SerializeField] private bool showWalk = true;
        [SerializeField] private bool showHorizontalMovement = true;
        [SerializeField] private bool showRun = true;
        [SerializeField] private bool showDash = true;
        [SerializeField] private bool showRotation = true;
        [SerializeField, Tooltip("Show the selected animation state.")]
        private bool showAnimation = true;
        [SerializeField] private bool showGravity = true;
        [SerializeField] private bool showMotor = true;

        // Reuse a text builder each frame, then pass the finished text to the drawing code.
        private readonly StringBuilder textBuilder = new StringBuilder(2048);
        private string terminalText = string.Empty;
        private Vector2 scrollPosition;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;
        // A private copy of Unity's appearance settings keeps other interfaces unchanged.
        private GUISkin panelSkin;
        // Textures for the custom terminal scrollbar so other interfaces stay untouched.
        private Texture2D scrollbarTrackTexture;
        private Texture2D scrollbarThumbNormalTexture;
        private Texture2D scrollbarThumbActiveTexture;
        // These track the game window, not whether the player's movement is allowed.
        private bool hasFocus = true;
        private bool isPaused;
        private int lastToggleFrame = -1;
        private InputActionAsset debugActions;
        private InputActionAsset debugSource;
        private InputAction sourceDebugAction;
        private InputAction debugAction;
        private InputDevice[] debugDevices;
        private bool debugDeviceFilterDirty = true;

        private const float ScrollbarWidth = 8f;
        private static readonly Color ScrollbarTrackColor = new Color(0.08f, 0.11f, 0.15f, 0.6f);
        private static readonly Color ScrollbarThumbColor = new Color(0.36f, 0.85f, 1f, 0.45f);
        private static readonly Color ScrollbarThumbActiveColor = new Color(0.50f, 0.92f, 1f, 0.95f);

        // Other scripts can show or hide the panel without changing movement settings.
        public bool IsOverlayVisible { get; set; }

        private void Awake()
        {
            // Unity calls this when the component first starts. Use the saved opening preference.
            ValidateSettings();
            IsOverlayVisible = startVisible;
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDebugDeviceChange;
            RefreshDebugAction();
        }

        private void Update()
        {
            RefreshDebugAction();
            if (hasFocus && !isPaused && debugAction != null && debugAction.WasPressedThisFrame())
                ToggleOverlay();
        }

        private void RefreshDebugAction()
        {
            PlayerInputActions input = GetComponent<PlayerInputActions>();
            InputActionAsset source = input != null ? input.RuntimeActions ?? input.InputActionAsset : null;
            InputAction action = source?.FindActionMap("Player", false)?.FindAction("Debug", false);
            if (source != debugSource || action != sourceDebugAction)
            {
                ReleaseDebugActions();
                debugSource = source;
                sourceDebugAction = action;
                if (action != null && action.type == InputActionType.Button)
                {
                    // Enable only the diagnostic action in a private copy, never the player's movement.
                    debugActions = Instantiate(source);
                    debugActions.Disable();
                    debugAction = debugActions.FindActionMap("Player").FindAction("Debug");
                    debugDeviceFilterDirty = true;
                    UpdateDebugActionState();
                }
            }
            if (debugAction == null)
                return;

            // Use the same device selection as movement, so this panel follows this player's controls.
            ReadOnlyArray<InputDevice>? devices = input.AssignedDevices;
            bool changed = devices.HasValue != (debugDevices != null);
            if (devices.HasValue && !changed)
            {
                changed = devices.Value.Count != debugDevices.Length;
                for (int index = 0; !changed && index < debugDevices.Length; index++)
                    changed = devices.Value[index] != debugDevices[index];
            }
            if (!changed && !debugDeviceFilterDirty)
                return;
            debugDevices = devices.HasValue ? new InputDevice[devices.Value.Count] : null;
            if (debugDevices != null)
                for (int index = 0; index < debugDevices.Length; index++)
                    debugDevices[index] = devices.Value[index];
            ReadOnlyArray<InputDevice>? filter = debugDevices == null
                ? (ReadOnlyArray<InputDevice>?)null : new ReadOnlyArray<InputDevice>(debugDevices);
            debugActions.devices = filter;
            debugAction.actionMap.devices = filter;
            debugDeviceFilterDirty = false;
        }

        private void OnDebugDeviceChange(InputDevice device, InputDeviceChange change) => debugDeviceFilterDirty = true;

        private void UpdateDebugActionState()
        {
            if (debugAction == null)
                return;
            if (isActiveAndEnabled && hasFocus && !isPaused)
                debugAction.Enable();
            else
                debugAction.Disable();
        }

        private void ReleaseDebugActions()
        {
            debugAction = null;
            sourceDebugAction = null;
            debugSource = null;
            debugDevices = null;
            if (debugActions == null)
                return;
            debugActions.Disable();
            if (Application.isPlaying)
                Destroy(debugActions);
            else
                DestroyImmediate(debugActions);
            debugActions = null;
        }

        public void ToggleOverlay()
        {
            // Ignore a second request in the same frame so the panel does not immediately close again.
            if (lastToggleFrame == Time.frameCount)
                return;

            lastToggleFrame = Time.frameCount;
            IsOverlayVisible = !IsOverlayVisible;
        }

        // Ignore the shortcut while another window is selected or the application is paused.
        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            UpdateDebugActionState();
        }

        private void OnApplicationPause(bool paused)
        {
            isPaused = paused;
            UpdateDebugActionState();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDebugDeviceChange;
            ReleaseDebugActions();
            // Forget the old drawing data and release the appearance copy when this component is off.
            terminalText = string.Empty;
            textStyle = null;
            titleStyle = null;
            scrollPosition = Vector2.zero;
            ReleasePanelSkin();
        }

        // Also release the copy if Unity removes this component or its GameObject.
        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDebugDeviceChange;
            ReleaseDebugActions();
            ReleasePanelSkin();
        }

        // Unity calls this when saved settings are edited in the Inspector.
        private void OnValidate() => ValidateSettings();

        private void ValidateSettings()
        {
            // Keep sizes, spacing and opacity within usable limits before drawing.
            panelWidth = ClampFinite(panelWidth, 320f, 600f, 380f);
            fontSize = Mathf.Clamp(fontSize, 10, 20);
            backgroundOpacity = ClampFinite(backgroundOpacity, 0.2f, 1f, 0.9f);
            screenOffset.x = ClampFinite(screenOffset.x, 0f, 10000f, 12f);
            screenOffset.y = ClampFinite(screenOffset.y, 0f, 10000f, 12f);
        }

        private void LateUpdate()
        {
            // A hidden panel does not need to collect data or rebuild its text.
            if (!IsOverlayVisible)
                return;

            // Unity calls this after Update, when the movement step has normally finished.
            // Look only on this Player, so another player's data cannot appear here.
            PlayerStates states = GetComponent<PlayerStates>();
            PlayerInputActions input = GetComponent<PlayerInputActions>();
            PlayerWalk walk = GetComponent<PlayerWalk>();
            PlayerRun run = GetComponent<PlayerRun>();
            PlayerDash dash = GetComponent<PlayerDash>();
            PlayerRotation rotation = GetComponent<PlayerRotation>();
            PlayerAnimation animation = GetComponent<PlayerAnimation>();
            PlayerGravity gravity = GetComponent<PlayerGravity>();
            PlayerMotor motor = GetComponent<PlayerMotor>();
            PlayerHorizontalMovement horizontal = GetComponent<PlayerHorizontalMovement>();

            textBuilder.Clear();

            // Only include sections that are selected and have the data they need.
            if (showTransform)
            {
                AppendHeading("TRANSFORM & SPEED", "#FFDD55");
                AppendVector("Position", transform.position, "m");
                if (states != null)
                {
                    // Show movement measured after collisions, rather than the requested speed.
                    AppendVector("Actual velocity", states.ActualVelocity, "m/s");
                    textBuilder.AppendFormat(CultureInfo.InvariantCulture,
                        "<b>Planar speed:</b> {0:F2} m/s ({1:F2} km/h)\n", states.PlanarSpeed, states.PlanarSpeed * 3.6f);
                }
            }

            if (showStates && states != null)
            {
                AppendHeading("STATES", "#66FFAA");
                AppendMovementState(states);
                AppendBoolean("Moving", states.IsMoving);
                AppendBoolean("Grounded", states.IsGrounded);
            }

            if (showAnimation && animation != null)
                AppendAnimation(animation);

            if (showInput && input != null)
            {
                AppendHeading("INPUT", "#5CC2FF");
                Vector2 move = input.MoveInput;
                textBuilder.AppendFormat(CultureInfo.InvariantCulture, "<b>Move Input:</b> ({0:F2}, {1:F2})\n", move.x, move.y);
                AppendInputConfiguration(input);
                AppendNumber("Input Deadzone", input.InputDeadzone, "(0–1)");
            }

            if (showGravity && gravity != null)
            {
                AppendHeading("GRAVITY & VERTICAL", "#BA9EFF");
                // The measured vertical speed and gravity settings describe different things.
                if (states != null)
                    AppendNumber("Vertical Velocity", states.ActualVelocity.y, "m/s");
                AppendNumber("Gravity", gravity.GravityAcceleration, "m/s²");
                AppendNumber("Ground stick", gravity.GroundStickVelocity, "m/s");
                AppendNumber("Max fall speed", gravity.MaxFallSpeed, "m/s");
            }

            if (showMotor && motor != null)
            {
                AppendHeading("MOTOR", "#FFB35C");
                AppendNumber("Moving threshold", motor.MovingSpeedThreshold, "m/s");
            }

            if (showRotation && rotation != null)
            {
                AppendHeading("ROTATION", "#A8B5FF");
                Transform model = rotation.ResolvedModel;
                if (model != null)
                    AppendNumber("Facing Angle", model.eulerAngles.y, "deg");
                else
                    textBuilder.AppendLine("<b>Model:</b> <color=#FFB35C>Not found</color>");
                AppendNumber("Rotation Speed", rotation.RotationSpeed, "deg/s");
                AppendBoolean("Finish Rotation When Stopped", rotation.FinishRotationWhenStopped);
            }

            if (showHorizontalMovement && horizontal != null)
            {
                AppendHeading("HORIZONTAL MOVEMENT", "#FFD65C");
                AppendNumber("Acceleration", horizontal.Acceleration, "m/s²");
                AppendNumber("Deceleration", horizontal.Deceleration, "m/s²");
            }

            if (showWalk && walk != null)
            {
                AppendHeading("WALK", "#6BD699");
                AppendBoolean("Always Full Walk Speed", walk.AlwaysFullWalkSpeed);
                AppendNumber("Walk Speed", walk.WalkSpeed, "m/s");
            }

            if (showRun && run != null)
            {
                AppendHeading("RUN", "#F27891");
                AppendBoolean("Always Run", run.AlwaysRun);
                AppendBoolean("Always Full Run Speed", run.AlwaysFullRunSpeed);
                textBuilder.Append("<b>Activation Mode:</b> ").Append(run.ActivationMode).AppendLine();
                AppendNumber("Run Speed", run.RunSpeed, "m/s");
            }

            if (showDash && dash != null)
            {
                AppendHeading("DASH", "#61E8FF");
                AppendNumber("Dash Speed", dash.DashSpeed, "m/s");
                AppendNumber("Dash Duration", dash.DashDuration, "s");
                textBuilder.AppendFormat(CultureInfo.InvariantCulture, "<b>Cooldown:</b> {0:F2} s", dash.Cooldown);
                if (dash.CooldownRemaining > 0f)
                    textBuilder.AppendFormat(CultureInfo.InvariantCulture,
                        " <color=#61E8FF>(<b>remaining:</b> {0:F2} s)</color>", dash.CooldownRemaining);
                textBuilder.AppendLine();
                AppendBoolean("Dashing", dash.IsDashing);
            }

            // Keep one finished reading for drawing, which Unity may request several times per frame.
            terminalText = textBuilder.ToString();
        }

        // Show only the state chosen by this player's animation adapter.
        private void AppendAnimation(PlayerAnimation animation)
        {
            AppendHeading("ANIMATION", "#FFB87A");
            string selected = animation.CurrentState == PlayerAnimation.IdleState ? "Idle"
                : animation.CurrentState == PlayerAnimation.RunState ? "Run"
                : animation.CurrentState == PlayerAnimation.DashState ? "Dash" : "Unknown";
            string color = selected == "Dash" ? "#61E8FF" : selected == "Run" ? "#F27891" : "#8C97A6";
            textBuilder.Append("<b>Animation State:</b> <color=").Append(color).Append("><b>")
                .Append(selected).Append("</b></color>\n");
        }

        private void AppendInputConfiguration(PlayerInputActions input)
        {
            // Explain a broken input setup without fixing it or changing the player's controls.
            InputActionAsset asset = input.InputActionAsset;
            string message = null;
            if (asset == null)
                message = "Assign an Input Action Asset.";
            else
            {
                // Movement needs the Player group and a Move action that provides two direction numbers.
                InputActionMap map = asset.FindActionMap(input.ActionMapName, false);
                InputAction action = map?.FindAction(input.MoveActionName, false);
                if (map == null)
                    message = "Add the Player action map.";
                else if (action == null)
                    message = "Add the Player/Move action.";
                else if (action.type != InputActionType.Value || action.expectedControlType != "Vector2")
                    message = "Set Player/Move to Value / Vector2.";
            }

            // A valid setup needs no extra line, even when the player is standing still.
            if (message != null)
                textBuilder.Append("<b>Configuration:</b> <color=#FFB35C>")
                    .Append(message).Append("</color>\n");
        }

        private void AppendMovementState(PlayerStates states)
        {
            // Name what the player actually does. Holding movement against a wall can still mean Idle.
            string stateName;
            string color;
            if (!states.IsGrounded)
            {
                stateName = "InAir";
                color = "#BA9EFF";
            }
            else if (!states.IsMoving)
            {
                stateName = "Idle";
                color = "#8C97A6";
            }
            else
            {
                // Use the chosen movement's name so future abilities need no new list here.
                stateName = states.ActiveMovementSource == "None" ? "Moving" : states.ActiveMovementSource;
                color = stateName == "Dash" ? "#61E8FF" : stateName == "Run" ? "#F27891" : "#6BD699";
            }

            textBuilder.Append("<b>State:</b> <color=").Append(color).Append("><b>")
                .Append(EscapeRichText(stateName)).Append("</b></color>\n");
        }

        private void AppendHeading(string title, string color)
        {
            // These text tags add color and bold letters; they are hidden in the finished panel.
            if (textBuilder.Length > 0)
                textBuilder.Append('\n');
            textBuilder.Append("<color=").Append(color).Append("><b>> ").Append(title).Append("</b></color>\n");
        }

        private void AppendNumber(string label, float value, string unit)
        {
            // Keep two decimal places and a dot so readings have the same format on every computer.
            textBuilder.AppendFormat(CultureInfo.InvariantCulture, "<b>{0}:</b> {1:F2} {2}\n", label, value, unit);
        }

        private void AppendVector(string label, Vector3 value, string unit)
        {
            // Show the three world directions together: X across, Y up, and Z forward.
            textBuilder.AppendFormat(CultureInfo.InvariantCulture, "<b>{0}:</b> ({1:F2}, {2:F2}, {3:F2}) {4}\n",
                label, value.x, value.y, value.z, unit);
        }

        private void AppendBoolean(string label, bool value)
        {
            // Show yes/no readings in color so they are easy to spot while the player moves.
            textBuilder.Append("<b>").Append(label).Append(":</b>").Append(value
                ? " <color=#66FFAA><b>Yes</b></color>\n"
                : " <color=#F87171><b>No</b></color>\n");
        }

        private void OnGUI()
        {
            // Unity calls this to draw the panel and handle its mouse controls.
            // Skip drawing when hidden, disabled or too small to read.
            if (!IsOverlayVisible || !isActiveAndEnabled || Screen.width < 100 || Screen.height < 140)
                return;

            // Unity shares these drawing settings with other interfaces, so save them before use.
            Color originalColor = GUI.color;
            Color originalContentColor = GUI.contentColor;
            Color originalBackgroundColor = GUI.backgroundColor;
            int originalDepth = GUI.depth;
            GUISkin originalSkin = GUI.skin;
            try
            {
                EnsurePanelSkin(originalSkin);
                GUI.skin = panelSkin;
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.depth = -100;
                PrepareStyles();
                DrawTerminal();
            }
            finally
            {
                // Other on-screen interfaces keep their own colors and drawing order.
                GUI.color = originalColor;
                GUI.contentColor = originalContentColor;
                GUI.backgroundColor = originalBackgroundColor;
                GUI.depth = originalDepth;
                GUI.skin = originalSkin;
            }
        }

        private void PrepareStyles()
        {
            // Create text styles once and reuse them while the component stays enabled.
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontStyle = FontStyle.Normal,
                    wordWrap = true,
                    padding = new RectOffset(),
                    margin = new RectOffset()
                };
                textStyle.normal.textColor = new Color(0.9f, 0.93f, 0.96f);
                titleStyle = new GUIStyle(textStyle) { fontStyle = FontStyle.Bold, wordWrap = false };
                titleStyle.normal.textColor = new Color(0.36f, 0.85f, 1f);
                RemoveHoverAppearance(textStyle);
                RemoveHoverAppearance(titleStyle);
            }

            // Apply size changes from the Inspector without rebuilding the styles.
            textStyle.fontSize = fontSize;
            textStyle.font = GUI.skin.font;
            titleStyle.fontSize = fontSize + 1;
            titleStyle.font = textStyle.font;
        }

        private void EnsurePanelSkin(GUISkin source)
        {
            if (panelSkin != null)
                return;

            // Keep the shared skin unchanged for other interfaces and players.
            panelSkin = Instantiate(source);
            panelSkin.hideFlags = HideFlags.HideAndDontSave;

            // Give the terminal its own sleek scrollbar to replace the clunky default.
            scrollbarTrackTexture = CreateCapsuleTexture(8, 16, ScrollbarTrackColor);
            scrollbarThumbNormalTexture = CreateCapsuleTexture(8, 16, ScrollbarThumbColor);
            scrollbarThumbActiveTexture = CreateCapsuleTexture(8, 16, ScrollbarThumbActiveColor);

            GUIStyle vTrack = panelSkin.verticalScrollbar;
            vTrack.fixedWidth = ScrollbarWidth;
            vTrack.border = new RectOffset(3, 3, 5, 5);
            vTrack.padding = new RectOffset();
            vTrack.margin = new RectOffset();
            vTrack.normal.background = scrollbarTrackTexture;

            GUIStyle vThumb = panelSkin.verticalScrollbarThumb;
            vThumb.fixedWidth = ScrollbarWidth;
            vThumb.border = new RectOffset(3, 3, 5, 5);
            vThumb.padding = new RectOffset();
            vThumb.margin = new RectOffset();
            vThumb.normal.background = scrollbarThumbNormalTexture;
            vThumb.active.background = scrollbarThumbActiveTexture;

            // Completely remove the arrow buttons so the slider is clean and modern without arrows.
            panelSkin.verticalScrollbarUpButton = GUIStyle.none;
            panelSkin.verticalScrollbarDownButton = GUIStyle.none;

            // Hide the unused horizontal scrollbar and its buttons.
            GUIStyle hTrack = panelSkin.horizontalScrollbar;
            hTrack.fixedWidth = 0;
            hTrack.fixedHeight = 0;
            panelSkin.horizontalScrollbarThumb.fixedWidth = 0;
            panelSkin.horizontalScrollbarThumb.fixedHeight = 0;
            panelSkin.horizontalScrollbarLeftButton = GUIStyle.none;
            panelSkin.horizontalScrollbarRightButton = GUIStyle.none;

            foreach (GUIStyle style in new[] { panelSkin.label, panelSkin.scrollView,
                vTrack, vThumb, hTrack, panelSkin.horizontalScrollbarThumb })
                RemoveHoverAppearance(style);
        }

        private static void RemoveHoverAppearance(GUIStyle style)
        {
            // Use the resting appearance when the mouse passes over the panel or its scrollbars.
            style.hover = style.normal;
            style.onHover = style.onNormal;
        }

        private void ReleasePanelSkin()
        {
            DestroyTexture(ref scrollbarTrackTexture);
            DestroyTexture(ref scrollbarThumbNormalTexture);
            DestroyTexture(ref scrollbarThumbActiveTexture);

            if (panelSkin == null)
                return;
            // Unity removes objects later during play, but immediately outside play.
            if (Application.isPlaying)
                Destroy(panelSkin);
            else
                DestroyImmediate(panelSkin);
            panelSkin = null;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;
            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
            texture = null;
        }

        private static Texture2D CreateCapsuleTexture(int width, int height, Color fillColor)
        {
            // Creates a small rounded shape that stretches cleanly at any size.
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = (width - 1) * 0.5f;
            float centerX = radius;
            float topCenterY = (height - 1) - radius;
            float bottomCenterY = radius;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float dy;
                if (y > topCenterY)
                    dy = y - topCenterY;
                else if (y < bottomCenterY)
                    dy = bottomCenterY - y;
                else
                    dy = 0f;

                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Abs(x - centerX);
                    float distance = (dy == 0f) ? dx : Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * width + x] = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void DrawTerminal()
        {
            // Leave room for screen edges, text padding and the standard scrollbar.
            float width = Mathf.Min(panelWidth, Screen.width - 16f);
            float contentWidth = width - 42f;

            // Scale the content font down if any line is too wide for the panel.
            textStyle.fontSize = fontSize;
            while (textStyle.fontSize > 8 && GetMaxLineWidth(terminalText, textStyle) > contentWidth)
                textStyle.fontSize--;

            // Wrapped lines need more height. Long content will scroll instead of leaving the screen.
            float contentHeight = textStyle.CalcHeight(new GUIContent(terminalText), contentWidth);
            float height = Mathf.Min(contentHeight + 60f, Screen.height - 16f);
            // Position the panel from the chosen corner, then keep it inside the screen.
            bool right = anchor == ScreenAnchor.TopRight || anchor == ScreenAnchor.BottomRight;
            bool bottom = anchor == ScreenAnchor.BottomLeft || anchor == ScreenAnchor.BottomRight;
            float x = right ? Screen.width - width - screenOffset.x : screenOffset.x;
            float y = bottom ? Screen.height - height - screenOffset.y : screenOffset.y;
            Rect panel = new Rect(Mathf.Clamp(x, 8f, Screen.width - width - 8f),
                Mathf.Clamp(y, 8f, Screen.height - height - 8f), width, height);

            DrawRectangle(panel, new Color(0.04f, 0.05f, 0.07f, backgroundOpacity));
            DrawRectangle(new Rect(panel.x, panel.y, panel.width, 3f), new Color(0.36f, 0.85f, 1f));
            // Inside this group, positions are measured from the panel's top-left corner.
            GUI.BeginGroup(panel);

            // Scale the title font down if the panel is narrow so no letters are ever cut off.
            const string titleText = "> PLAYER MOVEMENT DEBUG";
            float availableTitleWidth = width - 24f;
            titleStyle.fontSize = Mathf.Max(8, textStyle.fontSize + 1);
            while (titleStyle.fontSize > 8)
            {
                titleStyle.CalcMinMaxWidth(new GUIContent(titleText), out _, out float titleWidth);
                if (titleWidth <= availableTitleWidth)
                    break;
                titleStyle.fontSize--;
            }

            GUI.Label(new Rect(12f, 12f, availableTitleWidth, 26f), titleText, titleStyle);
            DrawRectangle(new Rect(12f, 40f, width - 24f, 1f), new Color(0.2f, 0.25f, 0.3f));
            Rect viewport = new Rect(12f, 48f, width - 24f, height - 60f);
            float maxScroll = Mathf.Max(0f, contentHeight - viewport.height);
            scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0f, maxScroll);

            // Block mouse clicks and dragging on the slider so it only works as a visual indicator.
            Rect scrollbarArea = new Rect(viewport.xMax - 20f, viewport.y, 20f, viewport.height);
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                && scrollbarArea.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
            }

            // Let the mouse wheel scroll the text smoothly when hovering anywhere over the panel.
            Rect panelBounds = new Rect(0f, 0f, width, height);
            if (Event.current.type == EventType.ScrollWheel && panelBounds.Contains(Event.current.mousePosition))
            {
                if (maxScroll > 0f)
                {
                    scrollPosition.y = Mathf.Clamp(scrollPosition.y + Event.current.delta.y * 20f, 0f, maxScroll);
                    Event.current.Use();
                }
            }

            Vector2 previousScroll = scrollPosition;
            // The viewport is the visible area. Save how far the user has scrolled through the text.
            scrollPosition = GUI.BeginScrollView(viewport, scrollPosition,
                new Rect(0f, 0f, contentWidth, contentHeight), false, false);

            // Ignore mouse clicks or drags so only the mouse wheel can move the scroll position.
            if (Event.current.isMouse && Event.current.type != EventType.ScrollWheel)
                scrollPosition = previousScroll;

            GUI.Label(new Rect(0f, 0f, contentWidth, contentHeight), terminalText, textStyle);
            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private static float GetMaxLineWidth(string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            // Check each line's unwrapped width so long readings fit on a single line.
            float maxWidth = 0f;
            int start = 0;
            while (start < text.Length)
            {
                int end = text.IndexOf('\n', start);
                if (end < 0)
                    end = text.Length;

                if (end > start)
                {
                    string line = text.Substring(start, end - start);
                    style.CalcMinMaxWidth(new GUIContent(line), out _, out float lineWidth);
                    if (lineWidth > maxWidth)
                        maxWidth = lineWidth;
                }

                start = end + 1;
            }

            return maxWidth;
        }

        private static void DrawRectangle(Rect rectangle, Color color)
        {
            // Tint Unity's small white image to draw a solid rectangle without creating a new image.
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static string EscapeRichText(string value)
        {
            // A movement name must appear as text, even if it contains characters used by formatting tags.
            return string.IsNullOrEmpty(value) ? "Unnamed" : value.Replace('<', '[').Replace('>', ']');
        }

        private static float ClampFinite(float value, float minimum, float maximum, float fallback)
        {
            // Replace unusable numbers with a default; keep ordinary numbers inside the allowed range.
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
        }
    }
}
