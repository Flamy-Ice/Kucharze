#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if KUCHARZE_ONLINE
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
#endif

// Prepares a separate online test scene while keeping the original scene and player prefab intact.
[InitializeOnLoad]
public static class OnlineTestSetup
{
    private const string Root = ProjectAssetPaths.DevelopmentNetworking;
    private const string Request = "Assets/Project/Scripts/Networking/Editor/Online/OnlineSetup.request";
    private const string Report = Root + "/OnlineSetup.result";
    public const string ScenePath = ProjectAssetPaths.OnlineTestScene;
    private static double nextPoll;
    private static bool processing;

    static OnlineTestSetup()
    {
        EditorApplication.delayCall += ProcessRequest;
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1d;
        ProcessRequest();
    }

    private static void ProcessRequest()
    {
        if (processing || !File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string operation = File.ReadAllText(Request).Trim();
        processing = true;
        File.Delete(Request);
        try
        {
            if (operation == "configure") Prepare();
            else if (operation == "build" || operation == "complete-and-build")
            {
                if (operation == "complete-and-build") ConfigureSharedState();
                OnlineTestBuildQueue.Request(Report);
                return;
            }
            else if (operation == "services-status")
            {
                File.WriteAllText(Report, OnlineServicesEditorStatus.Describe());
                return;
            }
            else if (operation == "link-services")
            {
                _ = OnlineServicesProjectLink.LinkConfiguredProjectAsync(Report);
                return;
            }
            else throw new InvalidOperationException("Unknown setup request.");
            File.WriteAllText(Report, "SUCCESS: " + operation + "\n" + ScenePath);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Report, "FAILED: " + exception);
        }
        finally
        {
            processing = false;
        }
    }

    [MenuItem("Tools/Kucharze/Online/Prepare Test Scene")]
    public static void Prepare()
    {
#if KUCHARZE_ONLINE
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before preparing the scene.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("The online test already exists. Open it instead of replacing it.");
        Scene previousActive = SceneManager.GetActiveScene();
        bool hasUnsavedScenes = Enumerable.Range(0, SceneManager.sceneCount)
            .Any(index => SceneManager.GetSceneAt(index).isDirty);
        ProjectAssetPaths.EnsureFolder(Root);
        ProjectAssetPaths.EnsureFolder(ProjectAssetPaths.PrototypeScenes);
        AssetDatabase.Refresh();
        const string sourcePath = ProjectAssetPaths.LocalPrototypeScene;
        Scene original = SceneManager.GetSceneByPath(sourcePath);
        bool openedSource = !original.IsValid() || !original.isLoaded;
        if (openedSource) original = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
        if (!EditorSceneManager.SaveScene(original, ScenePath, true))
            throw new IOException("Could not save the online scene copy.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var players = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerMovementResolver>(true)).ToArray();
        if (players.Length == 0) throw new InvalidOperationException("The source scene has no movement player.");
        PlayerMovementResolver source = players.FirstOrDefault(player => player.gameObject.activeInHierarchy) ?? players[0];
        GameObject player = UnityEngine.Object.Instantiate(source.gameObject);
        player.name = "OnlinePlayer";
        player.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(player, scene);
        if (PrefabUtility.IsPartOfPrefabInstance(player))
            PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        GetOrAdd<NetworkObject>(player);
        NetworkTransform networkTransform = GetOrAdd<NetworkTransform>(player);
        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        networkTransform.Interpolate = true;
        networkTransform.SyncRotAngleX = networkTransform.SyncRotAngleY = networkTransform.SyncRotAngleZ = false;
        networkTransform.SyncScaleX = networkTransform.SyncScaleY = networkTransform.SyncScaleZ = false;
        GetOrAdd<PlayerNetworkInput>(player);
        GetOrAdd<PlayerNetworkState>(player);
        GetOrAdd<PlayerNetworkSpawn>(player);
        // Multiple diagnostic overlays would cover the connection panel in this test scene.
        foreach (PlayerMovementDebug debug in player.GetComponentsInChildren<PlayerMovementDebug>(true)) debug.enabled = false;
        var input = player.GetComponent<PlayerInputActions>();
        if (input.InputActionAsset == null)
            input.InputActionAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/Project/Scripts/Player/Movement/Input/PlayerControls.inputactions");
        if (input.InputActionAsset == null) throw new InvalidOperationException("The player needs its PlayerControls asset.");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, Root + "/OnlinePlayer.prefab");
        UnityEngine.Object.DestroyImmediate(player);
        foreach (PlayerMovementResolver local in players)
            if (local != null) UnityEngine.Object.DestroyImmediate(local.gameObject);

        if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<NetworkManager>(true)).Any())
            throw new InvalidOperationException("The source scene already contains a NetworkManager.");
        var session = new GameObject("OnlineSession");
        SceneManager.MoveGameObjectToScene(session, scene);
        var transport = session.AddComponent<UnityTransport>();
        var manager = session.AddComponent<NetworkManager>();
        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.PlayerPrefab = prefab;
        manager.NetworkConfig.EnableSceneManagement = true;
        manager.NetworkConfig.ConnectionApproval = false;
        manager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = true;
        var prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
        prefabs.Add(new NetworkPrefab { Prefab = prefab });
        AssetDatabase.CreateAsset(prefabs, Root + "/OnlineNetworkPrefabs.asset");
        manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
        manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabs);
        session.AddComponent<RelayConnection>();
        session.AddComponent<RelayConnectionMenu>();
        // Keep a desktop host simulating while another game window has focus.
        var settings = new SerializedObject(manager);
        SerializedProperty runInBackground = settings.FindProperty("RunInBackground");
        if (runInBackground != null) runInBackground.boolValue = true;
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the configured scene.");
        AssetDatabase.SaveAssets();
        if (hasUnsavedScenes)
        {
            // Keep unsaved work in memory; the completed test scene can be opened separately.
            if (previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
            EditorSceneManager.CloseScene(scene, true);
            if (openedSource) EditorSceneManager.CloseScene(original, true);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }
        else
        {
            var otherScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).Where(loaded => loaded != scene).ToArray();
            foreach (Scene other in otherScenes) EditorSceneManager.CloseScene(other, true);
            Selection.activeObject = session;
        }
#else
        throw new InvalidOperationException("Install the networking packages and enable KUCHARZE_ONLINE first.");
#endif
    }

#if KUCHARZE_ONLINE
    private static T GetOrAdd<T>(GameObject target) where T : Component
        => target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();
#endif

    [MenuItem("Tools/Kucharze/Online/Build Windows Test")]
    public static void Build()
    {
        OnlineTestBuildQueue.Request(Report);
    }

    [MenuItem("Tools/Kucharze/Online/Configure Shared Wallet And Timer")]
    public static void ConfigureSharedState()
    {
#if KUCHARZE_ONLINE
        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CoinWallet wallet in root.GetComponentsInChildren<CoinWallet>(true))
            {
                if (wallet.transform.parent != null && wallet.transform.parent.GetComponentInParent<NetworkObject>() != null)
                    throw new InvalidOperationException("A wallet cannot be nested inside another NetworkObject.");
                GetOrAdd<NetworkObject>(wallet.gameObject);
                GetOrAdd<CoinWalletNetwork>(wallet.gameObject);
            }
            foreach (CountdownTimer timer in root.GetComponentsInChildren<CountdownTimer>(true))
            {
                if (timer.transform.parent != null && timer.transform.parent.GetComponentInParent<NetworkObject>() != null)
                    throw new InvalidOperationException("A timer cannot be nested inside another NetworkObject.");
                GetOrAdd<NetworkObject>(timer.gameObject);
                GetOrAdd<CountdownTimerNetwork>(timer.gameObject);
            }
        }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (opened)
        {
            if (previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
            EditorSceneManager.CloseScene(scene, true);
        }
#else
        throw new InvalidOperationException("Enable KUCHARZE_ONLINE first.");
#endif
    }
}
#endif
