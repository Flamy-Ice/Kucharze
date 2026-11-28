#if KUCHARZE_ONLINE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace ModularMovement
{
    // Exercises the gameplay HUD with real network peers only when development test arguments are supplied.
    public sealed class GameHudSmokeProbe : MonoBehaviour
    {
        private NetworkManager manager;
        private IEnumerator scenario;
        private string directory, role;
        private float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-kucharze-hud-probe");
            if (index < 0 || arguments.Length < index + 3) return;
            var probe = new GameObject("GameHudSmokeProbe").AddComponent<GameHudSmokeProbe>();
            DontDestroyOnLoad(probe.gameObject);
            probe.role = arguments[index + 1];
            probe.directory = arguments[index + 2];
            Directory.CreateDirectory(probe.directory);
            probe.started = Time.realtimeSinceStartup;
            probe.scenario = probe.Run();
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 70) throw new Exception("HUD test timed out.");
                if (scenario != null && !scenario.MoveNext()) Finish("PASS", 0);
            }
            catch (Exception exception) { Finish("FAIL: " + exception, 1); }
        }

        private IEnumerator Run()
        {
            while (manager == null) { manager = FindAnyObjectByType<NetworkManager>(); yield return null; }
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 17841);
            bool host = role == "host";
            bool lobbyOnly = Environment.GetCommandLineArgs().Contains("-kucharze-hud-lobby");
            Check(host ? manager.StartHost() : manager.StartClient(), "Could not start network peer.");
            if (host && !lobbyOnly)
            {
                LobbySession session = null;
                while (session == null || session.PlayerCount != 2)
                { session = FindAnyObjectByType<LobbySession>(); yield return null; }
                Check(session.LevelSelection.TrySelectLevel(0, 0), "Could not select the game scene.");
                var layout = FindAnyObjectByType<LobbyLayout>();
                Check(layout != null && layout.TryGetStartCenter(out _), "No ready zone.");
                layout.TryGetStartCenter(out var center);
                int slot = 0;
                foreach (var client in manager.ConnectedClientsList)
                {
                    var player = client.PlayerObject;
                    var resolver = player.GetComponent<PlayerMovementResolver>();
                    bool simulationWasEnabled = resolver.SimulationEnabled;
                    resolver.SimulationEnabled = false;
                    var controller = player.GetComponent<CharacterController>();
                    controller.enabled = false;
                    player.GetComponent<NetworkTransform>().Teleport(center + Vector3.right * (slot++ * 1.5f - 0.75f), player.transform.rotation, player.transform.localScale);
                    controller.enabled = true;
                    // Placement must not leave persistent players unable to dash in the next scene.
                    resolver.SimulationEnabled = simulationWasEnabled;
                }
            }
            HudPlayerRoster roster = null;
            while (roster == null || (!lobbyOnly && SceneManager.GetActiveScene().path.EndsWith("/Lobby.unity")))
            { roster = FindAnyObjectByType<HudPlayerRoster>(); yield return null; }
            while (FindObjectsByType<PlayerNetworkAppearance>().Count(player => player.IsSpawned) < 2) yield return null;
            float until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) yield return null;
            roster.Refresh();
            Transform hud = roster.transform;
            var platform = hud.GetComponent<PlatformUI>();
            Check(!Find(hud, "Mobile").gameObject.activeSelf, "Desktop unexpectedly showed mobile controls.");
            var players = FindObjectsByType<PlayerNetworkAppearance>().Where(player => player.IsSpawned)
                .OrderBy(player => player.PaletteIndex).ThenBy(player => player.OwnerClientId).ToArray();
            var rows = Find(hud, "Player_Section").GetComponentsInChildren<Image>();
            Check(players.Length == 2 && rows.Length == 2, "Roster did not hide empty slots.");
            for (int i = 0; i < rows.Length; i++)
            {
                var skin = players[i].GetComponentInChildren<SkinnedMeshRenderer>();
                var properties = new MaterialPropertyBlock();
                foreach (int slot in new[] { 0, 1 })
                {
                    skin.GetPropertyBlock(properties, slot);
                    Check(properties.GetColor("_BaseColor") == LobbyPlayerColors.Get(players[i].PaletteIndex),
                        "Coat, sleeves or hat did not use the replicated player color.");
                }
                skin.GetPropertyBlock(properties, 2);
                Check(properties.GetColor("_BaseColor") == skin.sharedMaterials[2].GetColor("_BaseColor"),
                    "Hands and head did not retain their shared skin color.");
                Check(rows[i].color == LobbyPlayerColors.Get(players[i].PaletteIndex), "Roster color differed from the player's palette.");
                Check(rows[i].GetComponentInChildren<TMP_Text>().text == players[i].GetComponent<LobbyPlayerNickname>().Nickname,
                    "Roster nickname differed from the replicated nickname.");
            }
            if (!lobbyOnly)
            {
                Check(Find(hud, "Timer_Background").GetComponent<CanvasGroup>().alpha == 0, "Clock graphics were visible.");
                Check(Find(hud, "Time").GetComponent<CountdownTimer>().IsRunning, "Hiding the clock stopped its logic.");
            }
            var wallet = FindAnyObjectByType<CoinWallet>();
            if (host) Check(wallet.TryAddCoins(7), "Could not change shared coins.");
            until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) yield return null;
            Check(Find(hud, "Money").GetComponent<TMP_Text>().text == wallet.Coins + "$", "Shared money label was stale.");
            if (!host)
            {
                // Force only this test client's mobile presentation; production still detects its platform.
                platform.enabled = false;
                Find(hud, "Mobile").gameObject.SetActive(true);
                Check(!Find(hud, "Pick Up Icon").gameObject.activeInHierarchy && !Find(hud, "Interact Icon").gameObject.activeInHierarchy,
                    "Unused interactions appeared on mobile.");
                var joystick = Find(hud, "Joystick").GetComponent<FloatingJoystick>();
                var rect = (RectTransform)joystick.transform;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                var pointer = new PointerEventData(EventSystem.current) { pointerId = 7, position = point, button = PointerEventData.InputButton.Left };
                joystick.OnPointerDown(pointer);
                pointer.position += Vector2.right * 150;
                joystick.OnDrag(pointer);
                var body = (RectTransform)Find(hud, "Joystick Body");
                var handle = (RectTransform)Find(hud, "Joystick Move");
                foreach (Vector2 direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down, Vector2.one.normalized })
                {
                    pointer.position = point + direction * 10000f;
                    joystick.OnDrag(pointer);
                    float allowed = Mathf.Min(body.rect.width, body.rect.height) * 0.5f;
                    Check(Mathf.Abs(handle.anchoredPosition.magnitude - allowed) <= 0.1f,
                        "Joystick handle center did not stop at the rim with half the handle outside.");
                }
                pointer.position = point + Vector2.right * 150;
                joystick.OnDrag(pointer);
                until = Time.realtimeSinceStartup + 0.2f;
                while (Time.realtimeSinceStartup < until) yield return null;
                var input = manager.LocalClient.PlayerObject.GetComponent<PlayerInputActions>();
                Check(input.CaptureSnapshot().Move.x > 0.4f, "Joystick did not reach the owning player's controls.");
                uint presses = input.CaptureSnapshot().DashPressCount;
                var dash = Find(hud, "Dash Icon").GetComponent<OnScreenButton>();
                var secondFinger = new PointerEventData(EventSystem.current) { pointerId = 8, button = PointerEventData.InputButton.Left };
                dash.OnPointerDown(secondFinger);
                until = Time.realtimeSinceStartup + 0.1f;
                while (Time.realtimeSinceStartup < until) yield return null;
                Check(input.CaptureSnapshot().DashPressCount > presses && input.CaptureSnapshot().Move.x > 0.4f,
                    "Dash and joystick could not be used together.");
                dash.OnPointerUp(secondFinger);
                joystick.OnPointerUp(pointer);
                pointer.position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.min + Vector2.one));
                joystick.OnPointerDown(pointer);
                var corners = new Vector3[4];
                body.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = rect.InverseTransformPoint(corner);
                    Check(local.x >= rect.rect.xMin - 0.1f && local.x <= rect.rect.xMax + 0.1f
                        && local.y >= rect.rect.yMin - 0.1f && local.y <= rect.rect.yMax + 0.1f,
                        "Joystick base escaped the screen touch area.");
                }
                joystick.OnPointerUp(pointer);
                var menu = hud.GetComponent<HudGameMenu>();
                var cameraData = Camera.main.GetUniversalAdditionalCameraData();
                var colorOption = cameraData.requiresColorOption;
                var menuCamera = Camera.main;
                Vector3 cameraPosition = menuCamera.transform.position;
                Quaternion cameraRotation = menuCamera.transform.rotation;
                float cameraFov = menuCamera.fieldOfView;
                float cameraSize = menuCamera.orthographicSize;
                float cameraAspect = menuCamera.aspect;
                CaptureMenu(hud, Path.Combine(directory, "baseline.png"));
                var dashAbility = manager.LocalClient.PlayerObject.GetComponent<PlayerDash>();
                until = Time.realtimeSinceStartup + 3f;
                while (dashAbility.CooldownRemaining > 0f && Time.realtimeSinceStartup < until) yield return null;
                dash.OnPointerDown(secondFinger);
                until = Time.realtimeSinceStartup + 1f;
                while (!dashAbility.IsDashing && Time.realtimeSinceStartup < until) yield return null;
                Check(dashAbility.IsDashing, "Could not start a dash before opening Menu.");
                uint dashReset = input.CaptureSnapshot().DashResetVersion;
                float dashRemaining = dashAbility.CaptureSimulation().Remaining;
                menu.Open();
                Check(menu.IsOpen && !input.InputEnabled, "Opening Menu did not stop local controls.");
                Check(input.CaptureSnapshot().DashResetVersion == dashReset,
                    "Opening Menu reset the accepted dash when hiding touch controls.");
                Check(input.CaptureSnapshot().Move == Vector2.zero && !input.CaptureSnapshot().DashAvailable,
                    "Menu allowed new movement or dash input.");
                until = Time.realtimeSinceStartup + Mathf.Min(0.04f, dashRemaining * 0.25f);
                while (Time.realtimeSinceStartup < until) yield return null;
                Check(dashAbility.IsDashing || dashAbility.DashProgress > 0.99f,
                    "Opening Menu cancelled the dash instead of finishing it.");
                Check(Find(hud, "Dimmer").GetComponent<Image>().color.a == 0.65f,
                    "The menu dimmer was not updated.");
                if (Find(hud, "MenuContent") != null)
                {
                    Check(!Find(hud, "Recipes").GetComponent<Button>().interactable, "Recipes should remain unavailable.");
                    Check(Find(hud, "LiveBlur").GetComponent<RawImage>().material.HasProperty("_MainTex"),
                        "Blur material is missing the texture property required by UI.");
                    Check(Find(hud, "LiveBlur").GetComponent<RawImage>().material.passCount == 1,
                        "Canvas must not execute the renderer's fullscreen blur passes.");
                    var captured = Find(hud, "LiveBlur").GetComponent<RawImage>().texture as RenderTexture;
                    Check(captured != null && captured.width > 1 && captured.height > 1, "The menu has no owned scene capture.");
                    Check(Mathf.Abs((float)captured.width / captured.height
                        - (float)menuCamera.pixelWidth / menuCamera.pixelHeight) < 0.01f, "The menu capture changed the frame aspect.");
                    Check(cameraData.requiresColorOption == colorOption, "Opening the menu changed camera color settings.");
                    until = Time.realtimeSinceStartup + 0.3f;
                    while (Time.realtimeSinceStartup < until) yield return null;
                    Check(hud.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay,
                        "The production pause menu must be checked in overlay mode.");
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, "pause-overlay.png"));
                    until = Time.realtimeSinceStartup + 0.2f;
                    while (Time.realtimeSinceStartup < until) yield return null;
                    Check(Vector3.Distance(menuCamera.transform.position, cameraPosition) < 0.001f
                        && Quaternion.Angle(menuCamera.transform.rotation, cameraRotation) < 0.001f
                        && Mathf.Abs(menuCamera.fieldOfView - cameraFov) < 0.001f
                        && Mathf.Abs(menuCamera.orthographicSize - cameraSize) < 0.001f
                        && Mathf.Abs(menuCamera.aspect - cameraAspect) < 0.001f,
                        "Opening the pause menu changed the camera framing.");
                    foreach (var button in Find(hud, "MenuContent").GetComponentsInChildren<Button>())
                    {
                        var image = button.GetComponent<Image>();
                        Check(image != null && image.isActiveAndEnabled && image.canvasRenderer.GetAlpha() > 0.1f
                            && button.transform.lossyScale.sqrMagnitude > 0.001f,
                            "Pause button was not visible: " + button.name);
                    }
                    CaptureMenu(hud, Path.Combine(directory, "pause-menu.png"));
                    CaptureTexture(Find(hud, "LiveBlur").GetComponent<RawImage>().texture as RenderTexture,
                        Path.Combine(directory, "overlay-source.png"));
                    until = Time.realtimeSinceStartup + 0.2f;
                    while (Time.realtimeSinceStartup < until) yield return null;
                    Find(hud, "Options").GetComponent<Button>().onClick.Invoke();
                    Check(Find(hud, "PauseOptions").gameObject.activeInHierarchy, "Options button did not open settings.");
                    Check(Find(hud, "PauseOptions").GetComponent<MenuVideoOptions>().isActiveAndEnabled,
                        "Pause options were not connected to the existing settings view.");
                    Find(Find(hud, "PauseOptions"), "BackButton").GetComponent<Button>().onClick.Invoke();
                    Check(!Find(hud, "PauseOptions").gameObject.activeSelf && Find(hud, "MenuContent").gameObject.activeInHierarchy,
                        "Back did not return to the pause menu.");
                }
                var content = Find(hud, "MenuContent");
                Check(content.Find("Resume") == null, "The old X close button is still present.");
                Check(content.Find("DismissHint").GetComponent<TMP_Text>().text == "Click or tap outside to close",
                    "The outside-click hint is missing.");
                Canvas.ForceUpdateCanvases();
                var menuPanel = (RectTransform)content.Find("Panel");
                Vector2 inside = RectTransformUtility.WorldToScreenPoint(null,
                    menuPanel.TransformPoint(new Vector3(0, menuPanel.rect.yMax - 8, 0)));
                ClickAt(inside);
                Check(menu.IsOpen, "Clicking blank space inside the menu panel closed it.");
                ClickAt(new Vector2(Screen.width * 0.05f, Screen.height * 0.5f));
                Check(!menu.IsOpen && input.InputEnabled, "Closing Menu did not restore local controls.");
                Check(cameraData.requiresColorOption == colorOption, "Closing Menu did not restore its camera settings.");
                until = Time.realtimeSinceStartup + 0.15f;
                while (Time.realtimeSinceStartup < until) yield return null;
                Check(input.CaptureSnapshot().Move == Vector2.zero, "Touch release left movement held.");
                File.WriteAllText(Path.Combine(directory, "mobile.checked"), "PASS: joystick, simultaneous dash, menu and input release");
            }
            else
            {
                while (!File.Exists(Path.Combine(directory, "mobile.checked"))) yield return null;
                File.WriteAllText(Path.Combine(directory, "host.checked"), "PASS");
                while (manager.ConnectedClients.Count > 1) yield return null;
            }
            if (!host)
                while (!File.Exists(Path.Combine(directory, "host.checked"))) yield return null;
        }

        private static Transform Find(Transform root, string name)
            => root.GetComponentsInChildren<Transform>(true).First(item => item.name == name);
        private static void ClickAt(Vector2 position)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0, "No UI element received the test click.");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        private static void CaptureTexture(RenderTexture target, string path)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                Check(texture.GetPixels32().Any(pixel => pixel.r > 8 || pixel.g > 8 || pixel.b > 8),
                    "The menu's overlay scene capture was empty.");
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Destroy(texture); }
        }
        private static void CaptureMenu(Transform hud, string path)
        {
            // Render without converting overlay UI to camera-space UI, so the capture matches the production path.
            var camera = Camera.main;
            var canvas = hud.GetComponent<Canvas>();
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            float oldAspect = camera.aspect;
            var oldActive = RenderTexture.active;
            int width = camera.pixelWidth, height = camera.pixelHeight;
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                var backdrop = hud.GetComponentInChildren<HudMenuBackdrop>();
                if (backdrop != null) backdrop.RefreshCapture();
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.aspect = oldAspect;
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                var backdrop = hud.GetComponentInChildren<HudMenuBackdrop>();
                if (backdrop != null) backdrop.RefreshCapture();
                RenderTexture.active = oldActive;
                Destroy(texture);
                Destroy(target);
            }
        }
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); }
        private void Finish(string result, int code)
        {
            scenario = null;
            File.WriteAllText(Path.Combine(directory, role + ".result"), result);
            if (manager != null) manager.Shutdown();
            enabled = false;
            Application.Quit(code);
        }
    }
}
#endif
