#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Restyles the existing menu while keeping its buttons, audio assignments and network references.
public static class MainMenuPresentationSetup
{
    private const string GroupPath = "Assets/Project/UI/MainMenu/Artwork/MainMenu_Group.png";
    private static readonly Color Ink = new Color32(37, 33, 56, 255);
    private static readonly Color Purple = new Color32(75, 71, 86, 255);
    private static readonly Color Paper = new Color32(255, 254, 221, 255);
    private static readonly Color Teal = new Color32(5, 158, 148, 255);
    private static readonly Color Track = new Color32(213, 207, 225, 255);

    [MenuItem("Tools/Kucharze/Main Menu/Refresh Main Menu Appearance")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(MainMenuSceneSetup.ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save your menu changes before refreshing its appearance.");
        if (opened) scene = EditorSceneManager.OpenScene(MainMenuSceneSetup.ScenePath, OpenSceneMode.Additive);
        try
        {
            ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void ApplyToScene(Scene scene)
    {
        Font heading = AssetDatabase.LoadAssetAtPath<Font>("Assets/Project/UI/Fonts/Sources/Jua-Regular.ttf");
        Font body = AssetDatabase.LoadAssetAtPath<Font>("Assets/Project/UI/Fonts/Sources/MadimiOne-Regular.ttf");
        var importer = (TextureImporter)AssetImporter.GetAtPath(GroupPath);
        if (importer != null && (importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed))
        {
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        Sprite group = AssetDatabase.LoadAllAssetsAtPath(GroupPath).OfType<Sprite>().Single();
        if (heading == null || body == null) throw new InvalidOperationException("The menu needs both Jua and Madimi One.");
        Transform canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(item => item.name == "MainMenuCanvas");
        canvas.Find("WoodBackground").GetComponent<Image>().preserveAspect = false;
        Transform home = canvas.Find("HomeCanvas");
        Transform play = canvas.Find("PlayCanvas");
        Transform options = canvas.Find("OptionsCanvas");
        Transform credits = canvas.Find("CreditsCanvas");
        Image homeGroup;
        Transform existing = home.Find("MainMenu_Group");
        if (existing != null) homeGroup = existing.GetComponent<Image>();
        else
        {
            var item = new GameObject("MainMenu_Group", typeof(RectTransform), typeof(Image));
            item.transform.SetParent(home, false);
            homeGroup = item.GetComponent<Image>();
        }
        Panel(homeGroup, group, new Vector2(0, -85), new Vector2(760, 758));
        homeGroup.transform.SetAsFirstSibling();

        foreach (Transform screen in new[] { play, options, credits })
        {
            Transform card = screen.Cast<Transform>().Single(item => item.name.EndsWith("Card", StringComparison.Ordinal));
            Panel(card.GetComponent<Image>(), group, new Vector2(0, -85), new Vector2(1000, 758));
            // Keep the panel's existing children so all saved UI references stay valid.
            foreach (Text label in card.GetComponentsInChildren<Text>(true))
            {
                label.font = body;
                label.color = Ink;
            }
            Text title = card.Cast<Transform>().Select(item => item.GetComponent<Text>()).First(label => label != null);
            title.font = heading;
            title.fontSize = 54;
            Place(title.rectTransform, new Vector2(0, 300), new Vector2(820, 70));
            Transform back = screen.Find("BackButton") ?? card.Find("BackButton");
            back.SetParent(card, false);
            Place((RectTransform)back, new Vector2(0, -320), new Vector2(250, 91));
            StyleButton(back.GetComponent<Button>(), Purple, heading, 32);
            // Button labels follow the resized button, including when a future sprite replaces its background.
            Stretch(back.GetComponentInChildren<Text>().rectTransform, 12);
        }

        StylePlay(play.Find("PlayCard"), heading, body);
        StyleOptions(options.Find("OptionsCard"), heading, body);
        StyleCredits(credits, group, heading, body);
        MainMenuOptionsSetup.ApplyToScene(scene, heading, body);
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            if (button.GetComponent<MenuSelectableScale>() == null) button.gameObject.AddComponent<MenuSelectableScale>();
        MainMenuFeedbackSetup.ApplyToScene(scene);
        MainMenuArtworkSetup.ApplyToScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void StylePlay(Transform card, Font heading, Font body)
    {
        Text subtitle = card.Find("A private game for 2-4 players").GetComponent<Text>();
        Place(subtitle.rectTransform, new Vector2(0, 240), new Vector2(820, 40));
        subtitle.font = body;
        subtitle.fontSize = 27;
        Image hostPanel = Surface(card, "HostPanel", new Color32(205, 235, 215, 255), new Vector2(-235, 35), new Vector2(450, 340));
        Image joinPanel = Surface(card, "JoinPanel", new Color32(224, 216, 241, 255), new Vector2(235, 35), new Vector2(450, 340));
        hostPanel.transform.SetAsFirstSibling();
        joinPanel.transform.SetAsFirstSibling();
        Caption(hostPanel.transform, "Heading", "CREATE A LOBBY", heading, 29, new Vector2(0, 125), new Vector2(410, 48));
        Caption(hostPanel.transform, "Description", "Start a private game\nand share the lobby code.", body, 25, new Vector2(0, 58), new Vector2(410, 74));
        Caption(joinPanel.transform, "Heading", "JOIN YOUR CREW", heading, 29, new Vector2(0, 129), new Vector2(410, 44));
        Caption(joinPanel.transform, "Description", "Enter the code shared\nby your host.", body, 25, new Vector2(0, 65), new Vector2(410, 74));
        Button host = card.Find("Host a gameButton").GetComponent<Button>();
        Place((RectTransform)host.transform, new Vector2(-235, -35), new Vector2(350, 128));
        StyleButton(host, Teal, heading, 34);
        host.GetComponentInChildren<Text>().text = "Host Game";
        Stretch(host.GetComponentInChildren<Text>().rectTransform, 16);
        card.Find("or join your crew").gameObject.SetActive(false);
        InputField input = card.Find("JoinCode").GetComponent<InputField>();
        Caption(card, "CodeFieldLabel", "LOBBY CODE", heading, 20, new Vector2(235, 47), new Vector2(360, 24));
        Place((RectTransform)input.transform, new Vector2(235, -4), new Vector2(360, 66));
        Image field = input.GetComponent<Image>();
        field.color = Color.clear;
        input.transition = Selectable.Transition.None;
        MenuRoundedPanel codeSurface = RoundedSurface(input.transform, "CodeSurface", Paper, Vector2.zero, new Vector2(360, 66));
        codeSurface.transform.SetAsFirstSibling();
        input.textComponent.font = heading;
        input.textComponent.fontSize = 32;
        input.textComponent.color = Ink;
        input.textComponent.alignment = TextAnchor.MiddleCenter;
        Stretch(input.textComponent.rectTransform, 16);
        input.textComponent.rectTransform.offsetMin = new Vector2(16, 6);
        input.textComponent.rectTransform.offsetMax = new Vector2(-16, -6);
        input.customCaretColor = true;
        input.caretColor = Ink;
        input.selectionColor = new Color(0.02f, 0.62f, 0.58f, 0.3f);
        Text placeholder = (Text)input.placeholder;
        placeholder.font = body;
        placeholder.fontSize = 26;
        placeholder.fontStyle = FontStyle.Normal;
        placeholder.text = "e.g. ABC123";
        placeholder.alignment = TextAnchor.MiddleCenter;
        placeholder.color = new Color32(121, 112, 132, 255);
        Stretch(placeholder.rectTransform, 16);
        placeholder.rectTransform.offsetMin = new Vector2(16, 6);
        placeholder.rectTransform.offsetMax = new Vector2(-16, -6);
        Button join = card.Find("JoinButton").GetComponent<Button>();
        Place((RectTransform)join.transform, new Vector2(235, -82), new Vector2(220, 80));
        StyleButton(join, Teal, heading, 30);
        join.GetComponentInChildren<Text>().text = "Join Game";
        Stretch(join.GetComponentInChildren<Text>().rectTransform, 10);
        MenuRoundedPanel statusSurface = RoundedSurface(card, "ConnectionStatusSurface", new Color32(53, 48, 68, 255), new Vector2(0, -211), new Vector2(900, 110));
        statusSurface.transform.SetAsFirstSibling();
        Text statusHeading = Caption(card, "ConnectionStatusHeading", "CONNECTION STATUS", heading, 19, new Vector2(-130, -177), new Vector2(540, 25));
        statusHeading.alignment = TextAnchor.MiddleLeft;
        statusHeading.color = new Color32(128, 225, 208, 255);
        var online = new SerializedObject(card.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MainMenuOnlinePanel>(true)).Single());
        Text status = (Text)online.FindProperty("statusLabel").objectReferenceValue;
        Place(status.rectTransform, new Vector2(-130, -226), new Vector2(540, 62));
        status.font = body;
        status.color = Paper;
        status.fontSize = 24;
        status.alignment = TextAnchor.MiddleLeft;
        status.resizeTextForBestFit = true;
        status.resizeTextMinSize = 18;
        status.resizeTextMaxSize = 24;
        Transform oldBadge = card.Find("PrivateLobbyBadge");
        if (oldBadge != null) oldBadge.gameObject.SetActive(false);
        Button cancel = card.Find("Cancel connectionButton").GetComponent<Button>();
        Place((RectTransform)cancel.transform, new Vector2(300, -215), new Vector2(260, 68));
        StyleButton(cancel, new Color32(248, 169, 140, 255), heading, 24);
        Text cancelLabel = cancel.GetComponentInChildren<Text>(true);
        cancelLabel.text = "Cancel Connection";
        cancelLabel.color = new Color32(126, 61, 34, 255);
        Stretch(cancelLabel.rectTransform, 8);
        cancel.transform.SetAsLastSibling();
    }

    private static void StyleOptions(Transform card, Font heading, Font body)
    {
        foreach (Slider slider in card.GetComponentsInChildren<Slider>(true))
        {
            Image track = slider.transform.Find("Track").GetComponent<Image>();
            Place(track.rectTransform, Vector2.zero, new Vector2(700, 18));
            track.color = Track;
            // The fill must cover the full track at 100%, rather than leave an empty strip at its start.
            Place((RectTransform)slider.transform.Find("FillArea"), Vector2.zero, new Vector2(700, 18));
            slider.fillRect.GetComponent<Image>().color = Teal;
            Place((RectTransform)slider.transform.Find("HandleArea"), Vector2.zero, new Vector2(672, 46));
            slider.handleRect.sizeDelta = new Vector2(28, 0);
            slider.handleRect.GetComponent<Image>().color = Purple;
            // A refresh applies the current value without changing the saved audio setting.
            slider.SetValueWithoutNotify(slider.value);
            Text caption = card.Find(slider.name == "MusicSlider" ? "Music" : "Sounds").GetComponent<Text>();
            caption.font = heading;
        }
        Text hint = card.Find("Your settings are saved automatically").GetComponent<Text>();
        hint.font = body;
        hint.fontSize = 25;
        Place(hint.rectTransform, new Vector2(0, -210), new Vector2(840, 44));
    }

    private static void StyleCredits(Transform screen, Sprite group, Font heading, Font body)
    {
        Transform card = screen.Find("CreditsCard");
        foreach (Transform child in card.Cast<Transform>().Where(item => item.name.StartsWith("TEAM MEMBER ", StringComparison.Ordinal)).ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        var settings = new SerializedObject(screen.GetComponent<MenuCredits>());
        string[] names = { "Jakub Lewandowski", "Mateusz Filipczak", "Adriana Frankowska", "Szymon Wosiek" };
        string[] roles = { "2D Graphics\n3D Graphics\nProgrammer\nUI/UX", "Level Design\nSounds", "2D Graphics\n3D Graphics\nAnimations\nUI/UX", "3D Graphics\nVFX" };
        Color[] tints = { new Color32(200, 239, 224, 255), new Color32(247, 212, 188, 255),
            new Color32(227, 217, 255, 255), new Color32(210, 233, 255, 255) };
        SerializedProperty team = settings.FindProperty("team");
        team.arraySize = 4;
        for (int index = 0; index < 4; index++)
        {
            team.GetArrayElementAtIndex(index).FindPropertyRelative("Name").stringValue = names[index];
            team.GetArrayElementAtIndex(index).FindPropertyRelative("Role").stringValue = roles[index];
            Text name = (Text)settings.FindProperty("nameLabels").GetArrayElementAtIndex(index).objectReferenceValue;
            Text role = (Text)settings.FindProperty("roleLabels").GetArrayElementAtIndex(index).objectReferenceValue;
            float x = index % 2 == 0 ? -235 : 235;
            float y = index < 2 ? 128 : -137;
            Transform entry = card.Find("CreditEntry" + (index + 1));
            if (entry == null)
            {
                entry = new GameObject("CreditEntry" + (index + 1), typeof(RectTransform), typeof(Image)).transform;
                entry.SetParent(card, false);
            }
            Image entryImage = entry.GetComponent<Image>();
            entryImage.sprite = group;
            entryImage.type = Image.Type.Simple;
            entryImage.preserveAspect = false;
            entryImage.color = tints[index];
            entryImage.raycastTarget = false;
            Place((RectTransform)entry, new Vector2(x, y), new Vector2(450, 250));
            Image roleSurface = Surface(entry, "RoleSurface", new Color32(255, 254, 237, 250), new Vector2(0, -24), new Vector2(410, 164));
            roleSurface.transform.SetAsFirstSibling();
            name.transform.SetParent(entry, false);
            role.transform.SetParent(entry, false);
            Place(name.rectTransform, new Vector2(0, 83), new Vector2(426, 48));
            name.font = heading;
            name.fontSize = 34;
            name.color = Ink;
            name.text = names[index];
            Place(role.rectTransform, new Vector2(0, -24), new Vector2(426, 160));
            role.font = body;
            role.fontSize = 28;
            role.lineSpacing = 1.02f;
            role.color = Purple;
            role.alignment = TextAnchor.MiddleCenter;
            role.text = roles[index];
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static MenuRoundedPanel RoundedSurface(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        Transform item = parent.Find(name);
        if (item == null)
        {
            item = new GameObject(name, typeof(RectTransform)).transform;
            item.SetParent(parent, false);
        }
        Image oldImage = item.GetComponent<Image>();
        if (oldImage != null) UnityEngine.Object.DestroyImmediate(oldImage);
        MenuRoundedPanel panel = item.GetComponent<MenuRoundedPanel>();
        if (panel == null) panel = item.gameObject.AddComponent<MenuRoundedPanel>();
        if (item.GetComponent<CanvasRenderer>() == null) item.gameObject.AddComponent<CanvasRenderer>();
        panel.color = color;
        panel.raycastTarget = false;
        Place(panel.rectTransform, position, size);
        return panel;
    }

    private static Image Surface(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        Transform item = parent.Find(name);
        if (item == null)
        {
            item = new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
            item.SetParent(parent, false);
        }
        Image image = item.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.25f;
        image.color = color;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
        return image;
    }

    private static Text Caption(Transform parent, string name, string value, Font font, int size, Vector2 position, Vector2 bounds)
    {
        Transform item = parent.Find(name);
        if (item == null)
        {
            item = new GameObject(name, typeof(RectTransform), typeof(Text)).transform;
            item.SetParent(parent, false);
        }
        Text text = item.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        Place(text.rectTransform, position, bounds);
        return text;
    }

    private static void StyleButton(Button button, Color color, Font font, int fontSize)
    {
        button.GetComponent<Image>().color = color;
        Text label = button.GetComponentInChildren<Text>(true);
        label.font = font;
        label.fontSize = fontSize;
        label.color = Paper;
    }

    private static void Panel(Image image, Sprite sprite, Vector2 position, Vector2 size)
    {
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
#endif
