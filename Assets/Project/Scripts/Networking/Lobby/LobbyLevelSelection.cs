#if KUCHARZE_ONLINE
using System;
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Keeps nine level destinations and replicates the host's selected slot.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class LobbyLevelSelection : NetworkBehaviour
    {
        public const int LevelCount = 9;

        [Serializable]
        public sealed class Level
        {
            public string title;
            public string scenePath;
            public Sprite icon;
        }

        [SerializeField] private Level[] levels = new Level[LevelCount];
        [SerializeField, Min(0), Tooltip("Time the host must stay on one pad, in seconds. Zero selects immediately.")]
        private float selectionHoldSeconds;
        private readonly NetworkVariable<int> selected = new NetworkVariable<int>(-1);
        public int SelectedIndex => selected.Value;
        public float SelectionHoldSeconds => Mathf.Max(0, selectionHoldSeconds);
        public bool HasSelection => IsSpawned && IsLevelAvailable(SelectedIndex);
        public string SelectedScenePath => HasSelection ? levels[SelectedIndex].scenePath : "";

        public override void OnNetworkSpawn()
        {
            if (IsServer) selected.Value = -1;
        }

        public string GetTitle(int index)
            => IsIndexValid(index) && levels[index] != null && !string.IsNullOrWhiteSpace(levels[index].title)
                ? levels[index].title : "Level " + (index + 1);

        public string GetScenePath(int index)
            => IsIndexValid(index) && levels[index] != null ? levels[index].scenePath : "";

        public Sprite GetIcon(int index)
            => IsIndexValid(index) && levels[index] != null ? levels[index].icon : null;

        public bool IsLevelAvailable(int index)
        {
            string path = GetScenePath(index);
            return !string.IsNullOrWhiteSpace(path) && Application.CanStreamedLevelBeLoaded(path);
        }

        // Clients cannot write this value; the server accepts only the host's player.
        public bool TrySelectLevel(int index, ulong playerId)
        {
            if (!IsSpawned || !IsServer || !NetworkManager.IsHost || playerId != Unity.Netcode.NetworkManager.ServerClientId
                || !IsLevelAvailable(index) || selected.Value == index) return false;
            selected.Value = index;
            return true;
        }

        private bool IsIndexValid(int index) => levels != null && index >= 0 && index < LevelCount && index < levels.Length;

        private void OnValidate()
        {
            if (levels == null) levels = new Level[LevelCount];
            if (levels.Length != LevelCount) Array.Resize(ref levels, LevelCount);
            for (int index = 0; index < LevelCount; index++)
                if (levels[index] == null) levels[index] = new Level { title = "Level " + (index + 1) };
        }
    }
}
#endif
