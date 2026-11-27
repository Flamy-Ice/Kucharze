#if UNITY_EDITOR && KUCHARZE_ONLINE
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps lobby objects in editor folders without changing their world transforms.
public static class LobbyHierarchySetup
{
    public static Transform Find(Scene scene, string name)
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name);

    public static Transform Find(Transform parent, string name)
        => parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);

    public static void Organize(Scene scene)
    {
        Group(scene, null, "SceneSystems", "LobbyCamera", "LobbyLight", "LobbyEntry");
        Group(scene, null, "Environment", "LobbyPresentation");
        Group(scene, null, "Gameplay", "LobbyMarkers", "StartZone_MoveAndResizeMe", "LobbyLevelRoom");
        Group(scene, null, "UI", "LobbyHUD");
        Transform scenery = Find(scene, "LobbyPresentation");
        if (scenery != null)
        {
            Group(scene, scenery, "Architecture", "BuildingBase", "LobbyFloor", "SharedHallTiles",
                "NorthWall", "NorthWallCap", "NorthWallSkirting", "SouthWall", "SouthWallCap", "SouthWallSkirting",
                "WestWall", "WestWallCap", "WestWallSkirting", "EastWall", "EastWallCap", "EastWallSkirting");
            Group(scene, scenery, "PlayerRooms", "Room_1_Decor", "Room_2_Decor", "Room_3_Decor", "Room_4_Decor");
        }
        Transform levels = Find(scene, "LobbyLevelRoom");
        if (levels != null)
        {
            Group(scene, levels, "LeftWallLevels", "Level_01", "Level_02", "Level_03");
            Group(scene, levels, "RightWallLevels", "Level_04", "Level_05", "Level_06");
            Group(scene, levels, "FrontWallLevels", "Level_07", "Level_08", "Level_09");
        }
    }

    private static void Group(Scene scene, Transform parent, string name, params string[] members)
    {
        Transform folder = parent == null
            ? scene.GetRootGameObjects().FirstOrDefault(root => root.name == name)?.transform
            : parent.Find(name);
        if (folder == null)
        {
            folder = new GameObject(name).transform;
            SceneManager.MoveGameObjectToScene(folder.gameObject, scene);
            folder.SetParent(parent, false);
        }
        foreach (string member in members)
        {
            Transform item = Find(scene, member);
            if (item != null && item != folder) item.SetParent(folder, true);
        }
    }
}
#endif
