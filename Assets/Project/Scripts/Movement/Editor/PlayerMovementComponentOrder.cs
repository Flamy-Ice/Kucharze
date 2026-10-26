using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ModularMovement.Editor
{
    // Unity starts this helper when editor scripts load, without adding it to a player.
    [InitializeOnLoad]
    public static class PlayerMovementComponentOrder
    {
        // Avoid duplicate requests and new requests caused by our own rearrangement.
        private static bool isQueued;
        private static bool isArranging;

        static PlayerMovementComponentOrder()
        {
            // Watch for scene and component changes that may affect the Inspector order.
            EditorApplication.hierarchyChanged += QueueArrangement;
            ObjectChangeEvents.changesPublished += OnObjectsChanged;
            QueueArrangement();
        }

        private static void OnObjectsChanged(ref ObjectChangeEventStream changes)
        {
            // Changing a setting does not need sorting; adding or removing components does.
            for (int index = 0; index < changes.length; index++)
            {
                if (changes.GetEventType(index) == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    QueueArrangement();
                    return;
                }
            }
        }

        private static void QueueArrangement()
        {
            if (isQueued || isArranging || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // Wait until Unity finishes adding all RequireComponent dependencies.
            isQueued = true;
            EditorApplication.delayCall += ArrangeLoadedPlayers;
        }

        private static void ArrangeLoadedPlayers()
        {
            isQueued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            // Includes inactive players; Arrange filters out assets and unsafe scene objects.
            foreach (PlayerContext context in Resources.FindObjectsOfTypeAll<PlayerContext>())
            {
                if (context != null)
                    Arrange(context.gameObject);
            }
            // Debug can exist without a movement foundation and must not add one just to sort.
            foreach (PlayerMovementDebug debug in Resources.FindObjectsOfTypeAll<PlayerMovementDebug>())
            {
                if (debug != null && debug.GetComponent<PlayerContext>() == null)
                    Arrange(debug.gameObject);
            }
            foreach (PlayerRotation rotation in Resources.FindObjectsOfTypeAll<PlayerRotation>())
            {
                if (rotation != null && rotation.GetComponent<PlayerContext>() == null)
                    Arrange(rotation.gameObject);
            }
            foreach (PlayerAnimation animation in Resources.FindObjectsOfTypeAll<PlayerAnimation>())
            {
                if (animation != null && animation.GetComponent<PlayerContext>() == null)
                    Arrange(animation.gameObject);
            }
        }

        public static void Arrange(GameObject player)
        {
            // Only change editable, loaded scene objects while outside Play Mode.
            if (player == null || isArranging || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorUtility.IsPersistent(player) || !player.scene.IsValid() || !player.scene.isLoaded ||
                PrefabUtility.IsPartOfImmutablePrefab(player) || IsProtectedPath(player.scene.path))
                return;

            // Remember the movement slots so renderers and unrelated colliders stay in place.
            Component[] original = player.GetComponents<Component>();
            var ordered = new List<Component>();
            var movementSlots = new List<int>();
            for (int index = 0; index < original.Length; index++)
            {
                Component component = original[index];
                if (component != null && GetRank(component) >= 0)
                {
                    ordered.Add(component);
                    movementSlots.Add(index);
                }
            }

            // Equal ranks keep their original order, including future movement abilities.
            ordered.Sort((first, second) =>
            {
                int rank = GetRank(first).CompareTo(GetRank(second));
                return rank != 0 ? rank : Array.IndexOf(original, first).CompareTo(Array.IndexOf(original, second));
            });

            isArranging = true;
            try
            {
                // Swap movement components within their existing slots, keeping other components in place.
                for (int targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
                {
                    Component component = ordered[targetIndex];
                    Component[] current = player.GetComponents<Component>();
                    int currentIndex = Array.IndexOf(current, component);
                    int targetSlot = movementSlots[targetIndex];
                    if (currentIndex == targetSlot)
                        continue;

                    Component displaced = current[targetSlot];
                    if (!TryMoveToIndex(component, targetSlot) || !TryMoveToIndex(displaced, currentIndex))
                    {
                        // Try to restore the original pair if Unity rejects a prefab component move.
                        TryMoveToIndex(displaced, targetSlot);
                        TryMoveToIndex(component, currentIndex);
                        return;
                    }
                }
            }
            finally
            {
                // Allow later requests again, even if a component move could not finish.
                isArranging = false;
            }
        }

        // Moves an Inspector entry one slot at a time. It does not move the object in the scene.
        private static bool TryMoveToIndex(Component component, int targetIndex)
        {
            int currentIndex = Array.IndexOf(component.gameObject.GetComponents<Component>(), component);
            while (currentIndex != targetIndex)
            {
                bool moved = currentIndex > targetIndex
                    ? ComponentUtility.MoveComponentUp(component)
                    : ComponentUtility.MoveComponentDown(component);
                if (!moved)
                    return false;
                currentIndex += currentIndex > targetIndex ? -1 : 1;
            }
            return true;
        }

        // Smaller numbers appear higher in the Inspector; they do not set execution order.
        private static int GetRank(Component component)
        {
            if (component is CharacterController) return 0;
            if (component is PlayerContext) return 1;
            if (component is PlayerBus) return 2;
            if (component is PlayerMovementResolver) return 3;
            if (component is PlayerStates) return 4;
            if (component is PlayerAnimation) return 5;
            if (component is PlayerInputActions) return 6;
            if (component is PlayerGravity) return 7;
            if (component is PlayerMotor) return 8;
            if (component is PlayerRotation) return 9;
            if (component is PlayerHorizontalMovement) return 10;
            if (component is PlayerWalk) return 11;
            if (component is PlayerRun) return 12;
            if (component is PlayerDash) return 13;
            // Future abilities stay together, with the diagnostic view below the movement group.
            if (component is IMovementProvider) return 14;
            if (component is PlayerMovementDebug) return 15;
            // A negative rank leaves components outside the movement group untouched.
            return -1;
        }

        // Recognizes the private folder name regardless of letter case or slash style.
        private static bool IsProtectedPath(string path)
        {
            string normalized = "/" + path.Replace('\\', '/') + "/";
            return normalized.IndexOf("/todo/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
