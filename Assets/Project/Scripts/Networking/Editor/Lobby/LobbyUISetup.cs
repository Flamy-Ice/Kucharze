#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.Linq;
using ModularMovement;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prepares only the lobby code panel and ready-room sign, preserving the room layout.
public static class LobbyUISetup
{
    [MenuItem("Tools/Kucharze/Online/Refresh Lobby UI")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene scene = SceneManager.GetSceneByPath(ProjectAssetPaths.LobbyScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save your lobby changes first.");
        if (opened) scene = EditorSceneManager.OpenScene(ProjectAssetPaths.LobbyScene, OpenSceneMode.Additive);
        try
        {
            Ensure(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Lobby UI.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    public static void Ensure(Scene scene)
    {
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        TextMesh sign = objects.Select(item => item.GetComponent<TextMesh>())
            .FirstOrDefault(item => item != null && (item.name == "READY ROOM" || item.GetComponent<LobbyReadinessSign>() != null));
        if (sign == null) throw new InvalidOperationException("The ready-room sign is missing.");
        sign.text = "READY ROOM\n0/0 READY\nHOST: SELECT A LEVEL";
        sign.characterSize = 0.1f;
        sign.lineSpacing = 1f;
        sign.anchor = TextAnchor.MiddleCenter;
        sign.alignment = TextAlignment.Center;
        Vector3 position = sign.transform.position;
        sign.transform.position = new Vector3(position.x, 1.38f, position.z);
        if (!sign.TryGetComponent<LobbyReadinessSign>(out _)) sign.gameObject.AddComponent<LobbyReadinessSign>();
        Transform plaque = objects.FirstOrDefault(item => item.name == "READY ROOMPlaque");
        if (plaque != null)
        {
            Vector3 plaquePosition = plaque.position;
            plaque.position = new Vector3(plaquePosition.x, 1.4f, plaquePosition.z);
            plaque.localScale = new Vector3(6.6f, 1.6f, 0.12f);
        }
        if (objects.Any(item => item.GetComponent<LobbyCodeDisplay>() != null)) return;

        Sprite background = AssetDatabase.LoadAllAssetsAtPath("Assets/Project/Gameplay/UI/HUD/Panels/clock-background.png")
            .OfType<Sprite>().FirstOrDefault();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Shared/UI/Fonts/TextMeshPro/Jua-Regular SDF.asset");
        if (background == null || font == null) throw new InvalidOperationException("The timer artwork or lobby font is missing.");
        var hud = new GameObject("LobbyHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(hud, scene);
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        hud.GetComponent<Canvas>().sortingOrder = 10;
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;
        RectTransform safe = Rect("SafeArea", hud.transform);
        Stretch(safe);
        RectTransform panel = Rect("LobbyCode_Background", safe);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0);
        panel.pivot = new Vector2(0.5f, 0);
        panel.sizeDelta = new Vector2(400, 151);
        panel.anchoredPosition = Vector2.zero;
        Image image = panel.gameObject.AddComponent<Image>();
        image.sprite = background;
        image.raycastTarget = false;
        RectTransform textRect = Rect("LobbyCode", panel);
        Stretch(textRect);
        textRect.offsetMin = new Vector2(24, 20);
        textRect.offsetMax = new Vector2(-24, -25);
        TextMeshProUGUI label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = "<size=22>LOBBY CODE</size>\n------";
        label.fontSize = 44;
        label.enableAutoSizing = true;
        label.fontSizeMin = 26;
        label.fontSizeMax = 44;
        label.color = new Color(0.18f, 0.15f, 0.12f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        var display = textRect.gameObject.AddComponent<LobbyCodeDisplay>();
        var serialized = new SerializedObject(display);
        serialized.FindProperty("safeArea").objectReferenceValue = safe;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(parent, false);
        return item.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
#endif
