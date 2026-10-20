using UnityEditor;
using UnityEngine;

namespace ModularMovement.Editor
{
    // Upgrades only loaded players. It never opens or saves scene or prefab assets.
    [InitializeOnLoad]
    public static class PlayerMovementSettingsUpgrade
    {
        private static bool queued;

        static PlayerMovementSettingsUpgrade()
        {
            EditorApplication.hierarchyChanged += Queue;
            Queue();
        }

        private static void Queue()
        {
            if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            queued = true;
            // Wait until Unity finishes changing the hierarchy before upgrading loaded players.
            EditorApplication.delayCall += UpgradeLoadedPlayers;
        }

        private static void UpgradeLoadedPlayers()
        {
            queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (PlayerContext context in Resources.FindObjectsOfTypeAll<PlayerContext>())
            {
                if (context == null || EditorUtility.IsPersistent(context)
                    || !context.gameObject.scene.IsValid() || !context.gameObject.scene.isLoaded
                    || (context.hideFlags & HideFlags.DontSave) != 0) continue;
                Upgrade(context.gameObject);
            }
        }

        public static void Upgrade(GameObject player)
        {
            if (player == null || player.GetComponent<PlayerMovementResolver>() == null) return;
            if (player.GetComponent<PlayerHorizontalMovement>() == null)
                Undo.AddComponent<PlayerHorizontalMovement>(player);
            // These are the two historical formats. Future abilities need no upgrade entries.
            Migrate(player.GetComponent<PlayerWalk>());
            Migrate(player.GetComponent<PlayerRun>());
        }

        private static void Migrate(MonoBehaviour ability)
        {
            if (ability == null) return;
            var saved = new SerializedObject(ability);
            if (saved.FindProperty("legacyAcceleration").floatValue < 0f
                && saved.FindProperty("legacyDeceleration").floatValue < 0f
                && saved.FindProperty("legacyInputDeadzone").floatValue < 0f) return;
            var horizontal = ability.GetComponent<PlayerHorizontalMovement>();
            var input = ability.GetComponent<PlayerInputActions>();
            if (horizontal == null || input == null) return;
            // One Undo must restore both the old ability settings and their new shared owners.
            Undo.RecordObjects(new Object[] { ability, horizontal, input }, "Share movement settings");
            if (ability is PlayerWalk walk) walk.MigrateLegacySettings();
            if (ability is PlayerRun run) run.MigrateLegacySettings();
            foreach (Object changed in new Object[] { ability, horizontal, input })
            {
                EditorUtility.SetDirty(changed);
                PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
            }
        }
    }
}
