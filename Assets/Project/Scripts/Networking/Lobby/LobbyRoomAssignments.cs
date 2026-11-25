#if KUCHARZE_ONLINE
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace ModularMovement
{
    // Keeps each connected player in one room slot and places them once on scene entry.
    internal sealed class LobbyRoomAssignments
    {
        private readonly Dictionary<ulong, int> slots = new Dictionary<ulong, int>();
        private readonly HashSet<ulong> placed = new HashSet<ulong>();
        private readonly List<ulong> departed = new List<ulong>();

        public void BeginScene() => placed.Clear();

        public void Refresh(NetworkManager manager)
        {
            departed.Clear();
            foreach (ulong clientId in slots.Keys)
                if (!manager.ConnectedClients.ContainsKey(clientId)) departed.Add(clientId);
            foreach (ulong clientId in departed)
            {
                slots.Remove(clientId);
                placed.Remove(clientId);
            }
            foreach (NetworkClient client in manager.ConnectedClientsList)
            {
                if (slots.ContainsKey(client.ClientId)) continue;
                int slot = 0;
                while (slots.ContainsValue(slot)) slot++;
                if (slot < 4) slots.Add(client.ClientId, slot);
            }
        }

        public void PlacePlayers(NetworkManager manager, LobbyLayout layout)
        {
            if (layout == null) return;
            foreach (NetworkClient client in manager.ConnectedClientsList)
            {
                NetworkObject player = client.PlayerObject;
                if (player == null || !player.IsSpawned || placed.Contains(client.ClientId)
                    || !slots.TryGetValue(client.ClientId, out int slot)
                    || !layout.TryGetSpawn(slot, out Vector3 position)) continue;
                player.DestroyWithScene = false;
                player.GetComponent<PlayerNetworkAppearance>()?.SetPalette(slot);
                CharacterController controller = player.GetComponent<CharacterController>();
                bool enabled = controller != null && controller.enabled;
                if (controller != null) controller.enabled = false;
                // This is scene-entry placement; normal movement still belongs only to PlayerMotor.
                player.GetComponent<NetworkTransform>().Teleport(position, player.transform.rotation, player.transform.localScale);
                if (controller != null) controller.enabled = enabled;
                placed.Add(client.ClientId);
            }
        }
    }
}
#endif
