#if UNITY_EDITOR && KUCHARZE_ONLINE
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds the display controls while keeping the existing audio sliders and their references.
public static class MainMenuOptionsSetup
{
    private static readonly Color Ink = new Color32(37, 33, 56, 255);
    private static readonly Color Purple = new Color32(75, 71, 86, 255);
    private static readonly Color Paper = new Color32(255, 254, 221, 255);
    private static readonly Color Teal = new Color32(5, 158, 148, 255);

    public static void ApplyToScene(Scene scene, Font heading, Font body)
    {
        Transform screen = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(item => item.name == "OptionsCanvas");
        Transform card = screen.Find("OptionsCard");
        var audio = new SerializedObject(screen.GetComponent<MenuAudioOptions>());
        for (int index = 0; index < 2; index++)
        {
            string name = index == 0 ? "Music" : "Sounds";
            float y = index == 0 ? 165 : 75;
            Place((RectTransform)card.Find(name + "Slider"), new Vector2(0, y), new Vector2(700, 52));
            Place((RectTransform)card.Find(name), new Vector2(-230, y + 47), new Vector2(240, 44));
            Text value = (Text)audio.FindProperty(index == 0 ? "musicValue" : "soundsValue").objectReferenceValue;
            Place(value.rectTransform, new Vector2(265, y + 47), new Vector2(170, 44));
        }
        Text displayTitle = Text("DisplayTitle", "Display", card, heading, 30);
        Place(displayTitle.rectTransform, new Vector2(0, 12), new Vector2(500, 46));
        Text frameTitle = Text("FrameLimitLabel", "Frame limit", card, body, 29);
        Place(frameTitle.rectTransform, new Vector2(-210, -126), new Vector2(320, 56));
        frameTitle.alignment = TextAnchor.MiddleLeft;
        var resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
        Transform frameControl = card.Find("FrameLimit");
        if (frameControl == null)
        {
            GameObject item = DefaultControls.CreateDropdown(resources);
            item.name = "FrameLimit";
            item.transform.SetParent(card, false);
            frameControl = item.transform;
        }
        Dropdown frame = frameControl.GetComponent<Dropdown>();
        Place((RectTransform)frameControl, new Vector2(210, -126), new Vector2(280, 60));
        frame.GetComponent<Image>().color = Purple;
        foreach (Text label in frame.GetComponentsInChildren<Text>(true)) { label.font = body; label.fontSize = 28; label.color = Ink; }
        frame.captionText.color = Paper;
        frameControl.Find("Arrow").GetComponent<Image>().color = Paper;
        frame.template.GetComponent<Image>().color = Paper;
        frame.template.sizeDelta = new Vector2(0, 220);
        var content = (RectTransform)frame.template.Find("Viewport/Content");
        content.sizeDelta = new Vector2(0, 44);
        ((RectTransform)content.Find("Item")).sizeDelta = new Vector2(0, 44);
        content.Find("Item/Item Checkmark").GetComponent<Image>().color = Teal;
        frame.ClearOptions();
        frame.AddOptions(new[] { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" }.ToList());
        frame.SetValueWithoutNotify(4);

        Transform resolutionControl = card.Find("Resolution");
        if (resolutionControl == null)
        {
            resolutionControl = Object.Instantiate(frame.gameObject, card).transform;
            resolutionControl.name = "Resolution";
        }
        Place((RectTransform)resolutionControl, new Vector2(210, -55), new Vector2(280, 60));
        Dropdown resolution = resolutionControl.GetComponent<Dropdown>();
        if (resolution.GetComponent<MenuDropdownSelectionScroll>() == null)
            resolution.gameObject.AddComponent<MenuDropdownSelectionScroll>();
        foreach (Text label in resolution.GetComponentsInChildren<Text>(true)) { label.font = body; label.fontSize = 28; label.color = Ink; }
        resolution.captionText.color = Paper;
        resolution.ClearOptions();
        resolution.AddOptions(new[] { Screen.currentResolution.width + " x " + Screen.currentResolution.height }.ToList());
        Text resolutionLabel = Text("ResolutionLabel", "Resolution", card, body, 29);
        resolutionLabel.alignment = TextAnchor.MiddleLeft;
        Place(resolutionLabel.rectTransform, new Vector2(-210, -55), new Vector2(320, 56));
        Toggle fullscreen = Toggle("Fullscreen", card, new Vector2(-210, -202), resources, body);
        Toggle sync = Toggle("VSync", card, new Vector2(230, -202), resources, body);
        Text hint = card.Find("Your settings are saved automatically").GetComponent<Text>();
        Place(hint.rectTransform, new Vector2(0, -251), new Vector2(840, 32));
        hint.fontSize = 21;
        var options = screen.GetComponent<MenuVideoOptions>();
        if (options == null) options = screen.gameObject.AddComponent<MenuVideoOptions>();
        var settings = new SerializedObject(options);
        settings.FindProperty("frameLimit").objectReferenceValue = frame;
        settings.FindProperty("fullscreen").objectReferenceValue = fullscreen;
        settings.FindProperty("verticalSync").objectReferenceValue = sync;
        settings.FindProperty("hint").objectReferenceValue = hint;
        settings.FindProperty("resolution").objectReferenceValue = resolution;
        settings.FindProperty("resolutionLabel").objectReferenceValue = resolutionLabel;
        settings.ApplyModifiedPropertiesWithoutUndo();

        if (!scene.GetRootGameObjects().Any(root => root.GetComponent<MenuVideoSettings>() != null))
        {
            var owner = new GameObject("MenuVideoSettings");
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.AddComponent<MenuVideoSettings>();
        }
    }

    private static Toggle Toggle(string name, Transform parent, Vector2 position, DefaultControls.Resources resources, Font font)
    {
        Transform item = parent.Find(name);
        if (item == null)
        {
            GameObject created = DefaultControls.CreateToggle(resources);
            created.name = name;
            created.transform.SetParent(parent, false);
            item = created.transform;
        }
        Place((RectTransform)item, position, new Vector2(300, 56));
        var background = (RectTransform)item.Find("Background");
        background.anchorMin = background.anchorMax = new Vector2(0, 0.5f);
        background.anchoredPosition = new Vector2(24, 0);
        background.sizeDelta = new Vector2(44, 44);
        background.GetComponent<Image>().color = Purple;
        Image check = background.Find("Checkmark").GetComponent<Image>();
        check.rectTransform.sizeDelta = new Vector2(30, 30);
        check.color = Paper;
        Text label = item.Find("Label").GetComponent<Text>();
        label.text = name;
        label.font = font;
        label.fontSize = 28;
        label.color = Ink;
        label.rectTransform.offsetMin = new Vector2(60, 0);
        label.rectTransform.offsetMax = Vector2.zero;
        item.GetComponent<UnityEngine.UI.Toggle>().SetIsOnWithoutNotify(false);
        MainMenuFeedbackSetup.StyleCheckbox(item.GetComponent<UnityEngine.UI.Toggle>());
        return item.GetComponent<UnityEngine.UI.Toggle>();
    }

    private static Text Text(string name, string caption, Transform parent, Font font, int size)
    {
        Transform item = parent.Find(name);
        if (item == null)
        {
            item = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text)).transform;
            item.SetParent(parent, false);
        }
        var text = item.GetComponent<UnityEngine.UI.Text>();
        text.text = caption;
        text.font = font;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
#endif
