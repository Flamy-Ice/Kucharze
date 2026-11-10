using System;
using UnityEngine;

namespace ModularMovement
{
    // Keeps a visual total of earned coins. Coins cannot be spent or exchanged.
    [DisallowMultipleComponent]
    [AddComponentMenu("Economy/Coin Wallet")]
    public sealed class CoinWallet : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("Initial value of the visual coin counter. Usually zero. Coins cannot be spent.")]
        private int startingCoins = 0;

        private int coins;
        private bool initialized;

        // Reading the total is safe even before Unity has called Awake on this object.
        public int Coins => initialized ? coins : Mathf.Max(0, startingCoins);

        // Tells listening displays the new total whenever coins are added successfully.
        public event Action<int> CoinsChanged;

        // Unity calls this once when the object first loads.
        private void Awake()
        {
            Initialize();
        }

        // A future earning mechanic can increase the displayed total through this method.
        // False means nothing was added: the amount was invalid or the total would exceed the limit.
        public bool TryAddCoins(int amount)
        {
            Initialize();
            // Reject the whole increase if it is not positive or cannot fit in a whole-number total.
            if (amount <= 0 || amount > int.MaxValue - coins)
                return false;

            coins += amount;
            CoinsChanged?.Invoke(coins);
            return true;
        }

        private void Initialize()
        {
            // An early increase must not be erased when Awake runs later.
            if (initialized)
                return;

            // Even an invalid saved starting value must not make the counter negative.
            coins = Mathf.Max(0, startingCoins);
            initialized = true;
        }
    }
}
