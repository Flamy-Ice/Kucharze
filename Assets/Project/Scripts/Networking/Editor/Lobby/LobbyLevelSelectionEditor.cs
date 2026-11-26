#if UNITY_EDITOR && KUCHARZE_ONLINE
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEngine;

// Lets designers assign real scenes and icons to the nine level slots.
[CustomEditor(typeof(LobbyLevelSelection))]
public sealed class LobbyLevelSelectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty levels = serializedObject.FindProperty("levels");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("selectionHoldSeconds"), new GUIContent("Selection Hold (s)"));
        EditorGUILayout.HelpBox("The host selects a level by standing on its pad. Empty or disabled scenes cannot start a game.", MessageType.Info);
        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(index);
            SerializedProperty path = level.FindPropertyRelative("scenePath");
            EditorGUILayout.LabelField("Level " + (index + 1), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(level.FindPropertyRelative("title"), new GUIContent("Title"));
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
            EditorGUI.BeginChangeCheck();
            var chosen = (SceneAsset)EditorGUILayout.ObjectField("Gameplay Scene", scene, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck()) path.stringValue = chosen != null ? AssetDatabase.GetAssetPath(chosen) : "";
            EditorGUILayout.PropertyField(level.FindPropertyRelative("icon"), new GUIContent("Icon"));
            if (string.IsNullOrEmpty(path.stringValue))
                EditorGUILayout.HelpBox("Not configured. This pad is unavailable.", MessageType.None);
            else if (!EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == path.stringValue))
                EditorGUILayout.HelpBox("Enable this scene in File > Build Profiles > Open Scene List.", MessageType.Warning);
            EditorGUILayout.Space(8);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
