#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Builds the lobby's dollhouse scenery without changing its network rules or player markers.
public static class LobbyPresentationBuilder
{
    private const string ArtRoot = "LobbyPresentation";
    private const string AssetRoot = ProjectAssetPaths.LobbyMaterials;
    private static readonly Quaternion ViewRotation = Quaternion.Euler(45, 0, 0);
    private static readonly Color[] Accents =
    {
        LobbyPlayerColors.Get(0), LobbyPlayerColors.Get(1), LobbyPlayerColors.Get(2), LobbyPlayerColors.Get(3)
    };

    public static Vector3 RoomSpawn(int index)
        => new Vector3(-7.5f + index * 5, 1.1f, 4.3f);

    public static void CreatePresentation()
    {
        ProjectAssetPaths.EnsureFolder(AssetRoot);
        ProjectAssetPaths.EnsureFolder(ProjectAssetPaths.CameraPrefabs);
        var art = new GameObject(ArtRoot).transform;
        Material foundation = Material("Foundation", new Color(0.18f, 0.21f, 0.23f));
        Material grout = Material("Grout", new Color(0.60f, 0.64f, 0.58f));
        Material wall = Material("WallFace", new Color(0.76f, 0.76f, 0.65f));
        Material cap = Material("WallCap", new Color(0.89f, 0.85f, 0.72f));
        Material trim = Material("WoodTrim", new Color(0.34f, 0.25f, 0.19f));
        Material wood = Material("FurnitureWood", new Color(0.48f, 0.31f, 0.19f));
        Material cream = Material("Linen", new Color(0.92f, 0.87f, 0.72f));
        Material dark = Material("Ink", new Color(0.16f, 0.21f, 0.22f));
        Material leaf = Material("Leaves", new Color(0.29f, 0.46f, 0.30f));
        Material pot = Material("Clay", new Color(0.64f, 0.34f, 0.23f));
        Block("BuildingBase", new Vector3(0, -0.35f, 0), new Vector3(20.4f, 0.7f, 14.4f), foundation, art, true);
        Block("LobbyFloor", new Vector3(0, 0.015f, 0), new Vector3(20, 0.03f, 14), grout, art);
        Material[] tiles = { Material("TileA", new Color(0.68f, 0.71f, 0.64f)), Material("TileB", new Color(0.66f, 0.69f, 0.62f)) };
        var hall = new GameObject("SharedHallTiles").transform;
        hall.SetParent(art);
        for (int x = 0; x < 20; x++)
            for (int z = 0; z < 10; z++)
                Block("Tile", new Vector3(-9.5f + x, 0.04f, -6.5f + z * 0.97f), new Vector3(0.985f, 0.025f, 0.955f), tiles[(x + z) % 2], hall);
        Wall("NorthWall", new Vector3(0, 0, 7), 20, 2.8f, false, wall, cap, trim, art);
        Wall("SouthWall", new Vector3(0, 0, -7), 20, 0.55f, false, wall, cap, trim, art);
        Wall("WestWall", new Vector3(-10, 0, 0), 14, 1.0f, true, wall, cap, trim, art);
        Wall("EastWall", new Vector3(10, 0, 0), 14, 1.0f, true, wall, cap, trim, art);
        for (int index = 0; index < 4; index++)
        {
            float x = -7.5f + index * 5;
            var room = new GameObject("Room_" + (index + 1) + "_Decor").transform;
            room.SetParent(art);
            Material accent = Material("Player" + (index + 1), Accents[index]);
            Material pale = Material("Player" + (index + 1) + "Pale", Color.Lerp(Accents[index], cream.color, 0.20f));
            Block("RoomFloor", new Vector3(x, 0.065f, 4.9f), new Vector3(4.85f, 0.04f, 4.05f), pale, room);
            if (index < 3)
                Wall("RoomDivider", new Vector3(x + 2.5f, 0, 4.9f), 4.2f, 1.3f, true, wall, cap, trim, room);
            // Each room opens directly into the shared hall through a 1.8 m doorway.
            Wall("DoorLeft", new Vector3(x - 1.7f, 0, 2.8f), 1.6f, 0.65f, false, wall, cap, trim, room);
            Wall("DoorRight", new Vector3(x + 1.7f, 0, 2.8f), 1.6f, 0.65f, false, wall, cap, trim, room);
            Block("Doorstep", new Vector3(x, 0.085f, 2.8f), new Vector3(1.8f, 0.04f, 0.45f), accent, room);
            Block("SpawnRug", new Vector3(x, 0.10f, 4.3f), new Vector3(1.7f, 0.035f, 1.8f), accent, room);
            Block("BedFrame", new Vector3(x + 1.3f, 0.25f, 5.65f), new Vector3(1.35f, 0.4f, 2.1f), wood, room, true);
            Block("Mattress", new Vector3(x + 1.3f, 0.53f, 5.65f), new Vector3(1.25f, 0.2f, 2), cream, room);
            Block("Blanket", new Vector3(x + 1.3f, 0.66f, 5.35f), new Vector3(1.28f, 0.08f, 1.35f), accent, room);
            Block("Pillow", new Vector3(x + 1.3f, 0.7f, 6.25f), new Vector3(0.95f, 0.15f, 0.4f), cream, room);
            Sign("ROOM " + (index + 1).ToString("00"), new Vector3(x, 1.7f, 6.8f), new Vector2(2.2f, 0.6f), dark, cream.color, room, 0.08f);
        }
        var readyRoom = new GameObject("ReadyRoomDecor").transform;
        readyRoom.SetParent(art);
        Material green = Material("ReadyGreen", new Color(0.42f, 0.62f, 0.48f));
        Material mint = Material("ReadyMint", new Color(0.76f, 0.84f, 0.66f));
        Block("StartFloorBorder", new Vector3(0, 0.075f, -1), new Vector3(6.1f, 0.04f, 3.3f), cream, readyRoom);
        Block("StartFloor", new Vector3(0, 0.105f, -1), new Vector3(5.8f, 0.025f, 3), green, readyRoom);
        for (int index = 0; index < 4; index++)
        {
            Vector3 spot = new Vector3(-2.1f + index * 1.4f, 0.13f, -1);
            Primitive("ReadySpotBorder", PrimitiveType.Cylinder, spot, new Vector3(1.05f, 0.008f, 1.05f), mint, readyRoom);
            Primitive("ReadySpot", PrimitiveType.Cylinder, spot + Vector3.up * 0.02f, new Vector3(0.86f, 0.008f, 0.86f), green, readyRoom);
        }
        Sign("READY ROOM", new Vector3(0, 1.4f, 1.65f), new Vector2(6.6f, 1.6f), wood, cream.color, readyRoom, 0.1f);
        var cameraObject = new GameObject("LobbyCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        Vector3 target = new Vector3(0, 0.65f, 0);
        cameraObject.transform.SetPositionAndRotation(target - ViewRotation * Vector3.forward * 74, ViewRotation);
        camera.orthographic = false;
        camera.fieldOfView = 25;
        camera.farClipPlane = 200;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.075f, 0.11f, 0.13f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        cameraObject.AddComponent<LobbyCameraRendering>();
        cameraObject.AddComponent<LobbyCameraFraming>().FrameNow();
        PrefabUtility.SaveAsPrefabAssetAndConnect(cameraObject, ProjectAssetPaths.LobbyCamera, InteractionMode.AutomatedAction);
        var light = new GameObject("LobbyLight").AddComponent<Light>();
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = new Color(1, 0.96f, 0.88f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.70f;
        light.shadowBias = 0.03f;
        light.shadowNormalBias = 0.20f;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.43f, 0.46f);
        LobbyUISetup.Ensure(SceneManager.GetActiveScene());
        LobbyLevelRoomBuilder.Ensure(SceneManager.GetActiveScene());
    }

    private static void Sign(string text, Vector3 position, Vector2 size, Material material, Color color, Transform parent, float textSize)
    {
        Block(text + "Plaque", position, new Vector3(size.x, size.y, 0.12f), material, parent);
        Label(text, position + new Vector3(0, -0.02f, -0.075f), textSize, color, parent);
    }
    private static Material Material(string name, Color color)
    {
        string path = ProjectAssetPaths.LobbyMaterialPath(name);
        ProjectAssetPaths.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) throw new InvalidOperationException("The lobby needs the URP Simple Lit shader.");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0);
        material.SetColor("_SpecColor", Color.black);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Block(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid = false)
        => Primitive(name, PrimitiveType.Cube, position, scale, material, parent, solid);

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid = false)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
        return item;
    }

    private static void Wall(string name, Vector3 position, float length, float height, bool alongZ, Material face, Material cap, Material trim, Transform parent)
    {
        Vector3 bodySize = alongZ ? new Vector3(0.22f, height, length) : new Vector3(length, height, 0.22f);
        Block(name, position + Vector3.up * (height * 0.5f), bodySize, face, parent, true);
        Block(name + "Cap", position + Vector3.up * (height + 0.04f), alongZ ? new Vector3(0.30f, 0.08f, length) : new Vector3(length, 0.08f, 0.30f), cap, parent);
        Block(name + "Skirting", position + Vector3.up * 0.12f, alongZ ? new Vector3(0.26f, 0.20f, length) : new Vector3(length, 0.20f, 0.26f), trim, parent);
    }

    private static void Plant(Vector3 position, Material pot, Material leaves, Transform parent)
    {
        Primitive("PlantPot", PrimitiveType.Cylinder, position + Vector3.up * 0.24f, new Vector3(0.65f, 0.24f, 0.65f), pot, parent);
        Primitive("PlantLeaves", PrimitiveType.Sphere, position + Vector3.up * 0.75f, new Vector3(0.85f, 0.95f, 0.85f), leaves, parent);
        Primitive("PlantLeaves", PrimitiveType.Sphere, position + new Vector3(0.25f, 1.0f, 0), new Vector3(0.55f, 0.65f, 0.6f), leaves, parent);
    }

    private static void Bench(Vector3 position, Material wood, Material cushion, Transform parent)
    {
        Block("BenchBase", position + Vector3.up * 0.25f, new Vector3(3.6f, 0.4f, 1.1f), wood, parent, true);
        Block("BenchSeat", position + Vector3.up * 0.5f, new Vector3(3.55f, 0.16f, 1.05f), cushion, parent);
        Block("BenchBack", position + new Vector3(0, 0.75f, 0.5f), new Vector3(3.6f, 0.85f, 0.15f), wood, parent);
    }

    private static void Label(string text, Vector3 position, float size, Color color, Transform parent)
    {
        var label = new GameObject(text);
        label.transform.SetParent(parent);
        label.transform.SetPositionAndRotation(position, Quaternion.identity);
        var mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.color = color;
    }

    [MenuItem("Tools/Kucharze/Online/Refresh Lobby Appearance")]
    public static void Refresh() => RefreshCore(false);

    [MenuItem("Tools/Kucharze/Online/Rebuild Lobby Room Layout")]
    public static void RebuildLayout() => RefreshCore(true);

    private static void RefreshCore(bool reshapeLayout)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(LobbySceneSetup.LobbyPath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save your lobby changes before refreshing its appearance.");
        if (opened) scene = EditorSceneManager.OpenScene(LobbySceneSetup.LobbyPath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            // Keep authored wall heights and perspective while refreshing the generated room.
            var wallAppearance = reshapeLayout ? new Dictionary<string, (Vector3, Quaternion, Vector3)>() : CaptureOuterWalls(scene);
            Camera oldCamera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).FirstOrDefault();
            float savedFieldOfView = oldCamera != null ? oldCamera.fieldOfView : 0;
            Quaternion savedRotation = oldCamera != null ? oldCamera.transform.rotation : ViewRotation;
            var oldNames = new HashSet<string>
            {
                ArtRoot, "Floor", "NorthWall", "SouthWall", "WestWall", "EastWall", "Room_1", "Room_2", "Room_3", "Room_4",
                "StartFloor", "StartLeft", "StartRight", "START ROOM", "SHARED CORRIDOR", "PLAYER 1", "PLAYER 2", "PLAYER 3", "PLAYER 4", "LobbyCamera", "LobbyLight"
            };
            foreach (Transform item in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray())
                if (item != null && oldNames.Contains(item.name)) UnityEngine.Object.DestroyImmediate(item.gameObject);
            CreatePresentation();
            RestoreOuterWalls(scene, wallAppearance);
            if (!reshapeLayout && savedFieldOfView > 0)
            {
                Camera newCamera = LobbyHierarchySetup.Find(scene, "LobbyCamera").GetComponent<Camera>();
                newCamera.fieldOfView = savedFieldOfView;
                newCamera.transform.rotation = savedRotation;
                PrefabUtility.RecordPrefabInstancePropertyModifications(newCamera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newCamera.transform);
            }
            LobbyLevelRoomBuilder.Ensure(scene);
            if (reshapeLayout)
            {
                LobbyLayout layout = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyLayout>()).Single();
                var settings = new SerializedObject(layout);
                SerializedProperty spawns = settings.FindProperty("roomSpawns");
                for (int index = 0; index < 4; index++)
                    ((Transform)spawns.GetArrayElementAtIndex(index).objectReferenceValue).position = RoomSpawn(index);
                var zone = (BoxCollider)settings.FindProperty("startZone").objectReferenceValue;
                zone.transform.position = LobbyLevelRoomBuilder.ReadyCenter;
                zone.size = new Vector3(5.8f, 4, 3);
            }
            LobbyHierarchySetup.Organize(scene);
            Physics.SyncTransforms();
            ValidateScene(scene);
            ValidateCompactLayout(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Dictionary<string, (Vector3 position, Quaternion rotation, Vector3 scale)> CaptureOuterWalls(Scene scene)
    {
        var result = new Dictionary<string, (Vector3, Quaternion, Vector3)>();
        Transform scenery = LobbyHierarchySetup.Find(scene, ArtRoot);
        if (scenery == null) return result;
        foreach (string side in new[] { "NorthWall", "EastWall", "WestWall" })
            foreach (string suffix in new[] { "", "Cap", "Skirting" })
            {
                Transform wall = LobbyHierarchySetup.Find(scenery, side + suffix);
                if (wall != null) result[wall.name] = (wall.localPosition, wall.localRotation, wall.localScale);
            }
        return result;
    }

    private static void RestoreOuterWalls(Scene scene, Dictionary<string, (Vector3 position, Quaternion rotation, Vector3 scale)> appearance)
    {
        Transform scenery = LobbyHierarchySetup.Find(scene, ArtRoot);
        foreach (var entry in appearance)
        {
            Transform wall = LobbyHierarchySetup.Find(scenery, entry.Key);
            if (wall == null) continue;
            wall.localPosition = entry.Value.position;
            wall.localRotation = entry.Value.rotation;
            wall.localScale = entry.Value.scale;
        }
    }

    // The batch preview uses temporary players, so the saved scene still spawns players through Netcode only.
    public static void BatchRefreshAndPreview()
    {
        RefreshCore(true);
        Scene scene = EditorSceneManager.OpenScene(LobbySceneSetup.LobbyPath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.LobbyPlayer);
        int previewIndex = 0;
        foreach (Vector3 position in new[] { RoomSpawn(0), new Vector3(1.8f, 1.1f, 0), RoomSpawn(3) })
        {
            GameObject player = UnityEngine.Object.Instantiate(prefab);
            player.transform.position = position;
            player.GetComponent<PlayerRotation>().ResolvedModel.rotation = Quaternion.Euler(0, 180, 0);
            int paletteIndex = previewIndex == 2 ? 3 : previewIndex;
            previewIndex++;
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                {
                    var properties = new MaterialPropertyBlock();
                    properties.SetColor("_BaseColor", renderer is MeshRenderer ? Color.Lerp(Accents[paletteIndex], Color.white, 0.35f) : Accents[paletteIndex]);
                    if (renderer is SkinnedMeshRenderer) properties.SetColor("_EmissionColor", Accents[paletteIndex] * 0.20f);
                    renderer.SetPropertyBlock(properties, 0);
                    if (renderer is SkinnedMeshRenderer) renderer.SetPropertyBlock(properties, 1);
                }
        }
        Physics.SyncTransforms();
        ValidateScene(scene);
        Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
        var target = new RenderTexture(1400, 1000, 24);
        var texture = new Texture2D(1400, 1000, TextureFormat.RGB24, false);
        camera.targetTexture = target;
        camera.aspect = 1.4f;
        camera.GetComponent<LobbyCameraFraming>().FrameNow();
        var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        bool previousBatching = pipeline.useSRPBatcher;
        pipeline.useSRPBatcher = true;
        float shadowDistance = pipeline.shadowDistance;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        if (!GraphicsSettings.useScriptableRenderPipelineBatching || !Mathf.Approximately(shadowDistance, pipeline.shadowDistance))
            throw new InvalidOperationException("The lobby camera did not restore the rendering settings.");
        pipeline.useSRPBatcher = previousBatching;
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, 1400, 1000), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "LobbyPreview.png"), texture.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void ValidateCompactLayout(Scene scene)
    {
        LobbyLayout layout = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyLayout>()).Single();
        LobbyLevelZones zones = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyLevelZones>()).Single();
        for (int index = 0; index < 9; index++)
        {
            if (!zones.TryGetCenter(index, out Vector3 center) || !zones.TryGetLevel(center, out int found) || found != index || layout.Contains(center))
                throw new InvalidOperationException("Invalid or overlapping level pad: " + (index + 1));
        }
        // Check a player's full body along each doorway and both routes around the start area.
        for (int index = 0; index < 4; index++)
            for (float z = 4.3f; z >= 1.8f; z -= 0.25f)
                CheckPassage(new Vector3(-7.5f + index * 5, 1.1f, z));
        for (float z = -5.8f; z <= 2; z += 0.25f)
        {
            CheckPassage(new Vector3(-5.5f, 1.1f, z));
            CheckPassage(new Vector3(5.5f, 1.1f, z));
        }
    }

    private static void CheckPassage(Vector3 position)
    {
        foreach (Collider collider in Physics.OverlapCapsule(position + Vector3.down * 0.48f,
            position + Vector3.up * 0.48f, 0.46f, ~0, QueryTriggerInteraction.Ignore))
            if (collider.gameObject.scene == SceneManager.GetActiveScene() && collider.GetComponentInParent<PlayerMovementResolver>() == null)
                throw new InvalidOperationException("Lobby passage blocked by " + collider.name);
    }

    private static void ValidateScene(Scene scene)
    {
        LobbyLayout layout = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyLayout>()).Single();
        for (int index = 0; index < 4; index++)
        {
            if (!layout.TryGetSpawn(index, out Vector3 position)) throw new InvalidOperationException("Missing room spawn.");
            if (Physics.CheckCapsule(position + Vector3.down * 0.48f, position + Vector3.up * 0.48f, 0.46f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                // Preview players own controllers at their spawn, so ignore those when checking the saved scenery.
                foreach (Collider collider in Physics.OverlapCapsule(position + Vector3.down * 0.48f, position + Vector3.up * 0.48f, 0.46f, ~0, QueryTriggerInteraction.Ignore))
                    if (collider.GetComponentInParent<PlayerMovementResolver>() == null)
                        throw new InvalidOperationException("Room spawn is blocked by " + collider.name);
            }
        }
        if (!layout.TryGetStartCenter(out Vector3 startCenter) || !layout.Contains(startCenter) || layout.Contains(new Vector3(0, 1.1f, 2.2f)))
            throw new InvalidOperationException("The start room bounds changed.");
        foreach (Transform item in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                throw new InvalidOperationException("Missing script on " + item.name);
    }
}
#endif
