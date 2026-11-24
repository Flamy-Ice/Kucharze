#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Replicates the wallet total while allowing only the server to award coins.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(CoinWallet))]
    public sealed class CoinWalletNetwork : NetworkBehaviour
    {
        private readonly NetworkVariable<int> coins = new NetworkVariable<int>();
        private CoinWallet wallet;

        private void Awake()
        {
            wallet = GetComponent<CoinWallet>();
            wallet.CanAwardCoins = false;
        }

        public override void OnNetworkSpawn()
        {
            wallet.CanAwardCoins = IsServer;
            coins.OnValueChanged += OnCoinsChanged;
            if (IsServer)
            {
                coins.Value = wallet.Coins;
                wallet.CoinsChanged += OnAwarded;
            }
            else wallet.ApplyReplicatedCoins(coins.Value);
        }

        private void OnAwarded(int total) => coins.Value = total;
        private void OnCoinsChanged(int previous, int current)
        {
            if (!IsServer) wallet.ApplyReplicatedCoins(current);
        }

        public override void OnNetworkDespawn()
        {
            coins.OnValueChanged -= OnCoinsChanged;
            wallet.CoinsChanged -= OnAwarded;
            wallet.CanAwardCoins = false;
        }
    }
}
#endif
