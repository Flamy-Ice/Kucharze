#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using ModularMovement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Builds a small lobby from primitives and movable markers without editing the original scenes.
[InitializeOnLoad]
public static class LobbySceneSetup
{
    public const string Root = ProjectAssetPaths.NetworkPrefabs;
    public const string LobbyPath = ProjectAssetPaths.LobbyScene;
    public const string RoundPath = ProjectAssetPaths.GameScene;
    private const string Request = "Assets/Project/Scripts/Networking/Editor/Lobby/LobbySetup.request";
    private const string Report = ProjectAssetPaths.DevelopmentNetworking + "/LobbySetup.result";
    private static double nextPoll;

    static LobbySceneSetup() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string operation = File.ReadAllText(Request).Trim();
        if (!AssetDatabase.DeleteAsset(Request)) File.Delete(Request);
        try
        {
            if (operation == "create") Create();
            else if (operation == "validate") Validate();
            else if (operation == "preview") Preview();
            else if (operation == "build")
            {
                BuildLobbyTest();
                return;
            }
            else throw new InvalidOperationException("Unknown lobby operation.");
            File.WriteAllText(Report, "SUCCESS: " + operation + "\n" + LobbyPath);
        }
        catch (Exception exception) { File.WriteAllText(Report, "FAILED: " + exception); }
    }

    [MenuItem("Tools/Kucharze/Online/Create Simple Lobby")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (File.Exists(LobbyPath) || File.Exists(RoundPath) || File.Exists(ProjectAssetPaths.LobbyPlayer) || File.Exists(Root + "/LobbyConnection.prefab")) throw new InvalidOperationException("Lobby assets already exist; they will not be overwritten.");
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Development/Networking/OnlinePlayer.prefab");
        if (source == null || !File.Exists(OnlineTestSetup.ScenePath))
            throw new InvalidOperationException("Prepare the existing online test scene and player first.");
        Scene previous = SceneManager.GetActiveScene();
        Scene lobby = default;
        Scene round = default;
        try
        {
            ProjectAssetPaths.EnsureFolder(ProjectAssetPaths.GameScenes);
            ProjectAssetPaths.EnsureFolder(Root);
            ProjectAssetPaths.EnsureFolder(ProjectAssetPaths.CharacterPrefabs);
            AssetDatabase.Refresh();
            lobby = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(lobby);
            GameObject playerInstance = UnityEngine.Object.Instantiate(source);
            var oldSpawn = playerInstance.GetComponent<PlayerNetworkSpawn>();
            if (oldSpawn != null) UnityEngine.Object.DestroyImmediate(oldSpawn);
            playerInstance.name = "LobbyPlayer";
            GameObject playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerInstance, ProjectAssetPaths.LobbyPlayer);
            UnityEngine.Object.DestroyImmediate(playerInstance);

            var rules = new GameObject("LobbySession");
            rules.AddComponent<NetworkObject>();
            var session = rules.AddComponent<LobbySession>();
            SetString(session, "lobbyScene", LobbyPath);
            SetString(session, "gameScene", RoundPath);
            GameObject sessionPrefab = PrefabUtility.SaveAsPrefabAsset(rules, Root + "/LobbySession.prefab");
            UnityEngine.Object.DestroyImmediate(rules);

            var prefabList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            prefabList.Add(new NetworkPrefab { Prefab = playerPrefab });
            prefabList.Add(new NetworkPrefab { Prefab = sessionPrefab });
            AssetDatabase.CreateAsset(prefabList, Root + "/LobbyNetworkPrefabs.asset");
            var connection = new GameObject("LobbyConnection");
            var transport = connection.AddComponent<UnityTransport>();
            var manager = connection.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.EnableSceneManagement = true;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = true;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
            connection.AddComponent<RelayConnection>();
            connection.AddComponent<LobbyConnectionView>();
            var bootstrap = connection.AddComponent<LobbySessionBootstrap>();
            SetReference(bootstrap, "sessionPrefab", sessionPrefab.GetComponent<NetworkObject>());
            var managerSettings = new SerializedObject(manager);
            SerializedProperty background = managerSettings.FindProperty("RunInBackground");
            if (background != null) background.boolValue = true;
            managerSettings.ApplyModifiedPropertiesWithoutUndo();
            GameObject connectionPrefab = PrefabUtility.SaveAsPrefabAsset(connection, Root + "/LobbyConnection.prefab");
            UnityEngine.Object.DestroyImmediate(connection);

            BuildLobby(connectionPrefab);
            if (!EditorSceneManager.SaveScene(lobby, LobbyPath)) throw new IOException("Could not save Lobby.");

            if (!AssetDatabase.CopyAsset(OnlineTestSetup.ScenePath, RoundPath)) throw new IOException("Could not copy the test round.");
            round = EditorSceneManager.OpenScene(RoundPath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(round);
            foreach (GameObject root in round.GetRootGameObjects())
                foreach (NetworkManager oldManager in root.GetComponentsInChildren<NetworkManager>(true))
                    UnityEngine.Object.DestroyImmediate(oldManager.gameObject);
            AddLayout("RoundSpawnMarkers", new[] { source.transform.position, source.transform.position + Vector3.right * 2,
                source.transform.position + Vector3.right * 4, source.transform.position + Vector3.right * 6 }, null);
            if (!EditorSceneManager.SaveScene(round)) throw new IOException("Could not save the test round.");
            // Keep the user's existing build entries; NGO needs both new scene paths enabled.
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.Add(new EditorBuildSettingsScene(LobbyPath, true));
            scenes.Add(new EditorBuildSettingsScene(RoundPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (round.IsValid() && round.isLoaded) EditorSceneManager.CloseScene(round, true);
            if (lobby.IsValid() && lobby.isLoaded) EditorSceneManager.CloseScene(lobby, true);
        }
        Validate();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyPath);
    }

    [MenuItem("Tools/Kucharze/Online/Build Lobby Test")]
    public static void BuildLobbyTest()
    {
        Validate();
        OnlineTestBuildQueue.Request(Report, new[] { LobbyPath, RoundPath }, "KucharzeLobbyTest");
    }

    private static void BuildLobby(GameObject connectionPrefab)
    {
        LobbyPresentationBuilder.CreatePresentation();
        LobbyPlayerVisualSetup.Apply();
        var positions = Enumerable.Range(0, 4).Select(LobbyPresentationBuilder.RoomSpawn).ToArray();
        var zone = new GameObject("StartZone_MoveAndResizeMe");
        zone.transform.position = new Vector3(0, 1.5f, 4.85f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6.4f, 5, 4.6f);
        AddLayout("LobbyMarkers", positions, box);
        var entry = new GameObject("LobbyEntry").AddComponent<LobbyEntry>();
        SetReference(entry, "connectionPrefab", connectionPrefab);
    }
    private static void AddLayout(string name, Vector3[] positions, BoxCollider zone)
    {
        var layout = new GameObject(name).AddComponent<LobbyLayout>();
        var settings = new SerializedObject(layout);
        SerializedProperty spawns = settings.FindProperty("roomSpawns");
        spawns.arraySize = 4;
        for (int index = 0; index < 4; index++)
        {
            var marker = new GameObject("Player" + (index + 1) + "_Spawn_MoveMe");
            marker.transform.SetParent(layout.transform);
            marker.transform.position = positions[index];
            spawns.GetArrayElementAtIndex(index).objectReferenceValue = marker.transform;
        }
        settings.FindProperty("startZone").objectReferenceValue = zone;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object target, string property, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Kucharze/Online/Validate Simple Lobby")]
    public static void Validate()
    {
        if (!File.Exists(LobbyPath) || !File.Exists(RoundPath)) throw new InvalidOperationException("Both lobby scenes must exist.");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/LobbyConnection.prefab");
        NetworkManager manager = prefab.GetComponent<NetworkManager>();
        if (!manager.NetworkConfig.ConnectionApproval || manager.NetworkConfig.PlayerPrefab == null)
            throw new InvalidOperationException("The lobby needs connection approval and a player prefab.");
        foreach (string path in new[] { LobbyPath, RoundPath })
            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path))
                throw new InvalidOperationException("Missing build scene: " + path);
    }

    private static void Preview()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Additive);
        GameObject[] otherRoots = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt).Where(loaded => loaded != scene)
            .SelectMany(loaded => loaded.GetRootGameObjects()).Where(root => root.activeSelf).ToArray();
        RenderTexture target = new RenderTexture(1280, 720, 24);
        Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previousTarget = RenderTexture.active;
        try
        {
            // A preview camera also sees other loaded scenes unless their roots are temporarily disabled.
            foreach (GameObject root in otherRoots) root.SetActive(false);
            Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
            camera.targetTexture = target;
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
            else camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Report + ".png", texture.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            foreach (GameObject root in otherRoots) if (root != null) root.SetActive(true);
            RenderTexture.active = previousTarget;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // A separate batch editor can check the saved scene without touching the user's open scenes.
    public static void BatchValidateAndBuild()
    {
        Validate();
        Preview();
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { LobbyPath, RoundPath },
            locationPathName = Path.Combine(Directory.GetCurrentDirectory(), "TestBuild", "LobbyTest.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("Lobby test build failed.");
    }

    public static void BatchPreview() => Preview();
}
#endif
