#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Groups scene assets while keeping their references and package resource paths.
public static class ProjectStructureOrganization
{
    private static readonly string[,] Moves =
    {
        { "Assets/Settings/Build Profiles", "Assets/Project/Settings/BuildProfiles" },
        { "Assets/ThirdParty/TextMesh Pro", "Assets/Unity/TextMesh Pro" },
        { "Assets/DefaultNetworkPrefabs.asset", ProjectAssetPaths.DefaultNetworkPrefabs },
        { "Assets/AGENTS.md", "Assets/Project/Scripts/AGENTS.md" },
        { "Assets/Project/UI/MainMenu", "Assets/Project/MainMenu/UI" },
        { "Assets/Project/UI/HUD", "Assets/Project/Gameplay/UI/HUD" },
        { "Assets/Project/UI/Fonts", "Assets/Project/Shared/UI/Fonts" },
        { "Assets/Project/Models/Characters", "Assets/Project/Shared/Characters/Models" },
        { "Assets/Project/Animations", "Assets/Project/Shared/Characters/Animations" },
        { "Assets/Project/Prefabs/Characters/LobbyPlayer.prefab", ProjectAssetPaths.LobbyPlayer },
        { "Assets/Project/Prefabs/Characters/Player.prefab", "Assets/Project/Gameplay/Prefabs/Characters/Player.prefab" },
        { "Assets/Project/Prefabs/Cameras/LobbyCamera.prefab", ProjectAssetPaths.LobbyCamera },
        { "Assets/Project/Prefabs/Cameras/Camera.prefab", "Assets/Project/Gameplay/Prefabs/Cameras/Camera.prefab" },
        { "Assets/Project/Prefabs/Networking", ProjectAssetPaths.NetworkPrefabs },
        { "Assets/Project/Placeholders/Characters", "Assets/Project/Placeholders/Lobby/Characters" }
    };

    private sealed class Snapshot
    {
        public string Path;
        public string Guid;
        public string Hash;
    }

    [MenuItem("Tools/Kucharze/Project/Organize Scene Assets")]
    public static void Organize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for imports and compilation, and exit Play Mode first.");
        foreach (Scene scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
            if (scene.isDirty) throw new InvalidOperationException("Save your open scene first: " + scene.name);
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene.isDirty) throw new InvalidOperationException("Save your open prefab first.");

        // Check all destinations before moving anything; never overwrite existing assets.
        var pending = new List<int>();
        var snapshots = new List<Snapshot>();
        for (int index = 0; index < Moves.GetLength(0); index++)
        {
            string source = Moves[index, 0], target = Moves[index, 1];
            if (!Exists(source)) continue;
            if (Exists(target)) throw new InvalidOperationException("Both locations exist: " + source + " and " + target);
            if (Directory.Exists(source) && ContainsPrivateFile(source))
                throw new InvalidOperationException("A private file prevents moving this folder: " + source);
            pending.Add(index);
            Capture(source, source, target, snapshots);
        }
        string[] scenes = EditorBuildSettings.scenes.Select(scene => scene.enabled + ":" + scene.path).ToArray();
#if KUCHARZE_ONLINE
        // Save the list path before importing, so Netcode cannot recreate a root asset.
        var settings = Unity.Netcode.Editor.Configuration.NetcodeForGameObjectsProjectSettings.instance;
        settings.NetworkPrefabsPath = ProjectAssetPaths.DefaultNetworkPrefabs;
        settings.TempNetworkPrefabsPath = settings.NetworkPrefabsPath;
        typeof(Unity.Netcode.Editor.Configuration.NetcodeForGameObjectsProjectSettings)
            .GetMethod("SaveSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(settings, null);
#endif
        foreach (int index in pending)
        {
            // Earlier moves may supply a parent folder needed by the next move.
            ProjectAssetPaths.EnsureFolder(Path.GetDirectoryName(Moves[index, 1]).Replace('\\', '/'));
            string error = AssetDatabase.MoveAsset(Moves[index, 0], Moves[index, 1]);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (Snapshot snapshot in snapshots)
            if (AssetDatabase.AssetPathToGUID(snapshot.Path) != snapshot.Guid
                || (snapshot.Hash != null && Hash(snapshot.Path) != snapshot.Hash))
                throw new InvalidOperationException("Moved asset changed unexpectedly: " + snapshot.Path);
        if (!scenes.SequenceEqual(EditorBuildSettings.scenes.Select(scene => scene.enabled + ":" + scene.path)))
            throw new InvalidOperationException("The build scene list changed unexpectedly.");
        foreach (string root in new[] { "Assets/Settings", "Assets/Project/UI", "Assets/Project/Models", "Assets/Project/Prefabs" })
            RemoveEmptyFolders(root);
        AssetDatabase.SaveAssets();
        ProjectStructureValidation.Validate();
        Debug.Log("Scene asset organization complete: " + pending.Count + " moves; " + snapshots.Count
            + " GUID/content checks passed; build scene entries preserved.");
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    private static bool IsPrivate(string path)
        => string.Equals(Path.GetFileName(path), "todo.md", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFileName(path), "tasks.md", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsPrivateFile(string folder)
        => Directory.EnumerateFiles(folder).Any(IsPrivate)
            || Directory.EnumerateDirectories(folder).Any(ContainsPrivateFile);

    private static void Capture(string path, string source, string target, List<Snapshot> snapshots)
    {
        snapshots.Add(new Snapshot
        {
            Path = target + path.Substring(source.Length).Replace('\\', '/'),
            Guid = AssetDatabase.AssetPathToGUID(path.Replace('\\', '/')),
            Hash = Directory.Exists(path) ? null : Hash(path)
        });
        if (!Directory.Exists(path)) return;
        foreach (string child in Directory.EnumerateDirectories(path)) Capture(child, source, target, snapshots);
        foreach (string child in Directory.EnumerateFiles(path))
            if (!IsPrivate(child) && !child.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) Capture(child, source, target, snapshots);
    }

    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var algorithm = SHA256.Create())
            return BitConverter.ToString(algorithm.ComputeHash(stream));
    }

    private static void RemoveEmptyFolders(string folder)
    {
        if (!Directory.Exists(folder)) return;
        foreach (string child in Directory.EnumerateDirectories(folder)) RemoveEmptyFolders(child.Replace('\\', '/'));
        if (!Directory.EnumerateFileSystemEntries(folder).Any()) AssetDatabase.DeleteAsset(folder);
    }
}
#endif
