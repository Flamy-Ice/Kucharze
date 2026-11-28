#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Creates an editable menu scene from the supplied artwork without replacing an existing menu.
[InitializeOnLoad]
public static class MainMenuSceneSetup
{
    public const string ScenePath = ProjectAssetPaths.MenuScene;

    private const string LobbyPath = ProjectAssetPaths.LobbyScene;
    private const string RoundPath = ProjectAssetPaths.GameScene;
    private const string RequestPath = "Assets/Project/Scripts/UI/MainMenu/Editor/MainMenuSetup.request";
    private const string ResultPath = "Assets/Project/Scripts/UI/MainMenu/Editor/MainMenuSetup.result";
    private static Font font;
    private static readonly Color Paper = new Color32(255, 254, 221, 255);
    private static readonly Color Ink = new Color32(36, 33, 54, 255);
    private static readonly Color Teal = new Color32(3, 159, 150, 255);

    static MainMenuSceneSetup() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || BuildPipeline.isBuildingPlayer || !File.Exists(RequestPath)) return;
        string operation = File.ReadAllText(RequestPath).Trim();
        if (!AssetDatabase.DeleteAsset(RequestPath)) File.Delete(RequestPath);
        try
        {
            if (operation == "create") Create();
            else if (operation == "validate") Validate();
            else if (operation == "entrance") MainMenuEntranceSetup.Refresh();
            else if (operation == "entrance-defaults") MainMenuEntranceSetup.Refresh(true);
            else throw new InvalidOperationException("Unknown main menu operation.");
            File.WriteAllText(ResultPath, "SUCCESS: " + operation + "\n" + ScenePath);
        }
        catch (Exception exception) { File.WriteAllText(ResultPath, "FAILED: " + exception); }
    }

    [MenuItem("Tools/Kucharze/Main Menu/Create Main Menu")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("The main menu already exists. Edit its Canvas objects directly.");
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            throw new InvalidOperationException("Save the current untitled scene before creating the main menu.");
        font = AssetDatabase.LoadAssetAtPath<Font>(ProjectAssetPaths.FontSources + "/Jua-Regular.ttf");
        if (font == null) throw new InvalidOperationException("The Jua font is missing.");
        foreach (string name in new[] { "MainMenu_Background.png", "Let it Flush!.png", "Main_Menu_Button_Play.png",
            "MainMenu_Button_Options.png", "MainMenu_Button_Credits.png", "MainMenu_Button_Quit.png" }) PrepareSprite(name);
        GameObject connection = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.NetworkPrefabs + "/LobbyConnection.prefab");
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Project/Scripts/Player/Audio/Mixers/GameAudio.mixer");
        if (connection == null || mixer == null) throw new InvalidOperationException("The lobby connection and GameAudio mixer are required.");
        AudioMixerGroup musicGroup = mixer.FindMatchingGroups("Music").Single();

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("MainMenuCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            camera.cullingMask = 0;
            camera.gameObject.AddComponent<AudioListener>();

            var audio = new GameObject("MenuAudio - assign your music here").AddComponent<MenuAudioSettings>();
            SetReference(audio, "mixer", mixer);
            SetReference(audio, "musicOutput", musicGroup);
            SetString(audio, "menuScene", ScenePath);
            audio.GetComponent<AudioSource>().playOnAwake = false;

            var events = new GameObject("EventSystem").AddComponent<EventSystem>();
            events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Canvas canvas = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1085);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Image background = Picture("WoodBackground", canvas.transform, Sprite("MainMenu_Background.png"), Vector2.zero, Vector2.zero);
            Stretch(background.rectTransform);
            Picture("Let it Flush!", canvas.transform, Sprite("Let it Flush!.png"), new Vector2(0, 375), new Vector2(657, 128));

            var controller = new GameObject("MenuNavigation").AddComponent<MainMenuNavigation>();
            GameObject home = Screen("HomeCanvas", canvas.transform);
            GameObject play = Screen("PlayCanvas", canvas.transform);
            GameObject options = Screen("OptionsCanvas", canvas.transform);
            GameObject credits = Screen("CreditsCanvas", canvas.transform);
            Button optionsButton = ArtButton("Options", home.transform, "MainMenu_Button_Options.png", 174, controller.ShowOptions);
            ArtButton("Credits", home.transform, "MainMenu_Button_Credits.png", 4, controller.ShowCredits);
            Button playButton = ArtButton("Play", home.transform, "Main_Menu_Button_Play.png", -166, controller.ShowPlay);
            ArtButton("Quit", home.transform, "MainMenu_Button_Quit.png", -336, controller.Quit);

            Transform playCard = Card(play.transform, "PlayCard");
            Label("Play", playCard, new Vector2(0, 242), new Vector2(700, 66), 54);
            Label("A private game for 2-4 players", playCard, new Vector2(0, 178), new Vector2(720, 46), 27);
            Button host = PlainButton("Host a game", playCard, new Vector2(0, 100), new Vector2(520, 76), Teal);
            Label("or join your crew", playCard, new Vector2(0, 30), new Vector2(600, 42), 25);
            InputField code = CodeField(playCard, new Vector2(-110, -40));
            Button join = PlainButton("Join", playCard, new Vector2(235, -40), new Vector2(180, 68), Teal);
            Text status = Label("Ready", playCard, new Vector2(0, -128), new Vector2(760, 96), 23);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            Button cancel = PlainButton("Cancel connection", playCard, new Vector2(0, -230), new Vector2(340, 54), new Color(0.45f, 0.28f, 0.27f));
            Button playBack = PlainButton("Back", play.transform, new Vector2(0, -445), new Vector2(250, 62), Ink);
            UnityEventTools.AddPersistentListener(playBack.onClick, controller.ShowHome);
            var online = controller.gameObject.AddComponent<MainMenuOnlinePanel>();
            SetReference(online, "connectionPrefab", connection);
            SetString(online, "lobbyScene", LobbyPath);
            SetString(online, "menuScene", ScenePath);
            SetReference(online, "hostButton", host);
            SetReference(online, "joinButton", join);
            SetReference(online, "cancelButton", cancel);
            SetReference(online, "joinCode", code);
            SetReference(online, "statusLabel", status);
            UnityEventTools.AddPersistentListener(host.onClick, online.Host);
            UnityEventTools.AddPersistentListener(join.onClick, online.Join);
            UnityEventTools.AddPersistentListener(cancel.onClick, online.Cancel);
            cancel.gameObject.SetActive(false);

            Transform optionsCard = Card(options.transform, "OptionsCard");
            Label("Options", optionsCard, new Vector2(0, 230), new Vector2(700, 70), 54);
            Slider music = VolumeSlider("Music", optionsCard, 80, out Text musicValue);
            Slider sounds = VolumeSlider("Sounds", optionsCard, -100, out Text soundsValue);
            Label("Your settings are saved automatically", optionsCard, new Vector2(0, -245), new Vector2(740, 42), 24);
            var audioOptions = options.AddComponent<MenuAudioOptions>();
            SetReference(audioOptions, "musicSlider", music);
            SetReference(audioOptions, "soundsSlider", sounds);
            SetReference(audioOptions, "musicValue", musicValue);
            SetReference(audioOptions, "soundsValue", soundsValue);
            Button optionsBack = PlainButton("Back", options.transform, new Vector2(0, -445), new Vector2(250, 62), Ink);
            UnityEventTools.AddPersistentListener(optionsBack.onClick, controller.ShowHome);

            Transform creditsCard = Card(credits.transform, "CreditsCard");
            Label("Credits", creditsCard, new Vector2(0, 230), new Vector2(700, 70), 54);
            Text[] names = new Text[4];
            Text[] roles = new Text[4];
            for (int index = 0; index < 4; index++)
            {
                float x = index % 2 == 0 ? -215 : 215;
                float y = index < 2 ? 70 : -130;
                Label("TEAM MEMBER " + (index + 1), creditsCard, new Vector2(x, y + 48), new Vector2(390, 30), 20).color = new Color(0.48f, 0.85f, 0.81f);
                names[index] = Label("Your name", creditsCard, new Vector2(x, y), new Vector2(390, 65), 37);
                roles[index] = Label("Your role", creditsCard, new Vector2(x, y - 52), new Vector2(390, 50), 26);
            }
            var team = credits.AddComponent<MenuCredits>();
            SetArray(team, "nameLabels", names);
            SetArray(team, "roleLabels", roles);
            Button creditsBack = PlainButton("Back", credits.transform, new Vector2(0, -445), new Vector2(250, 62), Ink);
            UnityEventTools.AddPersistentListener(creditsBack.onClick, controller.ShowHome);
            SetArray(controller, "screens", new UnityEngine.Object[] { home, play, options, credits });
            SetArray(controller, "firstSelections", new UnityEngine.Object[] { optionsButton, host, music, creditsBack });
            events.firstSelectedGameObject = optionsButton.gameObject;
            controller.ShowHome();
            MainMenuPresentationSetup.ApplyToScene(scene);
            MainMenuNicknameSetup.ApplyToScene(scene);
            MainMenuEntranceSetup.ApplyToScene(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save the main menu scene.");
            var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            foreach (string path in new[] { LobbyPath, RoundPath })
            {
                EditorBuildSettingsScene existing = scenes.FirstOrDefault(item => item.path == path);
                if (existing != null) existing.enabled = true;
                else scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        Validate();
    }

    [MenuItem("Tools/Kucharze/Main Menu/Validate Main Menu")]
    public static void Validate()
    {
        foreach (string path in new[] { ScenePath, LobbyPath, RoundPath })
            if (!File.Exists(path) || !EditorBuildSettings.scenes.Any(item => item.enabled && item.path == path))
                throw new InvalidOperationException("Missing scene or disabled build entry: " + path);
        if (EditorBuildSettings.scenes.First(item => item.enabled).path != ScenePath)
            throw new InvalidOperationException("Main Menu must be the first enabled build scene.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (Transform item in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);
            var buttons = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true)).ToArray();
            if (buttons.Length != 10 || buttons.Any(button => button.onClick.GetPersistentEventCount() != 1
                || button.onClick.GetPersistentTarget(0) == null)) throw new InvalidOperationException("A menu button is not connected.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void PrepareSprite(string name)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(ProjectAssetPaths.MenuImage(name));
        if (importer == null) throw new InvalidOperationException("Missing artwork: " + name);
        // Keep existing sprite IDs and slicing, so other uses of the artwork keep their references.
        importer.textureType = TextureImporterType.Sprite;
        if (importer.spriteImportMode == SpriteImportMode.None) importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(ProjectAssetPaths.MenuImage(name)).OfType<Sprite>().First();

    private static GameObject Screen(string name, Transform parent)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        Stretch(rect);
        rect.gameObject.AddComponent<Canvas>();
        rect.gameObject.AddComponent<GraphicRaycaster>();
        return rect.gameObject;
    }

    private static Transform Card(Transform parent, string name)
    {
        Image card = Surface(name, parent, new Vector2(0, -85), new Vector2(940, 640), new Color32(54, 48, 68, 248));
        card.raycastTarget = false;
        return card.transform;
    }

    private static Button ArtButton(string name, Transform parent, string art, float y, UnityAction action)
    {
        Image picture = Picture(name, parent, Sprite(art), new Vector2(0, y), new Vector2(414, 151));
        Button button = picture.gameObject.AddComponent<Button>();
        picture.raycastTarget = true;
        ConfigureButton(button, picture);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    private static Button PlainButton(string text, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        Image image = Surface(text + "Button", parent, position, size, color);
        Button button = image.gameObject.AddComponent<Button>();
        ConfigureButton(button, image);
        Text label = Label(text, image.transform, Vector2.zero, size - new Vector2(20, 6), 31);
        label.raycastTarget = false;
        return button;
    }

    private static void ConfigureButton(Button button, Graphic graphic)
    {
        button.targetGraphic = graphic;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    private static InputField CodeField(Transform parent, Vector2 position)
    {
        Image background = Surface("JoinCode", parent, position, new Vector2(450, 68), Ink);
        var field = background.gameObject.AddComponent<InputField>();
        field.targetGraphic = background;
        Text value = Label("", background.transform, Vector2.zero, new Vector2(414, 60), 32);
        value.supportRichText = false;
        field.textComponent = value;
        Text placeholder = Label("Enter lobby code", background.transform, Vector2.zero, new Vector2(414, 60), 27);
        placeholder.color = new Color(0.7f, 0.7f, 0.66f);
        field.placeholder = placeholder;
        field.characterLimit = 16;
        field.contentType = InputField.ContentType.Alphanumeric;
        field.lineType = InputField.LineType.SingleLine;
        return field;
    }

    private static Slider VolumeSlider(string name, Transform parent, float y, out Text percentage)
    {
        Label(name, parent, new Vector2(-230, y + 48), new Vector2(240, 48), 36).alignment = TextAnchor.MiddleLeft;
        percentage = Label("100%", parent, new Vector2(265, y + 48), new Vector2(170, 48), 30);
        percentage.alignment = TextAnchor.MiddleRight;
        RectTransform root = Rect(name + "Slider", parent, new Vector2(0, y - 5), new Vector2(700, 52));
        Slider slider = root.gameObject.AddComponent<Slider>();
        Image hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        Surface("Track", root, Vector2.zero, new Vector2(700, 18), Ink).raycastTarget = false;
        RectTransform fillArea = Rect("FillArea", root, Vector2.zero, new Vector2(700, 18));
        Image fill = Surface("Fill", fillArea, Vector2.zero, Vector2.zero, Teal);
        Stretch(fill.rectTransform);
        fill.raycastTarget = false;
        RectTransform handleArea = Rect("HandleArea", root, Vector2.zero, new Vector2(672, 46));
        Image handle = Surface("Handle", handleArea, Vector2.zero, new Vector2(28, 0), Paper);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        return slider;
    }

    private static Image Picture(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Image Surface(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static Text Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        Text label = Rect(string.IsNullOrEmpty(text) ? "InputText" : text, parent, position, size).gameObject.AddComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Paper;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(name).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object target, string name, string value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(name).stringValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var settings = new SerializedObject(target);
        SerializedProperty property = settings.FindProperty(name);
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
