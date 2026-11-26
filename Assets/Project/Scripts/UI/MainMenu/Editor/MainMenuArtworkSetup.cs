#if UNITY_EDITOR && KUCHARZE_ONLINE
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Imports the supplied button artwork and applies it without replacing button events.
public static class MainMenuArtworkSetup
{
    public static void ApplyToScene(Scene scene)
    {
        foreach (string name in new[] { "Back", "HostGame", "JoinGame", "Cancel" })
        {
            string path = ProjectAssetPaths.MenuButtons + "/MainMenu_Button_" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new System.InvalidOperationException("Missing menu artwork: " + path);
            importer.textureType = TextureImporterType.Sprite;
            if (importer.spriteImportMode == SpriteImportMode.None) importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
            foreach (Button button in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true)))
            {
                bool matches = name == "Back" ? button.name == "BackButton" : name == "HostGame" ? button.name == "Host a gameButton"
                    : name == "JoinGame" ? button.name == "JoinButton" : button.name == "Cancel connectionButton";
                if (!matches) continue;
                Image image = button.GetComponent<Image>();
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                // These graphics already contain their captions.
                foreach (Text label in button.GetComponentsInChildren<Text>(true)) label.enabled = false;
                ColorBlock colors = button.colors;
                colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1);
                colors.disabledColor = new Color(0.64f, 0.64f, 0.64f, 0.9f);
                button.colors = colors;
            }
        }
    }
}
#endif
