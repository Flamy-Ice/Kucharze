#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Updates interaction feedback without rebuilding the menu's layout or replacing assigned artwork.
public static class MainMenuFeedbackSetup
{
    [MenuItem("Tools/Kucharze/Main Menu/Refresh Menu Interaction")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(MainMenuSceneSetup.ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save your menu changes before updating its interaction feedback.");
        if (opened) scene = EditorSceneManager.OpenScene(MainMenuSceneSetup.ScenePath, OpenSceneMode.Additive);
        try
        {
            ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void ApplyToScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EventSystem events in root.GetComponentsInChildren<EventSystem>(true)) events.firstSelectedGameObject = null;
            foreach (Selectable control in root.GetComponentsInChildren<Selectable>(true))
            {
                bool topLevelToggle = control is Toggle && control.transform.parent != null && control.transform.parent.name == "OptionsCard";
                if (!(control is Button) && !(control is Dropdown) && !topLevelToggle) continue;
                MenuSelectableScale motion = control.GetComponent<MenuSelectableScale>();
                if (control is Dropdown || topLevelToggle)
                {
                    if (motion != null) UnityEngine.Object.DestroyImmediate(motion);
                    control.transform.localScale = Vector3.one;
                    ColorBlock colors = control.colors;
                    colors.highlightedColor = colors.normalColor;
                    colors.selectedColor = colors.normalColor;
                    control.colors = colors;
                }
                else
                {
                    if (motion == null) motion = control.gameObject.AddComponent<MenuSelectableScale>();
                    var settings = new SerializedObject(motion);
                    settings.FindProperty("responseSeconds").floatValue = 0.035f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                if (control.GetComponent<MenuSoundFeedback>() == null) control.gameObject.AddComponent<MenuSoundFeedback>();
                if (topLevelToggle) StyleCheckbox((Toggle)control);
            }
        }
        MenuUISounds sounds = scene.GetRootGameObjects().Select(root => root.GetComponent<MenuUISounds>()).FirstOrDefault(item => item != null);
        if (sounds == null)
        {
            var owner = new GameObject("Menu UI Sounds - assign clips here");
            SceneManager.MoveGameObjectToScene(owner, scene);
            sounds = owner.AddComponent<MenuUISounds>();
        }
        var audio = new SerializedObject(sounds);
        SerializedProperty output = audio.FindProperty("outputMixerGroup");
        if (output.objectReferenceValue == null)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Project/Scripts/Player/Audio/Mixers/GameAudio.mixer");
            if (mixer != null) output.objectReferenceValue = mixer.FindMatchingGroups("SFX").Single();
            audio.ApplyModifiedPropertiesWithoutUndo();
        }
        AudioSource source = sounds.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.outputAudioMixerGroup = (AudioMixerGroup)output.objectReferenceValue;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    public static void StyleCheckbox(Toggle toggle)
    {
        Image background = toggle.targetGraphic as Image;
        if (background == null) return;
        background.color = new Color32(5, 158, 148, 255);
        Outline outline = background.GetComponent<Outline>();
        if (outline == null) outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(6, 79, 74, 255);
        outline.effectDistance = new Vector2(2, -2);
        if (toggle.graphic != null)
        {
            toggle.graphic.color = new Color32(37, 33, 56, 255);
            toggle.graphic.rectTransform.sizeDelta = new Vector2(34, 34);
        }
    }
}
#endif
