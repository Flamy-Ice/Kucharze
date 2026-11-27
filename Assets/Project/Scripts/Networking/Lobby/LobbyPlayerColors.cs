using UnityEngine;

namespace ModularMovement
{
    // Shares the four room colors with player coats and floor rings.
    public static class LobbyPlayerColors
    {
        public const int Count = 4;
        private static readonly Color[] Colors =
        {
            new Color32(35, 140, 240, 255), new Color32(240, 75, 89, 255),
            new Color32(145, 98, 230, 255), new Color32(242, 191, 48, 255)
        };

        public static Color Get(int index) => Colors[Mathf.Clamp(index, 0, Count - 1)];
    }
}
