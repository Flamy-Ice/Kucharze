#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using ModularMovement;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds the nickname field and player label without rebuilding the existing menu or lobby.
[InitializeOnLoad]
public static class MainMenuNicknameSetup
{
    private const string Request = "Assets/Project/Scripts/UI/MainMenu/Editor/NicknameSetup.request";
    private const string Result = "Assets/Project/Scripts/UI/MainMenu/Editor/NicknameSetup.result";
    private const string LabelMaterial = "Assets/Project/Placeholders/Lobby/Characters/Materials/NicknameLabel.mat";
    private const string NicknameFont = "Assets/Project/Shared/UI/Fonts/TextMeshPro/NicknameFont.asset";

    static MainMenuNicknameSetup() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || !File.Exists(Request)) return;
        if (!AssetDatabase.DeleteAsset(Request)) File.Delete(Request);
        try
        {
            Setup();
            File.WriteAllText(Result, "SUCCESS: nickname setup, validation and menu preview");
        }
        catch (Exception exception) { File.WriteAllText(Result, "FAILED: " + exception); }
    }

    [MenuItem("Tools/Kucharze/Main Menu/Setup Player Nicknames")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ProjectAssetPaths.MenuScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save Main Menu before setting up nicknames.");
        if (opened) scene = EditorSceneManager.OpenScene(ProjectAssetPaths.MenuScene, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            ApplyToScene(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Main Menu.");
            AssetDatabase.SaveAssets();
            PreviewMenu(scene);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void ApplyToScene(Scene scene)
    {
        SetupMenu(scene);
        SetupPlayer();
        Validate(scene);
    }

    private static void SetupMenu(Scene scene)
    {
        Transform card = Find(scene, "PlayCard");
        if (card == null) throw new InvalidOperationException("The Play card is missing.");
        Text title = card.GetComponentsInChildren<Text>(true).First(item => item.name == "Play");
        Place(title.rectTransform, new Vector2(0, 315), new Vector2(820, 56));
        title.fontSize = 50;
        Transform group = card.Find("PlayerProfile");
        if (group == null)
        {
            group = new GameObject("PlayerProfile", typeof(RectTransform)).transform;
            group.SetParent(card, false);
            Stretch((RectTransform)group);
        }
        InputField field = group.GetComponentInChildren<InputField>(true);
        if (field == null)
        {
            InputField source = Find(scene, "JoinCode").GetComponent<InputField>();
            field = UnityEngine.Object.Instantiate(source, group);
            field.name = "Nickname";
            field.onValueChanged = new InputField.OnChangeEvent();
            field.onEndEdit = new InputField.EndEditEvent();
        }
        Place(field.GetComponent<RectTransform>(), new Vector2(90, 240), new Vector2(560, 60));
        field.contentType = InputField.ContentType.Standard;
        field.lineType = InputField.LineType.SingleLine;
        field.characterLimit = PlayerNicknameProfile.MaximumLength;
        field.text = "";
        field.textComponent.supportRichText = false;
        field.textComponent.font = AssetDatabase.LoadAssetAtPath<Font>(ProjectAssetPaths.FontSources + "/MadimiOne-Regular.ttf");
        field.textComponent.fontSize = 28;
        Text placeholder = (Text)field.placeholder;
        placeholder.text = "Your nickname (2-16 characters)";
        placeholder.supportRichText = false;
        placeholder.font = field.textComponent.font;
        placeholder.fontSize = 24;
        Transform surface = field.transform.Find("CodeSurface");
        if (surface != null) Stretch((RectTransform)surface);
        Text caption = Find(scene, "NicknameLabel")?.GetComponent<Text>();
        if (caption == null) caption = Find(scene, "A private game for 2-4 players").GetComponent<Text>();
        caption.name = "NicknameLabel";
        caption.transform.SetParent(group, false);
        caption.text = "NICKNAME";
        caption.fontSize = 25;
        caption.alignment = TextAnchor.MiddleLeft;
        Place(caption.rectTransform, new Vector2(-325, 240), new Vector2(190, 40));
        MenuPlayerNickname profile = group.GetComponent<MenuPlayerNickname>();
        if (profile == null) profile = group.gameObject.AddComponent<MenuPlayerNickname>();
        SetReference(profile, "input", field);
        MainMenuOnlinePanel online = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MainMenuOnlinePanel>(true)).Single();
        SetReference(online, "nickname", profile);
        var settings = new SerializedObject(online);
        ((Button)settings.FindProperty("hostButton").objectReferenceValue).interactable = false;
        ((Button)settings.FindProperty("joinButton").objectReferenceValue).interactable = false;
        ((Text)settings.FindProperty("statusLabel").objectReferenceValue).text = "Enter your nickname (2-16 characters) to host or join.";
        MainMenuNavigation navigation = online.GetComponent<MainMenuNavigation>();
        var focus = new SerializedObject(navigation);
        focus.FindProperty("firstSelections").GetArrayElementAtIndex(1).objectReferenceValue = field;
        focus.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void SetupPlayer()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ProjectAssetPaths.LobbyPlayer);
        try
        {
            if (root.GetComponent<LobbyPlayerNickname>() == null) root.AddComponent<LobbyPlayerNickname>();
            LobbyPlayerNameplate view = root.GetComponent<LobbyPlayerNameplate>();
            if (view == null) view = root.AddComponent<LobbyPlayerNameplate>();
            Transform existing = root.transform.Find("NicknameLabel");
            TextMeshPro label;
            if (existing == null)
            {
                var item = new GameObject("NicknameLabel", typeof(RectTransform), typeof(TextMeshPro));
                item.transform.SetParent(root.transform, false);
                label = item.GetComponent<TextMeshPro>();
            }
            else label = existing.GetComponent<TextMeshPro>();
            TMP_FontAsset font = GetNicknameFont();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(LabelMaterial);
            if (material == null)
            {
                material = new Material(font.material) { name = "NicknameLabel" };
                material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.10f, 0.09f, 0.15f));
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
                material.EnableKeyword("OUTLINE_ON");
                AssetDatabase.CreateAsset(material, LabelMaterial);
            }
            label.font = font;
            label.fontSharedMaterial = material;
            label.text = "";
            label.richText = false;
            label.color = new Color(1, 0.99f, 0.90f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 3.5f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1.8f;
            label.fontSizeMax = 3.5f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(4.2f, 0.65f);
            label.transform.localPosition = new Vector3(0, 2.3f, 0);
            label.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            label.GetComponent<MeshRenderer>().receiveShadows = false;
            SetReference(view, "label", label);
            var settings = new SerializedObject(view);
            settings.FindProperty("lobbyScene").stringValue = ProjectAssetPaths.LobbyScene;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ProjectAssetPaths.LobbyPlayer);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static TMP_FontAsset GetNicknameFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NicknameFont);
        if (font != null) return font;
        Font source = AssetDatabase.LoadAssetAtPath<Font>(ProjectAssetPaths.FontSources + "/MadimiOne-Regular.ttf");
        if (source == null) throw new InvalidOperationException("The nickname font is missing.");
        font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "NicknameFont";
        if (!font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 _-.ąćęłńóśźżĄĆĘŁŃÓŚŹŻ"))
            throw new InvalidOperationException("The nickname font cannot render Polish names.");
        AssetDatabase.CreateAsset(font, NicknameFont);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (Texture2D atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        return font;
    }

    private static void Validate(Scene scene)
    {
        if (PlayerNicknameProfile.Normalize("  Kuba   Chef  ") != "Kuba Chef"
            || PlayerNicknameProfile.Normalize("Łukasz_Żółć") != "Łukasz_Żółć"
            || PlayerNicknameProfile.Normalize(new string('ą', 40)).Length != 16
            || PlayerNicknameProfile.IsValid("  ") || PlayerNicknameProfile.IsValid("A")
            || PlayerNicknameProfile.Normalize("A\nB<>🙂") != "AB")
            throw new InvalidOperationException("Nickname validation rules failed.");
        var profile = Find(scene, "PlayerProfile").GetComponent<MenuPlayerNickname>();
        var input = profile.GetComponentInChildren<InputField>(true);
        if (input == null || input.characterLimit != 16 || input.contentType != InputField.ContentType.Standard)
            throw new InvalidOperationException("The nickname field is not configured.");
        var online = Find(scene, "MenuNavigation").GetComponent<MainMenuOnlinePanel>();
        if (new SerializedObject(online).FindProperty("nickname").objectReferenceValue != profile)
            throw new InvalidOperationException("The connection panel has no nickname input.");
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.LobbyPlayer);
        if (player.GetComponent<LobbyPlayerNickname>() == null || player.GetComponent<LobbyPlayerNameplate>() == null
            || player.transform.Find("NicknameLabel").GetComponent<TextMeshPro>() == null)
            throw new InvalidOperationException("The player has no nickname replication or label.");
    }

    private static void PreviewMenu(Scene scene)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            GameObject canvasObject = UnityEngine.Object.Instantiate(Find(scene, "MainMenuCanvas").gameObject);
            SceneManager.MoveGameObjectToScene(canvasObject, preview);
            foreach (Transform item in canvasObject.GetComponentsInChildren<Transform>(true))
                if (new[] { "HomeCanvas", "PlayCanvas", "OptionsCanvas", "CreditsCanvas" }.Contains(item.name))
                    item.gameObject.SetActive(item.name == "PlayCanvas");
            Camera camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camera.gameObject, preview);
            camera.scene = preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.targetTexture = target;
            camera.aspect = 1280f / 720;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Result + ".png", texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static Transform Find(Scene scene, string name)
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name);

    private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
#endif
