# Gameplay HUD

The authored HUD in `Project/Scenes/Debug/Kuba/Kuba.unity` is shared by PC and Android. Existing money, menu, joystick, Dash and player-panel artwork stays in use.

The same HUD is also saved in Lobby.unity as PlayerHUD. Kuba retains its HUD. The lobby keeps its original code panel separately, adds one EventSystem for touch controls and a scene-local replicated LobbyCoins wallet, and omits the unused countdown object. Copy/update it through `Tools > Kucharze > HUD > Copy HUD To Lobby`; this leaves Kuba unchanged. Add `-kucharze-hud-lobby` to both development probe processes to test the lobby directly.

- PC: coins at the top right, connected players listed vertically below them.
- Android: the same coins and roster, plus Menu at the top left, a floating joystick in the lower left touch area, and Dash at the bottom right. Unimplemented interaction buttons remain hidden.
- Four roster rows support the complete session, including the local player. Empty rows disappear. Ordering follows the replicated palette index, then owner ID. Names come from LobbyPlayerNickname; colors come from PlayerNetworkAppearance and LobbyPlayerColors. Plain-text names fit within the panel and support Polish characters.
- PlatformUI defaults to Automatic and uses UnityEngine.Device.Application for Device Simulator support. Its editor preview remains available. HudSafeArea accounts for screen cutouts and system navigation space. Keep Android in landscape as configured by the project.

HudPlayerRoster only reads replicated player data and updates UI. The player tint material keeps the original panel silhouette/shading while replacing its baked hue with the player's actual palette. It does not edit the original sprite files.

FloatingJoystick sends `<Gamepad>/leftStick`; OnScreenButton sends `<Gamepad>/buttonEast`, matching PlayerControls Dash. The owning PlayerInputActions handles these through its existing Input System map. Remote players never consume this local virtual gamepad. Joystick and Dash can be held with separate fingers.

The currency label is right-aligned tightly beside the coin, accounting for the text and sprite padding, with geometry-centered vertical alignment. Its right anchor offset is -120 canvas units. The joystick's handle center may reach the base rim so roughly half the handle protrudes at full deflection. Travel is capped at the base radius in base-local units. The touch origin reserves space for that protrusion at screen edges. Both scenes save a 120-unit travel maximum for the current 240-unit base and 100-unit handle.

Before the server receives a valid nickname, LobbyPlayerNickname exposes `Loading...` rather than a numbered player name. The same placeholder is used in the HUD, room signs and overhead labels; the actual validated nickname replaces it through normal replication.

HudGameMenu opens from the Android Menu button or Escape on PC. Resume restores local input. Leave Game uses the existing connection shutdown and entry-scene return. The popup explains that the online game continues and that the host leaving ends the session. New local controls are blocked and touch controls are hidden while the popup is open. An already accepted Dash completes normally, including its cooldown; opening the menu does not reset its input version. New presses during the menu are discarded. Full input disable, focus loss and application suspension retain their existing hard-reset behavior. Time.timeScale is never changed.

The timer is outside the requested HUD. Only its graphics are hidden through CanvasGroup; its GameObject, countdown logic and network adapter remain active. CoinDisplay continues to read the existing shared CoinWallet.

Editor setup: `Tools > Kucharze > HUD > Configure Kuba HUD`. This updates the saved scene without rebuilding level geometry. Save Kuba first if it is open and modified. The setup checks bindings and generates temporary desktop, wide-phone and tablet previews; these previews contain sample names for inspection only. Runtime roster rows always use connected players.

Development-only integration probe: use a lobby/game development build with `-kucharze-hud-probe host REPORT_DIRECTORY` and a second process with `-kucharze-hud-probe client REPORT_DIRECTORY`. The probe uses local UDP port 17841, enters the real game through the lobby readiness gate and checks the roster, coins, hidden timer graphics, mobile touch input and local menu. It is inactive without those explicit arguments. Actual executed checks are recorded in Movement/Validation.md.


## Supplied pause artwork

The GameMenu overlay in Lobby and Kuba uses the supplied cream patterned panel, MENU heading, Options / Recipes / End Session sprites and the online-not-paused notice. Recipes stays visible but non-interactable by user choice. Clicking or tapping outside the cream menu container, or pressing Escape, resumes local controls. The old X button is removed. A bottom hint reads `Click or tap outside to close`. The dimmer receives outside clicks; the cream panel absorbs clicks in its empty space, and the hint does not block pointer events. Options opens a copy of the existing menu settings with the same persistent audio/video services and platform rules. Back returns to the pause menu. End Session uses the existing connection shutdown.

Artwork lives under Project/Gameplay/UI/PauseMenu/Artwork (pause-menu-panel.png, pause-menu-title.png, online-game-not-paused.png) and Buttons (options-button.png, recipes-button.png, end-session-button.png). AssetDatabase.MoveAsset preserves GUIDs; import settings are Single Sprite, transparent alpha, no mipmaps, bilinear/clamp and uncompressed UI textures. TAKEIT is removed only after all six destinations are verified and the source folder is empty.

HudMenuCaptureFeature is installed on the PC and Mobile renderers. While GameMenu is active, it copies the completed scene after postprocessing and applies separate horizontal and vertical Gaussian filters at half resolution before overlay UI. The filter samples adjacent texels with sigma 5 (about ten full-resolution pixels), instead of using widely spaced UI samples. HudMenuBackdrop owns/resizes that texture and assigns it to RawImage and a per-open material instance, so the texture dimensions used by the blur stay explicit. The render graph exports the copy to establish the overlay UI read dependency. The UI shader samples _MainTex using normal quad UVs, without global camera textures or manual Y-flip flags; a separate 65% black dimmer sits below the sharp artwork. Closing the menu unregisters and releases the capture. No camera properties or Time.timeScale are changed. PauseMenuArtworkSetup rebuilds only the overlay and updates Jakub's credits; GameHudSetup retains the new hierarchy when configuring the surrounding HUD.


The backdrop UI shader declares and samples the UI-required _MainTex property and has exactly one pass. Fullscreen Gaussian passes live in the separate Hidden/Kucharze/Menu Gaussian Blur shader and MenuGaussianBlur material, used only by the renderer feature. Do not put renderer filter passes in a Canvas material: Game View can draw those additional passes with unbound blit inputs and overwrite the correct backdrop with gray. The renderer copy keeps the camera aspect ratio and is independent of Screen Space Overlay coordinates, opaque-texture downsampling and camera render-target flip state. HudMenuCaptureSetup installs only this menu capture feature while preserving each renderer's existing features.
