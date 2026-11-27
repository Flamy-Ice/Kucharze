#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Places wall-side level choices and the start area together in one room.
public static class LobbyLevelRoomBuilder
{
    private const string RoomName = "LobbyLevelRoom";
    public static readonly Vector3 ReadyCenter = new Vector3(0, 1.5f, -1);

    [MenuItem("Tools/Kucharze/Online/Add Level Selection Room")]
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
            LobbyLevelZones zones = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyLevelZones>()).Single();
            for (int index = 0; index < 9; index++)
                if (!zones.TryGetCenter(index, out Vector3 center) || !zones.TryGetLevel(center, out int found) || found != index)
                    throw new InvalidOperationException("A level pad is missing or overlaps another selection zone: " + (index + 1));
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the level selection room.");
            Debug.Log("Level selection room ready: nine pads; host-only choice; configured level destinations preserved.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    public static void ConfigureInitialLevels(LobbySession session)
    {
        LobbyLevelSelection selection = session.GetComponent<LobbyLevelSelection>();
        var settings = new SerializedObject(selection);
        SerializedProperty levels = settings.FindProperty("levels");
        levels.arraySize = 9;
        for (int index = 0; index < 9; index++)
        {
            SerializedProperty item = levels.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("title").stringValue = "Level " + (index + 1);
            item.FindPropertyRelative("scenePath").stringValue = session.DefaultGameScenePath;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Ensure(Scene scene)
    {
        ConfigureSessionPrefab();
        Material wall = Material("Architecture/WallFace.mat");
        Material floor = Material("Architecture/Grout.mat");
        Material wood = Material("Furnishings/FurnitureWood.mat");
        Material tile = Material("Architecture/TileA.mat");
        Transform scenery = LobbyHierarchySetup.Find(scene, "LobbyPresentation");
        if (scenery == null) throw new InvalidOperationException("The lobby scenery is missing.");

        if (LobbyHierarchySetup.Find(scene, RoomName) == null)
            CreateRoom(scene, floor, wall, wood, tile);
        Transform room = LobbyHierarchySetup.Find(scene, RoomName);
        ArrangeRoom(room, scenery);
        MoveStartArea(scene, scenery);
        var bounds = new Bounds(new Vector3(0, 1.4f, 0), new Vector3(20.4f, 3, 14.4f));
        foreach (Renderer renderer in scenery.GetComponentsInChildren<Renderer>().Concat(room.GetComponentsInChildren<Renderer>()))
            bounds.Encapsulate(renderer.bounds);
        bounds.Expand(0.5f);
        foreach (LobbyCameraFraming framing in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyCameraFraming>(true)))
        {
            var settings = new SerializedObject(framing);
            settings.FindProperty("boardBounds").boundsValue = bounds;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(framing);
            framing.FrameNow();
            PrefabUtility.RecordPrefabInstancePropertyModifications(framing.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(framing.GetComponent<Camera>());
        }
        LobbyHierarchySetup.Organize(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ConfigureSessionPrefab()
    {
        string path = ProjectAssetPaths.NetworkPrefabs + "/LobbySession.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            LobbySession session = root.GetComponent<LobbySession>();
            if (session == null) throw new InvalidOperationException("LobbySession is missing from its prefab.");
            if (root.GetComponent<LobbyLevelSelection>() == null)
            {
                root.AddComponent<LobbyLevelSelection>();
                ConfigureInitialLevels(session);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ArrangeRoom(Transform room, Transform scenery)
    {
        // All choices now share the building floor, without a separate extension.
        foreach (string name in new[] { "Floor", "WestWall", "NorthWall", "SouthWall" })
        {
            Transform obsolete = room.Find(name);
            if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
        }
        Quaternion tilt = Quaternion.Euler(35, 0, 0);
        for (int index = 0; index < 9; index++)
        {
            Transform pad = LobbyHierarchySetup.Find(room, "Level_" + (index + 1).ToString("00"));
            int group = index / 3;
            int slot = index % 3;
            Vector3 center = group == 0 ? new Vector3(-8.3f, 0, 0.7f - slot * 2.5f)
                : group == 1 ? new Vector3(8.3f, 0, 0.7f - slot * 2.5f)
                : new Vector3(-2.8f + slot * 2.8f, 0, -5.8f);
            Place(pad, "SelectionPad", center + Vector3.up * 0.10f, new Vector3(1.55f, 0.07f, 1.4f));
            Transform zone = pad.Find("SelectionZone");
            zone.position = center + Vector3.up * 1.5f;
            zone.GetComponent<BoxCollider>().size = new Vector3(1.4f, 4, 1.25f);
            Vector3 card = center + new Vector3(0, 1.6f, 0.7f);
            Place(pad, "IconFrame", card, new Vector3(1.5f, 1.65f, 0.08f));
            pad.Find("IconFrame").rotation = tilt;
            pad.Find("LevelIcon").SetPositionAndRotation(card + tilt * new Vector3(0, 0.2f, -0.06f), tilt);
            pad.Find("IconPlaceholder").SetPositionAndRotation(card + tilt * new Vector3(0, 0.2f, -0.07f), tilt);
            TextMesh title = pad.Find("LevelTitle").GetComponent<TextMesh>();
            title.transform.SetPositionAndRotation(card + tilt * new Vector3(0, -0.51f, -0.07f), tilt);
            title.characterSize = 0.045f;
            title.color = new Color(0.96f, 0.93f, 0.83f);
        }
        Place(room, "LevelSelectionPlaque", new Vector3(0, 0.065f, -4), new Vector3(6, 0.04f, 0.85f));
        TextMesh status = room.Find("LevelSelectionStatus").GetComponent<TextMesh>();
        status.transform.SetPositionAndRotation(new Vector3(0, 0.10f, -4), Quaternion.Euler(90, 0, 0));
        status.transform.localScale = Vector3.one;
        status.characterSize = 0.065f;
        status.text = "HOST: SELECT A LEVEL\nSTEP ON A PAD TO SELECT";
    }

    private static void MoveStartArea(Scene scene, Transform scenery)
    {
        Transform decor = scenery.Find("ReadyRoomDecor");
        Transform floor = decor != null ? decor.Find("StartFloor") : null;
        if (floor == null) throw new InvalidOperationException("The start area's visual floor is missing.");
        decor.position += new Vector3(ReadyCenter.x - floor.position.x, 0, ReadyCenter.z - floor.position.z);
        // Keep the sign beside the green floor, clear of the wall-side choices.
        Transform plaque = decor.Find("READY ROOMPlaque");
        Transform sign = decor.Find("READY ROOM");
        if (plaque != null) plaque.position = new Vector3(ReadyCenter.x, plaque.position.y, 1.65f);
        if (sign != null) sign.position = new Vector3(ReadyCenter.x, sign.position.y, 1.575f);
        foreach (LobbyLayout layout in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyLayout>()))
        {
            var settings = new SerializedObject(layout);
            var zone = settings.FindProperty("startZone").objectReferenceValue as BoxCollider;
            if (zone == null) continue;
            zone.transform.position = ReadyCenter;
            zone.size = new Vector3(5.8f, 4, 3);
        }
    }

    private static void Place(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        Transform item = parent.Find(name);
        if (item == null) throw new InvalidOperationException("Missing generated room object: " + name);
        item.position = position;
        item.localScale = scale;
    }

    private static void CreateRoom(Scene scene, Material floor, Material wall, Material wood, Material tile)
    {
        var root = new GameObject(RoomName);
        SceneManager.MoveGameObjectToScene(root, scene);
        Transform room = root.transform;
        Block("Floor", new Vector3(-19.4f, -0.1f, 0), new Vector3(16, 0.2f, 11), floor, room, true);
        Block("WestWall", new Vector3(-27.4f, 2.3f, 0), new Vector3(0.22f, 4.6f, 11), wall, room, true);
        Block("NorthWall", new Vector3(-19.4f, 2.3f, 5.5f), new Vector3(16, 4.6f, 0.22f), wall, room, true);
        Block("SouthWall", new Vector3(-19.4f, 0.35f, -5.5f), new Vector3(16, 0.7f, 0.22f), wall, room, true);
        // The existing lobby wall also forms this room's east side.
        var zones = root.AddComponent<LobbyLevelZones>();
        var view = root.AddComponent<LobbyLevelRoomView>();
        var zoneSettings = new SerializedObject(zones);
        var viewSettings = new SerializedObject(view);
        foreach (string field in new[] { "pads", "titles", "icons", "placeholders" }) viewSettings.FindProperty(field).arraySize = 9;
        zoneSettings.FindProperty("selectionZones").arraySize = 9;
        for (int index = 0; index < 9; index++)
        {
            float x = -25.8f + index * 1.6f;
            float z = 4.15f;
            var pad = new GameObject("Level_" + (index + 1).ToString("00"));
            pad.transform.SetParent(room, false);
            GameObject surface = Block("SelectionPad", new Vector3(x, 0.03f, z), new Vector3(1.15f, 0.06f, 1.1f), tile, pad.transform, false);
            var marker = new GameObject("SelectionZone");
            marker.transform.SetParent(pad.transform, false);
            marker.transform.position = new Vector3(x, 1.5f, z);
            BoxCollider box = marker.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1, 4, 0.75f);
            Block("IconFrame", new Vector3(x, 1.65f, 5.25f), new Vector3(1.2f, 1.2f, 0.08f), wood, pad.transform, false);
            var iconObject = new GameObject("LevelIcon");
            iconObject.transform.SetParent(pad.transform, false);
            iconObject.transform.position = new Vector3(x, 1.65f, 5.19f);
            SpriteRenderer icon = iconObject.AddComponent<SpriteRenderer>();
            TextMesh placeholder = Label("IconPlaceholder", "?", new Vector3(x, 1.65f, 5.18f), 0.15f, pad.transform);
            TextMesh title = Label("LevelTitle", "Level " + (index + 1) + "\nUNAVAILABLE", new Vector3(x, 0.72f, 5.17f), 0.045f, pad.transform);
            title.color = new Color(0.12f, 0.16f, 0.18f);
            zoneSettings.FindProperty("selectionZones").GetArrayElementAtIndex(index).objectReferenceValue = box;
            SetElement(viewSettings, "pads", index, surface.GetComponent<Renderer>());
            SetElement(viewSettings, "titles", index, title);
            SetElement(viewSettings, "icons", index, icon);
            SetElement(viewSettings, "placeholders", index, placeholder);
        }
        Block("LevelSelectionPlaque", new Vector3(-19.4f, 3.5f, 5.44f), new Vector3(10, 1.1f, 0.08f), wood, room, false);
        TextMesh status = Label("LevelSelectionStatus", "HOST: SELECT A LEVEL\nSTEP ON A PAD TO SELECT", new Vector3(-19.4f, 3.5f, 5.35f), 0.1f, room);
        viewSettings.FindProperty("statusLabel").objectReferenceValue = status;
        zoneSettings.ApplyModifiedPropertiesWithoutUndo();
        viewSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetElement(SerializedObject settings, string field, int index, UnityEngine.Object value)
        => settings.FindProperty(field).GetArrayElementAtIndex(index).objectReferenceValue = value;

    private static Material Material(string relative)
        => AssetDatabase.LoadAssetAtPath<Material>(ProjectAssetPaths.LobbyMaterials + "/" + relative)
            ?? throw new InvalidOperationException("Missing lobby material: " + relative);

    private static GameObject Block(string name, Vector3 position, Vector3 size, Material material, Transform parent, bool solid)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.localScale = size;
        item.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
        return item;
    }

    private static TextMesh Label(string name, string text, Vector3 position, float size, Transform parent)
    {
        var item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        TextMesh label = item.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = size;
        label.color = new Color(0.96f, 0.93f, 0.83f);
        return label;
    }
}
#endif
