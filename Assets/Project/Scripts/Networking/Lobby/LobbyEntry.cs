#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Starts the connection menu once; returning to this scene keeps the existing session.
    public sealed class LobbyEntry : MonoBehaviour
    {
        [SerializeField] private GameObject connectionPrefab;

        private void Start()
        {
            if (NetworkManager.Singleton == null && connectionPrefab != null)
                Instantiate(connectionPrefab);
        }
    }
}
#endif
