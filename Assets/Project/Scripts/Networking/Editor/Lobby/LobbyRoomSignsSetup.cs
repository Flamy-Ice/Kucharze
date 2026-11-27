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

// Updates room colors and nickname plaques without rebuilding the room geometry.
[InitializeOnLoad]
public static class LobbyRoomSignsSetup
{
    private const string Request = "Assets/Project/Scripts/Networking/Editor/Lobby/LobbyVisuals.request";
    private const string Result = "Assets/Project/Scripts/Networking/Editor/Lobby/LobbyVisuals.result";

    static LobbyRoomSignsSetup() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || !File.Exists(Request)) return;
        if (!AssetDatabase.DeleteAsset(Request)) File.Delete(Request);
        try
        {
            LobbyUISetup.Refresh();
            AssetDatabase.SaveAssets();
            Preview();
            File.WriteAllText(Result, "SUCCESS: room names, shared colors and readable code; preview saved");
        }
        catch (Exception exception) { File.WriteAllText(Result, "FAILED: " + exception); }
    }

    public static void Ensure(Scene scene)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Project/Shared/UI/Fonts/TextMeshPro/NicknameFont.asset");
        if (font == null) throw new InvalidOperationException("Set up player nicknames before updating the room signs.");
        for (int index = 0; index < LobbyPlayerColors.Count; index++)
        {
            Transform room = LobbyHierarchySetup.Find(scene, "Room_" + (index + 1) + "_Decor");
            if (room == null) throw new InvalidOperationException("A player room is missing.");
            SetColor("Player" + (index + 1), LobbyPlayerColors.Get(index));
            SetColor("Player" + (index + 1) + "Pale", Color.Lerp(LobbyPlayerColors.Get(index), new Color(0.92f, 0.87f, 0.72f), 0.20f));
            Transform old = room.Find("ROOM " + (index + 1).ToString("00"));
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform plaque = room.Find("ROOM " + (index + 1).ToString("00") + "Plaque");
            if (plaque != null)
            {
                plaque.position = new Vector3(plaque.position.x, 1.9f, plaque.position.z);
                plaque.localScale = new Vector3(4.3f, 0.95f, 0.12f);
            }
            Transform existing = room.Find("RoomOccupantName");
            TextMeshPro label;
            if (existing == null)
            {
                var item = new GameObject("RoomOccupantName", typeof(RectTransform), typeof(TextMeshPro));
                item.transform.SetParent(room, false);
                label = item.GetComponent<TextMeshPro>();
            }
            else label = existing.GetComponent<TextMeshPro>();
            label.font = font;
            label.richText = false;
            label.text = "WAITING\nFOR PLAYER";
            label.color = new Color(1, 0.99f, 0.90f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSize = label.fontSizeMax = 4.5f;
            label.fontSizeMin = 2.4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(4.1f, 0.87f);
            Vector3 position = plaque != null ? plaque.position - plaque.forward * 0.085f
                : new Vector3(-7.5f + index * 5, 1.9f, 6.715f);
            label.rectTransform.anchoredPosition3D = room.InverseTransformPoint(position);
            if (plaque != null) label.transform.rotation = plaque.rotation;
            label.ForceMeshUpdate();
            LobbyRoomOccupantSign sign = label.GetComponent<LobbyRoomOccupantSign>();
            if (sign == null) sign = label.gameObject.AddComponent<LobbyRoomOccupantSign>();
            var settings = new SerializedObject(sign);
            settings.FindProperty("roomIndex").intValue = index;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetColor(string name, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ProjectAssetPaths.LobbyMaterialPath(name));
        if (material == null) throw new InvalidOperationException("Missing room material: " + name);
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
    }

    private static void Preview()
    {
        Scene scene = SceneManager.GetSceneByPath(ProjectAssetPaths.LobbyScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ProjectAssetPaths.LobbyScene, OpenSceneMode.Additive);
        Scene preview = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                SceneManager.MoveGameObjectToScene(UnityEngine.Object.Instantiate(root), preview);
            Camera camera = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
            camera.scene = preview;
            camera.targetTexture = target;
            camera.aspect = 1280f / 720;
            camera.GetComponent<LobbyCameraFraming>().FrameNow();
            Canvas canvas = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Single();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.GetComponentInChildren<LobbyCodeDisplay>().GetComponent<TMP_Text>().text = LobbyCodeDisplay.FormatCode("L9NB66");
            string[] names = { "Kuba", "Łukasz", "Aleksandra_123456", "WAITING\nFOR PLAYER" };
            for (int index = 0; index < 4; index++)
            {
                Transform room = LobbyHierarchySetup.Find(preview, "Room_" + (index + 1) + "_Decor");
                TextMeshPro label = room.Find("RoomOccupantName").GetComponent<TextMeshPro>();
                label.text = names[index];
                label.ForceMeshUpdate();
            }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Result + ".png", texture.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(preview);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
