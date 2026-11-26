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
        new Color(0.30f, 0.60f, 0.78f), new Color(0.88f, 0.45f, 0.32f),
        new Color(0.60f, 0.46f, 0.74f), new Color(0.83f, 0.65f, 0.25f)
    };

    public static Vector3 RoomSpawn(int index)
        => new Vector3((index % 2 == 0 ? -7.4f : 7.4f) + 0.45f, 1.1f, index < 2 ? -4.75f : 4.75f);

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
        Block("BuildingBase", new Vector3(0, -0.35f, 0), new Vector3(23.2f, 0.7f, 15.6f), foundation, art, true);
        Block("LobbyFloor", new Vector3(0, 0.015f, 0), new Vector3(22.8f, 0.03f, 15), grout, art);
        Material[] tiles = { Material("TileA", new Color(0.68f, 0.71f, 0.64f)), Material("TileB", new Color(0.66f, 0.69f, 0.62f)) };
        var corridor = new GameObject("CorridorTiles").transform;
        corridor.SetParent(art);
        for (int x = 0; x < 12; x++)
            for (int z = 0; z < 8; z++)
                Block("Tile", new Vector3(-10.45f + x * 1.9f, 0.04f, -6.5625f + z * 1.875f), new Vector3(1.888f, 0.025f, 1.863f), tiles[(x + z) % 2], corridor);
        Wall("NorthWall", new Vector3(0, 0, 7.5f), 23, 4.6f, false, wall, cap, trim, art);
        Wall("SouthWall", new Vector3(0, 0, -7.5f), 23, 0.72f, false, wall, cap, trim, art);
        Wall("WestWall", new Vector3(-11.4f, 0, 0), 15, 4.6f, true, wall, cap, trim, art);
        Wall("EastWall", new Vector3(11.4f, 0, 0), 15, 4.6f, true, wall, cap, trim, art);
        Material[] planks = { Material("PlankA", new Color(0.58f, 0.40f, 0.25f)), Material("PlankB", new Color(0.60f, 0.42f, 0.27f)), Material("PlankC", new Color(0.56f, 0.38f, 0.24f)) };
        for (int index = 0; index < 4; index++)
        {
            float x = index % 2 == 0 ? -7.4f : 7.4f;
            float z = index < 2 ? -4.75f : 4.75f;
            float side = Mathf.Sign(x);
            var room = new GameObject("Room_" + (index + 1) + "_Decor").transform;
            room.SetParent(art);
            Material accent = Material("Player" + (index + 1), Accents[index]);
            Material pale = Material("Player" + (index + 1) + "Pale", Color.Lerp(Accents[index], cream.color, 0.55f));
            for (int row = 0; row < 11; row++)
                Block("Floorboard", new Vector3(x, 0.045f, z - 2.45f + row * 0.49f), new Vector3(7.5f, 0.03f, 0.475f), planks[(row + index) % 3], room);
            float innerX = side * 3.5f;
            float doorZ = index < 2 ? -2 : 2;
            Wall("RoomSide", new Vector3(innerX, 0, z), 5.5f, 1.1f, true, wall, cap, trim, room);
            Wall("DoorLeft", new Vector3(x - 2.55f, 0, doorZ), 2.7f, 1.0f, false, wall, cap, trim, room);
            Wall("DoorRight", new Vector3(x + 2.55f, 0, doorZ), 2.7f, 1.0f, false, wall, cap, trim, room);
            Block("Doorstep", new Vector3(x, 0.065f, doorZ), new Vector3(2.4f, 0.035f, 0.45f), accent, room);
            Block("RugBorder", new Vector3(x + 0.45f, 0.075f, z), new Vector3(2.7f, 0.035f, 3.1f), pale, room);
            Block("Rug", new Vector3(x + 0.45f, 0.097f, z), new Vector3(2.48f, 0.015f, 2.88f), accent, room);
            for (int stripe = 0; stripe < 2; stripe++)
                Block("RugStripe", new Vector3(x + 0.45f, 0.11f, z - 1.0f + stripe * 2.0f), new Vector3(2.35f, 0.008f, 0.05f), pale, room);
            float bedX = x + side * 2.35f;
            Block("BedFrame", new Vector3(bedX, 0.28f, z - 0.7f), new Vector3(1.65f, 0.43f, 2.9f), wood, room, true);
            Block("Mattress", new Vector3(bedX, 0.56f, z - 0.7f), new Vector3(1.49f, 0.24f, 2.65f), cream, room);
            Block("Blanket", new Vector3(bedX, 0.72f, z - 1.05f), new Vector3(1.53f, 0.10f, 1.9f), pale, room);
            Block("BlanketFold", new Vector3(bedX, 0.79f, z - 0.2f), new Vector3(1.54f, 0.07f, 0.22f), accent, room);
            Block("Pillow", new Vector3(bedX, 0.76f, z + 0.15f), new Vector3(1.12f, 0.20f, 0.55f), cream, room);
            Block("Headboard", new Vector3(bedX, 0.62f, z + 0.75f), new Vector3(1.75f, 1.12f, 0.16f), wood, room);
            float tableX = x - side * 2.35f;
            Block("BedsideTable", new Vector3(tableX, 0.42f, z - 1.9f), new Vector3(1.05f, 0.72f, 0.90f), wood, room, true);
            Block("TableTop", new Vector3(tableX, 0.81f, z - 1.9f), new Vector3(1.15f, 0.08f, 1.0f), cream, room);
            Block("Book", new Vector3(tableX, 0.89f, z - 1.9f), new Vector3(0.5f, 0.08f, 0.4f), accent, room);
            Plant(new Vector3(x - side * 2.5f, 0.08f, z + 2.0f), pot, leaf, room);
            Sign("ROOM " + (index + 1).ToString("00"), new Vector3(x + side * 2.35f, 0.73f, doorZ - 0.14f), new Vector2(2.3f, 0.52f), dark, cream.color, room, 0.078f);
        }
        var readyRoom = new GameObject("ReadyRoomDecor").transform;
        readyRoom.SetParent(art);
        Material green = Material("ReadyGreen", new Color(0.42f, 0.62f, 0.48f));
        Material mint = Material("ReadyMint", new Color(0.76f, 0.84f, 0.66f));
        Block("StartFloorBorder", new Vector3(0, 0.04f, 4.85f), new Vector3(6.8f, 0.04f, 5.1f), cream, readyRoom);
        Block("StartFloor", new Vector3(0, 0.075f, 4.85f), new Vector3(6.55f, 0.025f, 4.85f), green, readyRoom);
        for (int index = 0; index < 4; index++)
        {
            Vector3 spot = new Vector3(-2.1f + index * 1.4f, 0.10f, 4.8f);
            Primitive("ReadySpotBorder", PrimitiveType.Cylinder, spot, new Vector3(1.05f, 0.008f, 1.05f), mint, readyRoom);
            Primitive("ReadySpot", PrimitiveType.Cylinder, spot + Vector3.up * 0.01f, new Vector3(0.86f, 0.008f, 0.86f), green, readyRoom);
        }
        Sign("READY ROOM", new Vector3(0, 1.1f, 7.32f), new Vector2(5.3f, 0.85f), wood, cream.color, readyRoom, 0.13f);
        for (int step = 0; step < 3; step++)
        {
            GameObject left = Block("Wayfinding", new Vector3(-0.22f, 0.09f, -0.5f + step * 0.7f), new Vector3(0.62f, 0.025f, 0.12f), cream, art);
            GameObject right = Block("Wayfinding", new Vector3(0.22f, 0.09f, -0.5f + step * 0.7f), new Vector3(0.62f, 0.025f, 0.12f), cream, art);
            left.transform.rotation = Quaternion.Euler(0, -35, 0);
            right.transform.rotation = Quaternion.Euler(0, 35, 0);
        }
        Bench(new Vector3(0, 0.08f, -6.3f), wood, cream, art);
        Block("CoffeeTable", new Vector3(0, 0.47f, -3.8f), new Vector3(2.0f, 0.80f, 1.2f), wood, art, true);
        Block("CoffeeTableTop", new Vector3(0, 0.91f, -3.8f), new Vector3(2.2f, 0.10f, 1.4f), cream, art);
        Plant(new Vector3(-2.9f, 0.08f, -6.6f), pot, leaf, art);
        Plant(new Vector3(2.9f, 0.08f, -6.6f), pot, leaf, art);
        var cameraObject = new GameObject("LobbyCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        Vector3 target = new Vector3(0, 0.65f, 0);
        cameraObject.transform.SetPositionAndRotation(target - ViewRotation * Vector3.forward * 74, ViewRotation);
        camera.orthographic = false;
        camera.fieldOfView = 15;
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
            var wallAppearance = CaptureOuterWalls(scene);
            Camera oldCamera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).FirstOrDefault();
            float savedFieldOfView = oldCamera != null ? oldCamera.fieldOfView : 0;
            Quaternion savedRotation = oldCamera != null ? oldCamera.transform.rotation : ViewRotation;
            var oldNames = new HashSet<string>
            {
                ArtRoot, "Floor", "NorthWall", "SouthWall", "WestWall", "EastWall", "Room_1", "Room_2", "Room_3", "Room_4",
                "StartFloor", "StartLeft", "StartRight", "START ROOM", "SHARED CORRIDOR", "PLAYER 1", "PLAYER 2", "PLAYER 3", "PLAYER 4", "LobbyCamera", "LobbyLight"
            };
            foreach (GameObject root in scene.GetRootGameObjects())
                if (oldNames.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
            CreatePresentation();
            RestoreOuterWalls(scene, wallAppearance);
            if (savedFieldOfView > 0)
            {
                Camera newCamera = scene.GetRootGameObjects().Single(root => root.name == "LobbyCamera").GetComponent<Camera>();
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
                zone.size = new Vector3(6.4f, 5, 4.6f);
            }
            LobbyPlayerVisualSetup.Apply();
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
        Transform scenery = scene.GetRootGameObjects().FirstOrDefault(root => root.name == ArtRoot)?.transform;
        if (scenery == null) return result;
        foreach (string side in new[] { "NorthWall", "EastWall", "WestWall" })
            foreach (string suffix in new[] { "", "Cap", "Skirting" })
            {
                Transform wall = scenery.Find(side + suffix);
                if (wall != null) result[wall.name] = (wall.localPosition, wall.localRotation, wall.localScale);
            }
        return result;
    }

    private static void RestoreOuterWalls(Scene scene, Dictionary<string, (Vector3 position, Quaternion rotation, Vector3 scale)> appearance)
    {
        Transform scenery = scene.GetRootGameObjects().Single(root => root.name == ArtRoot).transform;
        foreach (var entry in appearance)
        {
            Transform wall = scenery.Find(entry.Key);
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
        if (!layout.TryGetStartCenter(out Vector3 startCenter) || !layout.Contains(startCenter) || layout.Contains(new Vector3(0, 1.1f, 0)))
            throw new InvalidOperationException("The start room bounds changed.");
        foreach (Transform item in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                throw new InvalidOperationException("Missing script on " + item.name);
    }
}
#endif
