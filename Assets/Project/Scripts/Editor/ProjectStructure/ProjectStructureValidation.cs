#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;

// Checks the organized scene and asset locations without rebuilding user scenes.
public static class ProjectStructureValidation
{
    [MenuItem("Tools/Kucharze/Project/Validate Folder Layout")]
    public static void Validate()
    {
        string[] roots = { "Assets/Project", "Assets/ThirdParty", "Assets/Unity" };
        foreach (string folder in AssetDatabase.GetSubFolders("Assets"))
            if (!roots.Contains(folder))
                throw new InvalidOperationException("Assets must contain only Project, ThirdParty and Unity: " + folder);
        foreach (string file in Directory.EnumerateFiles("Assets", "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(file);
            if (string.Equals(name, "todo.md", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "tasks.md", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "todo.md.meta", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "tasks.md.meta", StringComparison.OrdinalIgnoreCase)) continue;
            if (!roots.Any(root => string.Equals(file.Replace('\\', '/'), root + ".meta", StringComparison.Ordinal)))
                throw new InvalidOperationException("Place root assets in Project, ThirdParty or Unity: " + name);
        }
#if KUCHARZE_ONLINE
        if (Unity.Netcode.Editor.Configuration.NetworkPrefabProcessor.DefaultNetworkPrefabsPath != ProjectAssetPaths.DefaultNetworkPrefabs)
            throw new InvalidOperationException("Netcode must use the organized default prefab list.");
#endif
        string[] playable = { ProjectAssetPaths.MenuScene, ProjectAssetPaths.LobbyScene, ProjectAssetPaths.GameScene };
        foreach (string path in playable)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null || !EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path))
                throw new InvalidOperationException("Missing playable scene or build entry: " + path);
        if (EditorBuildSettings.scenes.First(scene => scene.enabled).path != ProjectAssetPaths.MenuScene)
            throw new InvalidOperationException("Main Menu must remain the first enabled scene.");
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            if (Path.GetDirectoryName(scene.path).Replace('\\', '/') != ProjectAssetPaths.GameScenes)
                throw new InvalidOperationException("Playable scenes belong directly in Kuba: " + scene.path);
        foreach (string pattern in new[] { "*.unity", "*.prefab", "*.mat", "*.fbx", "*.png", "*.ttf", "*.controller", "*.anim" })
            if (Directory.EnumerateFiles("Assets/Project/Scripts", pattern, SearchOption.AllDirectories).Any())
                throw new InvalidOperationException("Game content must be outside Scripts: " + pattern);
#if KUCHARZE_ONLINE
        MainMenuSceneSetup.Validate();
        LobbySceneSetup.Validate();
#endif
        UnityEngine.Debug.Log("Project folder layout and playable scene entries are valid.");
    }
}
#endif
