#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using ModularMovement;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Imports the supplied pause artwork and wires both scene menus to the existing settings.
public static class PauseMenuArtworkSetup
{
    public const string Root = "Assets/Project/Gameplay/UI/PauseMenu";
    private const string BlurMaterial = Root + "/MenuBackdropBlur.mat";
    private const string Role = "Game Systems Integration\nOnline Multiplayer\nProgramming\nUI/UX";
    private static readonly string[,] Artwork =
    {
        { "Rectangle 10.png", "Artwork/pause-menu-panel.png" },
        { "MENU.png", "Artwork/pause-menu-title.png" },
        { "your game is not paused!.png", "Artwork/online-game-not-paused.png" },
        { "Group 12.png", "Buttons/options-button.png" },
        { "Group 13.png", "Buttons/recipes-button.png" },
        { "Group 15.png", "Buttons/end-session-button.png" }
    };

    [MenuItem("Tools/Kucharze/HUD/Apply Pause Menu Artwork")]
    public static void Setup()
    {
        HudMenuCaptureSetup.Install();
        string[] paths = { ProjectAssetPaths.MenuScene, ProjectAssetPaths.GameScene, ProjectAssetPaths.LobbyScene };
        var scenes = new Scene[3];
        var opened = new bool[3];
        Scene previous = SceneManager.GetActiveScene();
        for (int i = 0; i < paths.Length; i++)
        {
            scenes[i] = SceneManager.GetSceneByPath(paths[i]);
            opened[i] = !scenes[i].IsValid() || !scenes[i].isLoaded;
            if (!opened[i] && scenes[i].isDirty) throw new InvalidOperationException("Save " + paths[i] + " before updating the pause menu.");
        }
        try
        {
            ImportArtwork();
            for (int i = 0; i < paths.Length; i++)
                if (opened[i]) scenes[i] = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
            UpdateCredits(scenes[0]);
            for (int i = 1; i < scenes.Length; i++)
            {
                SceneManager.SetActiveScene(scenes[i]);
                var hud = scenes[i].GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HudGameMenu>(true)).Single();
                Build(hud, scenes[0]);
                Preview(hud, scenes[i], i == 1 ? "kuba" : "lobby");
                if (!EditorSceneManager.SaveScene(scenes[i])) throw new IOException("Could not save " + paths[i]);
            }
            EditorSceneManager.SaveScene(scenes[0]);
            AssetDatabase.SaveAssets();
            // Delete the import folder only after every supplied image has a verified destination.
            if (AssetDatabase.IsValidFolder("Assets/TAKEIT"))
            {
                if (Directory.EnumerateFileSystemEntries("Assets/TAKEIT").Any())
                    throw new IOException("TAKEIT still contains files; leaving it intact.");
                if (!AssetDatabase.DeleteAsset("Assets/TAKEIT")) throw new IOException("Could not remove the empty TAKEIT folder.");
            }
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            for (int i = scenes.Length - 1; i >= 0; i--)
                if (opened[i] && scenes[i].IsValid() && scenes[i].isLoaded) EditorSceneManager.CloseScene(scenes[i], true);
        }
    }

    private static void ImportArtwork()
    {
        ProjectAssetPaths.EnsureFolder(Root + "/Artwork");
        ProjectAssetPaths.EnsureFolder(Root + "/Buttons");
        for (int i = 0; i < Artwork.GetLength(0); i++)
        {
            string source = "Assets/TAKEIT/" + Artwork[i, 0];
            string destination = Root + "/" + Artwork[i, 1];
            if (File.Exists(source))
            {
                if (File.Exists(destination)) throw new IOException("Destination already exists: " + destination);
                string guid = AssetDatabase.AssetPathToGUID(source);
                string error = AssetDatabase.MoveAsset(source, destination);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                if (guid != AssetDatabase.AssetPathToGUID(destination)) throw new IOException("The sprite GUID changed.");
            }
            var importer = AssetImporter.GetAtPath(destination) as TextureImporter;
            if (importer == null) throw new IOException("Missing supplied artwork: " + destination);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            if (AssetDatabase.LoadAssetAtPath<Sprite>(destination) == null) throw new IOException("Sprite import failed: " + destination);
        }
    }

    private static void UpdateCredits(Scene scene)
    {
        var credits = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MenuCredits>(true)).Single();
        var serialized = new SerializedObject(credits);
        var team = serialized.FindProperty("team");
        bool found = false;
        for (int i = 0; i < team.arraySize; i++)
        {
            var member = team.GetArrayElementAtIndex(i);
            if (member.FindPropertyRelative("Name").stringValue != "Jakub Lewandowski") continue;
            member.FindPropertyRelative("Role").stringValue = Role;
            var label = serialized.FindProperty("roleLabels").GetArrayElementAtIndex(i).objectReferenceValue as Text;
            if (label != null) { label.text = Role; EditorUtility.SetDirty(label); }
            found = true;
        }
        if (!found) throw new InvalidOperationException("Jakub's credit entry was not found.");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform Find(Transform root, string name)
        => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one; }
    private static Transform Group(string name, Transform parent)
    {
        var group = new GameObject(name, typeof(RectTransform)).transform;
        group.SetParent(parent, false);
        Stretch((RectTransform)group);
        return group;
    }
    private static Image Image(string name, string path, Transform parent, Vector2 position, Vector2 size)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        Place(image.rectTransform, position, size);
        if (path != null) image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + path);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
    private static Button SpriteButton(string name, string path, Transform parent, float y)
    {
        var image = Image(name, path, parent, new Vector2(0, y), new Vector2(342, 139));
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        image.gameObject.AddComponent<MenuSelectableScale>();
        image.gameObject.AddComponent<MenuSoundFeedback>();
        return button;
    }
    private static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Build(HudGameMenu controller, Scene menuScene)
    {
        Transform hud = controller.transform;
        var old = Find(hud, "GameMenu");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        Transform panel = Group("GameMenu", hud);
        panel.gameObject.SetActive(false);
        panel.gameObject.AddComponent<HudMenuBackdrop>();
        Transform blur = Group("LiveBlur", panel);
        var raw = blur.gameObject.AddComponent<RawImage>();
        var material = AssetDatabase.LoadAssetAtPath<Material>(BlurMaterial);
        if (material == null)
        {
            material = new Material(Shader.Find("Kucharze/UI/Live Menu Blur")) { name = "MenuBackdropBlur" };
            AssetDatabase.CreateAsset(material, BlurMaterial);
        }
        raw.material = material;
        raw.raycastTarget = false;
        Transform dim = Group("Dimmer", panel);
        dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        Transform content = Group("MenuContent", panel);
        Image("Panel", "Artwork/pause-menu-panel.png", content, Vector2.zero, new Vector2(456, 538));
        Image("Title", "Artwork/pause-menu-title.png", content, new Vector2(0, 340), new Vector2(250, 91));
        Image("OnlineNotice", "Artwork/online-game-not-paused.png", content, new Vector2(0, -340), new Vector2(610, 70));
        Button options = SpriteButton("Options", "Buttons/options-button.png", content, 165);
        Button recipes = SpriteButton("Recipes", "Buttons/recipes-button.png", content, 0);
        recipes.interactable = false;
        Button leave = SpriteButton("Leave", "Buttons/end-session-button.png", content, -165);
        var optionsSource = menuScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(item => item.name == "OptionsCanvas");
        var optionsPanel = UnityEngine.Object.Instantiate(optionsSource.gameObject, panel);
        optionsPanel.name = "PauseOptions";
        Stretch(optionsPanel.GetComponent<RectTransform>());
        optionsPanel.transform.localScale = Vector3.one;
        optionsPanel.SetActive(false);
        foreach (var button in optionsPanel.GetComponentsInChildren<Button>(true)) button.onClick = new Button.ButtonClickedEvent();
        Button back = optionsPanel.GetComponentsInChildren<Button>(true).Single(button => button.name == "BackButton");
        Ref(controller, "panel", panel.gameObject);
        Ref(controller, "leaveButton", leave);
        Ref(controller, "optionsButton", options);
        Ref(controller, "optionsBackButton", back);
        Ref(controller, "menuContent", content.gameObject);
        Ref(controller, "optionsPanel", optionsPanel);
        ConfigureDismissal(controller);
        EnsureService<MenuAudioSettings>(hud, menuScene);
        EnsureService<MenuVideoSettings>(hud, menuScene);
        if (ShaderUtil.ShaderHasError(material.shader)) throw new InvalidOperationException("Pause menu blur shader failed.");
    }

    // Updates just the close gesture, preserving the existing artwork and settings view.
    public static void UpdateDismissal()
    {
        foreach (string path in new[] { ProjectAssetPaths.LobbyScene, ProjectAssetPaths.GameScene })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before updating menu dismissal.");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var controller = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HudGameMenu>(true)).Single();
                ConfigureDismissal(controller);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save menu dismissal.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }

    private static void ConfigureDismissal(HudGameMenu controller)
    {
        Transform panel = Find(controller.transform, "GameMenu");
        Transform content = panel.Find("MenuContent");
        Transform oldClose = content.Find("Resume");
        if (oldClose != null) UnityEngine.Object.DestroyImmediate(oldClose.gameObject);
        var dimmer = panel.Find("Dimmer").GetComponent<Image>();
        dimmer.raycastTarget = true;
        var close = dimmer.GetComponent<Button>();
        if (close == null) close = dimmer.gameObject.AddComponent<Button>();
        close.transition = Selectable.Transition.None;
        close.navigation = new Navigation { mode = Navigation.Mode.None };
        close.targetGraphic = dimmer;
        // Empty space inside the cream panel must absorb clicks instead of reaching the backdrop.
        content.Find("Panel").GetComponent<Image>().raycastTarget = true;
        Ref(controller, "resumeButton", close);
        var existing = content.Find("DismissHint");
        var hint = existing != null ? existing.GetComponent<TextMeshProUGUI>()
            : new GameObject("DismissHint", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        hint.transform.SetParent(content, false);
        var rect = hint.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 55);
        rect.sizeDelta = new Vector2(850, 54);
        hint.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Shared/UI/Fonts/TextMeshPro/NicknameFont.asset");
        hint.text = "Click or tap outside to close";
        hint.fontSize = 28;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color32(255, 253, 225, 255);
        hint.raycastTarget = false;
    }

    private static void EnsureService<T>(Transform hud, Scene source) where T : MonoBehaviour
    {
        if (hud.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Any()) return;
        var service = source.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        var copy = UnityEngine.Object.Instantiate(service.gameObject, hud.parent);
        copy.name = service.name;
    }

    private static void Preview(HudGameMenu hud, Scene scene, string suffix)
    {
        var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
        var canvas = hud.GetComponent<Canvas>();
        var panel = Find(hud.transform, "GameMenu").gameObject;
        var otherRoots = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .Where(item => item != scene && item.isLoaded).SelectMany(item => item.GetRootGameObjects())
            .Where(item => item.activeSelf).ToArray();
        var previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        float previousDistance = canvas.planeDistance;
        var blurImage = Find(panel.transform, "LiveBlur").GetComponent<RawImage>();
        var previousTexture = blurImage.texture;
        var previousMaterial = blurImage.material;
        var previewMaterial = new Material(previousMaterial);
        blurImage.material = previewMaterial;
        var sceneCopy = RTHandles.Alloc(640, 360, colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
            filterMode: FilterMode.Bilinear, name: "Pause Menu Preview");
        var target = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previousActive = RenderTexture.active;
        try
        {
            foreach (var root in otherRoots) root.SetActive(false);
            camera.targetTexture = target;
            camera.aspect = 1280f / 720;
            HudMenuCaptureFeature.Register(camera, sceneCopy);
            blurImage.texture = sceneCopy.rt;
            previewMaterial.mainTexture = sceneCopy.rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            panel.SetActive(false);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            HudMenuCaptureFeature.Unregister(camera, sceneCopy);
            panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Assets/Project/Scripts/UI/HUD/Editor/PausePreview." + suffix + ".png", image.EncodeToPNG());
        }
        finally
        {
            panel.SetActive(false);
            HudMenuCaptureFeature.Unregister(camera, sceneCopy);
            blurImage.texture = previousTexture;
            blurImage.material = previousMaterial;
            UnityEngine.Object.DestroyImmediate(previewMaterial);
            sceneCopy.Release();
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousDistance;
            RenderTexture.active = previousActive;
            foreach (var root in otherRoots) if (root != null) root.SetActive(true);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
#endif
