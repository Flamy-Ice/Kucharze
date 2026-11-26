#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Confirms a pad only after the host has stayed on it for a short time.
    internal sealed class LobbyHostLevelSelector
    {
        private int candidate = -1;
        private float heldSeconds;

        internal bool TrySelect(NetworkManager manager, LobbyLevelSelection selection, LobbyLevelZones zones, float deltaTime)
        {
            if (manager == null || !manager.IsServer || selection == null || zones == null
                || !manager.ConnectedClients.TryGetValue(NetworkManager.ServerClientId, out NetworkClient host)
                || host.PlayerObject == null || !host.PlayerObject.IsSpawned
                || !zones.TryGetLevel(host.PlayerObject.transform.position, out int index)
                || !selection.IsLevelAvailable(index) || selection.SelectedIndex == index)
            {
                candidate = -1;
                heldSeconds = 0;
                return false;
            }
            if (candidate != index)
            {
                candidate = index;
                heldSeconds = 0;
                return false;
            }
            heldSeconds += Mathf.Max(0, deltaTime);
            if (heldSeconds < selection.SelectionHoldSeconds) return false;
            return selection.TrySelectLevel(index, host.ClientId);
        }
    }
}
#endif
