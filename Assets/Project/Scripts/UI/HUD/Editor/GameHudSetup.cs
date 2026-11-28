#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using ModularMovement;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

// Connects the authored Kuba HUD to platform controls and the replicated player roster.
[InitializeOnLoad]
public static class GameHudSetup
{
    private const string Request = "Assets/Project/Scripts/UI/HUD/Editor/HudSetup.request";
    private const string Report = "Assets/Project/Scripts/UI/HUD/Editor/HudSetup.result";
    private const string TintMaterial = "Assets/Project/Gameplay/UI/HUD/Panels/PlayerTint.mat";
    static GameHudSetup() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || BuildPipeline.isBuildingPlayer || !File.Exists(Request)) return;
        string operation = File.ReadAllText(Request).Trim();
        if (!AssetDatabase.DeleteAsset(Request)) File.Delete(Request);
        try
        {
            if (operation == "lobby") SetupLobby();
            else if (operation == "setup") Setup();
            else if (operation == "both") { Setup(); SetupLobby(); }
            else if (operation == "pause") PauseMenuArtworkSetup.Setup();
            else if (operation == "capture") HudMenuCaptureSetup.Install();
            else if (operation == "dismissal") PauseMenuArtworkSetup.UpdateDismissal();
            else throw new InvalidOperationException("Unknown HUD operation.");
            File.WriteAllText(Report, operation == "pause" ? "SUCCESS: imported pause artwork, saved both menus and updated credits."
                : operation == "capture" ? "SUCCESS: installed menu capture on PC and Mobile renderers."
                : operation == "dismissal" ? "SUCCESS: saved outside-click dismissal and hint in Lobby and Kuba."
                : "SUCCESS: saved HUD, platform checks, four player slots and desktop/mobile previews.");
        }
        catch (Exception exception) { File.WriteAllText(Report, "FAILED: " + exception); }
    }

    [MenuItem("Tools/Kucharze/HUD/Copy HUD To Lobby")]
    public static void SetupLobby()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene game = SceneManager.GetSceneByPath(ProjectAssetPaths.GameScene);
        Scene lobby = SceneManager.GetSceneByPath(ProjectAssetPaths.LobbyScene);
        bool openedGame = !game.IsValid() || !game.isLoaded;
        bool openedLobby = !lobby.IsValid() || !lobby.isLoaded;
        if ((!openedGame && game.isDirty) || (!openedLobby && lobby.isDirty))
            throw new InvalidOperationException("Save Kuba and Lobby before copying the HUD.");
        if (openedGame) game = EditorSceneManager.OpenScene(ProjectAssetPaths.GameScene, OpenSceneMode.Additive);
        if (openedLobby) lobby = EditorSceneManager.OpenScene(ProjectAssetPaths.LobbyScene, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(lobby);
            var source = game.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HudPlayerRoster>(true)).Single();
            var roots = lobby.GetRootGameObjects();
            Transform ui = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Single(item => item.name == "UI");
            Transform systems = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Single(item => item.name == "SceneSystems");
            var existing = ui.GetComponentsInChildren<HudPlayerRoster>(true).SingleOrDefault();
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var copy = UnityEngine.Object.Instantiate(source.gameObject, ui);
            copy.name = "PlayerHUD";
            copy.GetComponent<Canvas>().sortingOrder = 20;
            UnityEngine.Object.DestroyImmediate(Find(copy.transform, "Timer_Background").gameObject);
            var wallet = systems.GetComponentsInChildren<CoinWallet>(true).SingleOrDefault();
            if (wallet == null)
            {
                wallet = new GameObject("LobbyCoins", typeof(CoinWallet), typeof(CoinWalletNetwork)).GetComponent<CoinWallet>();
                wallet.transform.SetParent(systems, false);
            }
            Reference(copy.GetComponentInChildren<CoinDisplay>(true), "wallet", wallet);
            if (!ui.GetComponentsInChildren<EventSystem>(true).Any())
            {
                var sourceEvents = game.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).Single();
                var events = UnityEngine.Object.Instantiate(sourceEvents.gameObject, ui);
                events.name = "EventSystem";
            }
            if (Find(copy.transform, "Player_Section").childCount != 4 || Find(copy.transform, "Mobile") == null
                || copy.GetComponent<HudGameMenu>() == null || ui.GetComponentsInChildren<EventSystem>(true).Length != 1)
                throw new InvalidOperationException("Lobby HUD copy is incomplete.");
            if (ui.GetComponentsInChildren<LobbyCodeDisplay>(true).Length != 1)
                throw new InvalidOperationException("The original lobby code display must remain.");
            if (!EditorSceneManager.SaveScene(lobby)) throw new IOException("Could not save Lobby.");
            wallet.GetComponent<NetworkObject>().SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
            EditorSceneManager.SaveScene(lobby);
            AssetDatabase.SaveAssets();
            Preview(copy.transform, false, 1280, 720, "lobby-desktop");
            Preview(copy.transform, true, 1600, 720, "lobby-android");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (openedLobby) EditorSceneManager.CloseScene(lobby, true);
            if (openedGame) EditorSceneManager.CloseScene(game, true);
        }
    }

    [MenuItem("Tools/Kucharze/HUD/Configure Kuba HUD")]
    public static void Setup()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ProjectAssetPaths.GameScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save Kuba before applying the HUD setup.");
        if (opened) scene = EditorSceneManager.OpenScene(ProjectAssetPaths.GameScene, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            Transform hud = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Single(canvas => canvas.name == "HUD").transform;
            Apply(hud);
            Validate(hud);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Kuba.");
            AssetDatabase.SaveAssets();
            Preview(hud, false, 1280, 720, "desktop");
            Preview(hud, true, 1600, 720, "android-wide");
            Preview(hud, true, 1280, 800, "android-tablet");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static T Component<T>(GameObject target) where T : UnityEngine.Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }

    private static Transform Find(Transform root, string name)
        => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);

    private static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ArrayReference(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void Apply(Transform hud)
    {
        var safe = Find(hud, "SafeArea") as RectTransform;
        if (safe == null)
        {
            safe = new GameObject("SafeArea", typeof(RectTransform), typeof(HudSafeArea)).GetComponent<RectTransform>();
            safe.SetParent(hud, false);
        }
        Stretch(safe);
        foreach (string name in new[] { "Money_Background", "Player_Section", "Mobile" })
            Find(hud, name).SetParent(safe, false);
        Transform mobile = Find(hud, "Mobile");
        Stretch((RectTransform)mobile);
        var platform = Component<PlatformUI>(hud.gameObject);
        Reference(platform, "mobileControls", mobile.gameObject);
        var platformData = new SerializedObject(platform);
        platformData.FindProperty("editorPreview").enumValueIndex = 0;
        platformData.ApplyModifiedPropertiesWithoutUndo();

        // Hide the clock's graphics while its authoritative timer continues to run.
        var timerGroup = Component<CanvasGroup>(Find(hud, "Timer_Background").gameObject);
        timerGroup.alpha = 0;
        timerGroup.interactable = timerGroup.blocksRaycasts = false;

        Place((RectTransform)Find(hud, "Money_Background"), Vector2.one, Vector2.one, new Vector2(-25, -25), new Vector2(337, 132));
        var money = Find(hud, "Money").GetComponent<TMP_Text>();
        money.enableAutoSizing = true;
        money.fontSizeMin = 30;
        money.fontSizeMax = 80;
        Place(money.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-120, 0), new Vector2(190, 100));
        money.alignment = TextAlignmentOptions.MidlineRight;
        Transform section = Find(hud, "Player_Section");
        Place((RectTransform)section, Vector2.one, Vector2.one, new Vector2(-25, -173), new Vector2(337, 248));
        var layout = section.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperRight;
        layout.spacing = 8;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var template = section.Find("Player1_Background").GetComponent<Image>();
        var material = AssetDatabase.LoadAssetAtPath<Material>(TintMaterial);
        if (material == null)
        {
            var shader = Shader.Find("Kucharze/UI/Player Tint");
            if (shader == null) throw new InvalidOperationException("The player tint shader has not imported.");
            material = new Material(shader) { name = "PlayerTint" };
            AssetDatabase.CreateAsset(material, TintMaterial);
        }
        var backgrounds = new Image[4];
        var labels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var row = section.Find("Player" + (i + 1) + "_Background");
            if (row == null)
            {
                row = UnityEngine.Object.Instantiate(template, section).transform;
                row.name = "Player" + (i + 1) + "_Background";
            }
            row.SetSiblingIndex(i);
            row.gameObject.SetActive(true);
            Place((RectTransform)row, Vector2.one, Vector2.one, new Vector2(0, -i * 64), new Vector2(300, 56));
            backgrounds[i] = row.GetComponent<Image>();
            backgrounds[i].sprite = template.sprite;
            backgrounds[i].material = material;
            backgrounds[i].color = LobbyPlayerColors.Get(i);
            backgrounds[i].raycastTarget = false;
            labels[i] = row.GetComponentInChildren<TMP_Text>(true);
            Stretch(labels[i].rectTransform);
            labels[i].rectTransform.offsetMin = new Vector2(30, 5);
            labels[i].rectTransform.offsetMax = new Vector2(-18, -5);
            labels[i].text = "Player " + (i + 1);
            labels[i].fontSize = 32;
            labels[i].enableAutoSizing = true;
            labels[i].fontSizeMin = 20;
            labels[i].fontSizeMax = 32;
            labels[i].alignment = TextAlignmentOptions.MidlineRight;
            labels[i].overflowMode = TextOverflowModes.Ellipsis;
            labels[i].richText = false;
            labels[i].raycastTarget = false;
            labels[i].color = new Color(1f, 0.98f, 0.92f);
            var nicknameFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Shared/UI/Fonts/TextMeshPro/NicknameFont.asset");
            if (nicknameFont != null)
            {
                labels[i].font = nicknameFont;
                const string textMaterialPath = "Assets/Project/Gameplay/UI/HUD/Panels/NicknameText.mat";
                var textMaterial = AssetDatabase.LoadAssetAtPath<Material>(textMaterialPath);
                if (textMaterial == null)
                {
                    textMaterial = new Material(nicknameFont.material) { name = "NicknameText" };
                    textMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.035f, 0.045f, 0.065f));
                    textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.16f);
                    textMaterial.EnableKeyword("OUTLINE_ON");
                    AssetDatabase.CreateAsset(textMaterial, textMaterialPath);
                }
                labels[i].fontSharedMaterial = textMaterial;
            }
        }
        var roster = Component<HudPlayerRoster>(hud.gameObject);
        ArrayReference(roster, "backgrounds", backgrounds);
        ArrayReference(roster, "nicknames", labels);

        Find(hud, "Pick Up Icon").gameObject.SetActive(false);
        Find(hud, "Interact Icon").gameObject.SetActive(false);
        Transform dash = Find(hud, "Dash Icon");
        Place((RectTransform)dash, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 32), new Vector2(168, 168));
        Component<OnScreenButton>(dash.gameObject).controlPath = "<Gamepad>/buttonEast";
        var joystick = (RectTransform)Find(hud, "Joystick");
        joystick.localScale = Vector3.one;
        joystick.anchorMin = Vector2.zero;
        joystick.anchorMax = new Vector2(0.48f, 0.66f);
        joystick.offsetMin = joystick.offsetMax = Vector2.zero;
        ((RectTransform)Find(hud, "Joystick Body")).sizeDelta = new Vector2(240, 240);
        ((RectTransform)Find(hud, "Joystick Move")).sizeDelta = new Vector2(100, 100);
        var joystickSettings = new SerializedObject(joystick.GetComponent<FloatingJoystick>());
        joystickSettings.FindProperty("movementRadius").floatValue = 120f;
        joystickSettings.ApplyModifiedPropertiesWithoutUndo();
        Transform menuButton = Find(hud, "Menu");
        Place((RectTransform)menuButton, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -20), new Vector2(110, 132));
        SetupMenu(hud, safe, menuButton.GetComponent<Button>(), joystick.gameObject, Find(hud, "Interaction Buttons").gameObject);
    }

    private static void SetupMenu(Transform hud, Transform safe, Button open, params GameObject[] controls)
    {
        Transform panel = Find(hud, "GameMenu");
        if (panel == null)
        {
            panel = new GameObject("GameMenu", typeof(RectTransform), typeof(Image)).transform;
            panel.SetParent(safe, false);
            Stretch((RectTransform)panel);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.93f);
            TMP_FontAsset font = Find(hud, "Nickname").GetComponent<TMP_Text>().font;
            Text(panel, "Title", "MENU", font, new Vector2(0, 130), new Vector2(600, 70), 50);
            Text(panel, "Notice", "The online game continues while this menu is open.", font, new Vector2(0, 63), new Vector2(900, 46), 25);
            MenuButton(panel, "Resume", "RESUME", font, new Vector2(0, -15));
            MenuButton(panel, "Leave", "LEAVE GAME", font, new Vector2(0, -105));
            Text(panel, "LeaveNotice", "Leaving as host ends the session for everyone.", font, new Vector2(0, -183), new Vector2(900, 46), 24);
        }
        var controller = Component<HudGameMenu>(hud.gameObject);
        Reference(controller, "panel", panel.gameObject);
        Reference(controller, "menuButton", open);
        Button dismiss = panel.Find("Dimmer") != null ? panel.Find("Dimmer").GetComponent<Button>() : null;
        Reference(controller, "resumeButton", dismiss != null ? dismiss : Find(panel, "Resume").GetComponent<Button>());
        Reference(controller, "leaveButton", Find(panel, "Leave").GetComponent<Button>());
        ArrayReference(controller, "touchControls", controls);
        panel.gameObject.SetActive(false);
    }

    private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, Vector2 position, Vector2 size, int fontSize)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        Place(text.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void MenuButton(Transform parent, string name, string value, TMP_FontAsset font, Vector2 position)
    {
        var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(parent, false);
        Place((RectTransform)button.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, position, new Vector2(410, 72));
        button.targetGraphic = button.GetComponent<Image>();
        button.GetComponent<Image>().color = new Color(0.16f, 0.24f, 0.31f);
        Stretch(Text(button.transform, "Label", value, font, Vector2.zero, Vector2.zero, 32).rectTransform);
    }

    private static void Validate(Transform hud)
    {
        if (new SerializedObject(hud.GetComponent<PlatformUI>()).FindProperty("editorPreview").enumValueIndex != 0)
            throw new InvalidOperationException("HUD platform must be automatic.");
        if (Find(hud, "Player_Section").childCount != 4 || hud.GetComponent<HudPlayerRoster>() == null)
            throw new InvalidOperationException("HUD needs four roster rows.");
        if (Find(hud, "Dash Icon").GetComponent<OnScreenButton>().controlPath != "<Gamepad>/buttonEast")
            throw new InvalidOperationException("Dash control does not match PlayerControls.");
        if (!Find(hud, "Time").gameObject.activeInHierarchy || Find(hud, "Timer_Background").GetComponent<CanvasGroup>().alpha != 0)
            throw new InvalidOperationException("Hide only the timer graphics, not its logic.");
        if (Find(hud, "GameMenu").gameObject.activeSelf) throw new InvalidOperationException("Menu starts closed.");
        if (ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Material>(TintMaterial).shader))
            throw new InvalidOperationException("Player color shader failed to compile.");
    }

    private static void Preview(Transform source, bool mobile, int width, int height, string suffix)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            var copy = UnityEngine.Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(copy, scene);
            copy.transform.localScale = Vector3.one;
            Find(copy.transform, "Mobile").gameObject.SetActive(mobile);
            Find(copy.transform, "Money").GetComponent<TMP_Text>().text = "125$";
            var labels = Find(copy.transform, "Player_Section").GetComponentsInChildren<TMP_Text>(true);
            string[] names = { "Flamy", "Łukasz_Żółć", "Chef Aleksandra", "Kuba" };
            for (int i = 0; i < labels.Length; i++) labels[i].text = names[i];
            var camera = new GameObject("HudPreviewCamera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.105f, 0.12f);
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            Canvas canvas = copy.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Report + "." + suffix + ".png", texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
