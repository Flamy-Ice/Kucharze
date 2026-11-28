#if UNITY_EDITOR
using System;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Adds screen entrances without rebuilding the menu or changing saved button callbacks.
public static class MainMenuEntranceSetup
{
    [MenuItem("Tools/Kucharze/Main Menu/Add Panel Entrance")]
    public static void Refresh() => Refresh(false);

    public static void Refresh(bool resetSettings)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene scene = SceneManager.GetSceneByPath(ProjectAssetPaths.MenuScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save Main Menu before adding its entrance.");
        if (opened) scene = EditorSceneManager.OpenScene(ProjectAssetPaths.MenuScene, OpenSceneMode.Additive);
        try
        {
            ApplyToScene(scene, resetSettings);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Could not save the menu entrance.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    public static void ApplyToScene(Scene scene, bool resetSettings = false)
    {
        var screens = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UnityEngine.Transform>(true))
            .Where(item => new[] { "HomeCanvas", "PlayCanvas", "OptionsCanvas", "CreditsCanvas" }.Contains(item.name)).ToArray();
        if (screens.Length != 4) throw new InvalidOperationException("Expected all four menu screens.");
        foreach (var screen in screens)
        {
            var entrance = screen.GetComponent<MenuPanelEntrance>();
            if (entrance == null) entrance = screen.gameObject.AddComponent<MenuPanelEntrance>();
            if (!resetSettings) continue;
            var settings = new SerializedObject(entrance);
            settings.FindProperty("durationSeconds").floatValue = 0.55f;
            settings.FindProperty("intensity").floatValue = 1f;
            settings.FindProperty("startingScale").floatValue = 0.76f;
            settings.FindProperty("startingOffset").vector2Value = new UnityEngine.Vector2(0, -150);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
#endif
