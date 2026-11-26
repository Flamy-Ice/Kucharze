#if UNITY_EDITOR
using System;
using UnityEditor;

// Keeps editor generators pointed at the same organized asset folders.
public static class ProjectAssetPaths
{
    public const string BuildProfiles = "Assets/Project/Settings/BuildProfiles";
    public const string DefaultNetworkPrefabs = "Assets/Project/Shared/Networking/DefaultNetworkPrefabs.asset";
    public const string UnityTextMeshPro = "Assets/Unity/TextMesh Pro";
    public const string GameScenes = "Assets/Project/Scenes/Debug/Kuba";
    public const string PrototypeScenes = GameScenes + "/Prototypes";
    public const string MenuScene = GameScenes + "/Main Menu.unity";
    public const string LobbyScene = GameScenes + "/Lobby.unity";
    public const string GameScene = GameScenes + "/Kuba.unity";
    public const string LocalPrototypeScene = PrototypeScenes + "/Kuba.unity";
    public const string OnlineTestScene = PrototypeScenes + "/OnlineTest.unity";
    public const string NetworkPrefabs = "Assets/Project/Lobby/Prefabs/Networking";
    public const string CharacterPrefabs = "Assets/Project/Lobby/Prefabs/Characters";
    public const string CameraPrefabs = "Assets/Project/Lobby/Prefabs/Cameras";
    public const string LobbyPlayer = CharacterPrefabs + "/LobbyPlayer.prefab";
    public const string LobbyCamera = CameraPrefabs + "/LobbyCamera.prefab";
    public const string DevelopmentNetworking = "Assets/Project/Development/Networking";
    public const string LobbyMaterials = "Assets/Project/Placeholders/Lobby/Materials";
    public const string CharacterMaterials = "Assets/Project/Placeholders/Lobby/Characters/Materials";
    public const string CharacterMeshes = "Assets/Project/Placeholders/Lobby/Characters/Meshes";
    public const string MenuArtwork = "Assets/Project/MainMenu/UI/Artwork";
    public const string MenuButtons = "Assets/Project/MainMenu/UI/Buttons";
    public const string FontSources = "Assets/Project/Shared/UI/Fonts/Sources";

    public static string MenuImage(string name)
        => (name.StartsWith("MainMenu_Button_", StringComparison.Ordinal) || name == "Main_Menu_Button_Play.png"
            ? MenuButtons : MenuArtwork) + "/" + name;

    public static string LobbyMaterialPath(string name)
    {
        string group = name.StartsWith("Room", StringComparison.Ordinal) || name.StartsWith("Player", StringComparison.Ordinal)
            || name == "StartZone" || name.StartsWith("Ready", StringComparison.Ordinal) ? "RoomColors"
            : name == "FurnitureWood" || name == "Linen" || name == "Leaves" || name == "Clay" || name == "Ink" || name == "ContactShadow"
                ? "Furnishings" : "Architecture";
        return LobbyMaterials + "/" + group + "/" + name + ".mat";
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        if (separator < 0) throw new ArgumentException("An asset folder must be below Assets.", nameof(path));
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
#endif
