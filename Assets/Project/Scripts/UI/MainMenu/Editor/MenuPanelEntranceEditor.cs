#if UNITY_EDITOR
using System.Collections.Generic;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Previews the real entrance motion and restores every temporarily shown menu screen afterward.
[CustomEditor(typeof(MenuPanelEntrance))]
public sealed class MenuPanelEntranceEditor : Editor
{
    private readonly List<GameObject> screens = new List<GameObject>();
    private readonly List<bool> activeStates = new List<bool>();
    private bool previewing;
    private double lastTick;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        if (EditorGUI.EndChangeCheck()) StopPreview();
        serializedObject.ApplyModifiedProperties();
        var entrance = (MenuPanelEntrance)target;
        EditorGUILayout.HelpBox("Spring Pop: pops into place with a lively spring overshoot.", MessageType.Info);
        EditorGUILayout.LabelField("Duration Seconds: time in seconds. Intensity: strength of the effect.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying && !entrance.gameObject.activeInHierarchy))
        {
            if (GUILayout.Button("Preview Animation")) StartPreview();
        }
        if (previewing || (Application.isPlaying && entrance.IsPlaying))
            if (GUILayout.Button("Stop Preview"))
            {
                if (Application.isPlaying) entrance.Complete(); else StopPreview();
            }
    }

    private void StartPreview()
    {
        var entrance = (MenuPanelEntrance)target;
        if (Application.isPlaying) { entrance.Play(); return; }
        StopPreview();
        Transform parent = entrance.transform.parent;
        if (parent != null)
        {
            foreach (Transform sibling in parent)
            {
                if (sibling.GetComponent<MenuPanelEntrance>() == null) continue;
                screens.Add(sibling.gameObject);
                activeStates.Add(sibling.gameObject.activeSelf);
                sibling.gameObject.SetActive(sibling == entrance.transform);
            }
        }
        previewing = true;
        entrance.Play();
        lastTick = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
    }

    private void Tick()
    {
        if (target == null) { StopPreview(); return; }
        double now = EditorApplication.timeSinceStartup;
        var entrance = (MenuPanelEntrance)target;
        entrance.Advance((float)(now - lastTick));
        lastTick = now;
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Repaint();
        if (!entrance.IsPlaying) StopPreview();
    }

    private void StopPreview()
    {
        if (!previewing) return;
        previewing = false;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        if (target != null) ((MenuPanelEntrance)target).ReleasePreview();
        for (int i = 0; i < screens.Count; i++)
            if (screens[i] != null) screens[i].SetActive(activeStates[i]);
        screens.Clear();
        activeStates.Clear();
        SceneView.RepaintAll();
    }

    private void OnPlayMode(PlayModeStateChange state) => StopPreview();
    private void OnSceneSaving(Scene scene, string path) => StopPreview();
    private void OnDisable() => StopPreview();
}
#endif
