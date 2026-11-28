#if UNITY_EDITOR
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Adds the menu-only scene copy to both configured renderers without replacing their other features.
public static class HudMenuCaptureSetup
{
    public static void Install()
    {
        const string materialPath = "Assets/Project/Gameplay/UI/PauseMenu/MenuGaussianBlur.mat";
        var blurMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (blurMaterial == null)
        {
            var shader = Shader.Find("Hidden/Kucharze/Menu Gaussian Blur");
            if (shader == null) throw new System.InvalidOperationException("Import the Gaussian blur shader first.");
            blurMaterial = new Material(shader) { name = "MenuGaussianBlur" };
            AssetDatabase.CreateAsset(blurMaterial, materialPath);
        }
        foreach (string path in new[] { "Assets/Unity/Settings/PC_Renderer.asset", "Assets/Unity/Settings/Mobile_Renderer.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (!data.rendererFeatures.OfType<HudMenuCaptureFeature>().Any())
            {
                var feature = ScriptableObject.CreateInstance<HudMenuCaptureFeature>();
                feature.name = "Pause Menu Scene Capture";
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
            }
            foreach (var feature in data.rendererFeatures.OfType<HudMenuCaptureFeature>())
            {
                var settings = new SerializedObject(feature);
                settings.FindProperty("blurMaterial").objectReferenceValue = blurMaterial;
                settings.ApplyModifiedPropertiesWithoutUndo();
                feature.Create();
                EditorUtility.SetDirty(feature);
            }
            var serialized = new SerializedObject(data);
            var map = serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize = data.rendererFeatures.Count;
            for (int i = 0; i < data.rendererFeatures.Count; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i], out string guid, out long id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            data.SetDirty();
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        UpdateDimmers();
    }

    private static void UpdateDimmers()
    {
        foreach (string path in new[] { ProjectAssetPaths.LobbyScene, ProjectAssetPaths.GameScene })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new System.InvalidOperationException("Save the scene before updating the menu dimmer.");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var backdrop in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HudMenuBackdrop>(true)))
                    backdrop.transform.Find("Dimmer").GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
                if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Could not save the menu dimmer.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
#endif
