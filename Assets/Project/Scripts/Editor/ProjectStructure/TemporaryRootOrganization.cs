#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TMPro;
using Unity.Netcode.Editor.Configuration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Moves the reviewed root assets once and checks the settings used by Unity packages.
[InitializeOnLoad]
public static class TemporaryRootOrganization
{
    const string Folder = "Assets/Project/Scripts/Editor/ProjectStructure/";
    const string Request = Folder + "RootOrganization.request";
    const string Result = Folder + "RootOrganization.result";
    [Serializable] public class Move { public string source, target, guid, hash; }
    [Serializable] public class Plan { public Move[] moves; }
    static bool busy;
    static TemporaryRootOrganization() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        string command = File.ReadAllText(Request).Trim(); File.Delete(Request);
        try
        {
            busy = true;
            if (command == "apply") Apply();
            else if (command == "validate") Validate();
        }
        catch (Exception error) { File.WriteAllText(Result, "FAILED: " + error); }
        finally { busy = false; }
    }
    static Plan ReadPlan() => JsonUtility.FromJson<Plan>(File.ReadAllText(Folder + "RootOrganization.plan.json"));
    static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var algorithm = SHA256.Create())
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
    }
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
            if (scene.isDirty) throw new InvalidOperationException("Save your open scenes first: " + scene.path);
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene.isDirty) throw new InvalidOperationException("Save your open prefab first.");
        var plan = ReadPlan();
        foreach (var move in plan.moves)
        {
            if (!File.Exists(move.source) || File.Exists(move.target) || Hash(move.source) != move.hash
                || AssetDatabase.AssetPathToGUID(move.source) != move.guid)
                throw new InvalidOperationException("Source changed or destination exists: " + move.source);
            ProjectAssetPaths.EnsureFolder(Path.GetDirectoryName(move.target).Replace('\\', '/'));
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        // Netcode stores its generated list path separately; update it before importing the move.
        var settings = NetcodeForGameObjectsProjectSettings.instance;
        settings.NetworkPrefabsPath = ProjectAssetPaths.DefaultNetworkPrefabs;
        settings.TempNetworkPrefabsPath = settings.NetworkPrefabsPath;
        typeof(NetcodeForGameObjectsProjectSettings).GetMethod("SaveSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(settings, null);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var move in plan.moves)
            {
                string error = AssetDatabase.MoveAsset(move.source, move.target);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(move.source + ": " + error);
            }
            string[] oldRoots = { "Assets/Settings", "Assets/XR", "Assets/CompositionLayers", "Assets/ThirdParty/TextMesh Pro" };
            foreach (string root in oldRoots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .Append(root).OrderByDescending(path => path.Length))
                    if (!Directory.EnumerateFileSystemEntries(directory).Any()) AssetDatabase.DeleteAsset(directory.Replace('\\', '/'));
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Validate();
    }
    public static void Validate()
    {
        var plan = ReadPlan();
        foreach (var move in plan.moves)
            if (AssetDatabase.AssetPathToGUID(move.target) != move.guid || Hash(move.target) != move.hash)
                throw new InvalidOperationException("Moved asset GUID or content changed: " + move.target);
        ProjectStructureValidation.Validate();
        CheckResource("CompositionLayersRuntimeSettings", ProjectAssetPaths.UnityCompositionLayers + "/UserSettings/Resources/CompositionLayersRuntimeSettings.asset");
        CheckResource("XRSimulationRuntimeSettings", ProjectAssetPaths.UnityXR + "/Resources/XRSimulationRuntimeSettings.asset");
        CheckResource("XRSimulationPreferences", ProjectAssetPaths.UnityXR + "/UserSimulationSettings/Resources/XRSimulationPreferences.asset");
        CheckResource("TMP Settings", ProjectAssetPaths.UnityTextMeshPro + "/Resources/TMP Settings.asset");
        Require(TMP_Settings.defaultFontAsset != null, "Default TMP font is missing.");
        foreach (var move in plan.moves.Where(move => move.target.EndsWith(".asset", StringComparison.Ordinal)))
            Require(AssetDatabase.LoadMainAssetAtPath(move.target) != null, "Settings asset could not load: " + move.target);
        foreach (string path in new[] { ProjectAssetPaths.MenuScene, ProjectAssetPaths.LobbyScene, ProjectAssetPaths.GameScene })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var owner in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(owner.gameObject) == 0, "Missing scene script: " + owner.name);
            foreach (var text in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
                Require(text.font != null, "Missing scene font: " + text.name);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Result, "PASS: " + plan.moves.Length + " GUIDs and asset hashes; three root folders; package Resources; default font; Netcode configuration; three playable scenes.");
        Debug.Log("ROOT ORGANIZATION PASS");
    }
    static void CheckResource(string name, string path)
    {
        var expected = AssetDatabase.LoadMainAssetAtPath(path);
        Require(expected != null && Resources.Load(name) == expected, "Resource path changed: " + name);
    }
    static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
#endif
