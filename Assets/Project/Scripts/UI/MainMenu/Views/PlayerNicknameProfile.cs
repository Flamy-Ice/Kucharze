using System.Globalization;
using System.Text;
using UnityEngine;

namespace ModularMovement
{
    // Stores this device's nickname and shares the same text rules with the server.
    public static class PlayerNicknameProfile
    {
        public const int MaximumLength = 16;
        private const string PreferenceKey = "Kucharze.Player.Nickname";

        public static string SavedName => Normalize(PlayerPrefs.GetString(PreferenceKey, ""));

        public static bool IsAllowed(char character)
            => char.IsLetterOrDigit(character) || character == ' ' || character == '_' || character == '-'
                || character == '.' || CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark;

        public static string Normalize(string value)
        {
            var result = new StringBuilder(MaximumLength);
            foreach (char character in value ?? "")
            {
                if (!IsAllowed(character)) continue;
                if (character == ' ' && (result.Length == 0 || result[result.Length - 1] == ' ')) continue;
                if (result.Length >= MaximumLength) break;
                result.Append(character);
            }
            return result.ToString().Trim();
        }

        public static bool IsValid(string value) => Normalize(value).Length >= 2;

        public static void Save(string value) => PlayerPrefs.SetString(PreferenceKey, Normalize(value));
    }
}
