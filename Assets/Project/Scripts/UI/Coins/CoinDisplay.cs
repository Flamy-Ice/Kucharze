using System.Globalization;
using TMPro;
using UnityEngine;

namespace ModularMovement
{
    // Shows the earned coin total in a TMP label without deciding how coins are earned.
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Coin Display")]
    public sealed class CoinDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Wallet to display. If empty, looks on this object and its parents.")]
        private CoinWallet wallet;

        [SerializeField, Tooltip("Text that shows the coins. If empty, uses TMP text on this object.")]
        private TMP_Text coinText;

        private CoinWallet subscribedWallet;

        // Unity fills convenient local references when this component is added or reset.
        private void Reset()
        {
            wallet = GetComponentInParent<CoinWallet>();
            coinText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (coinText == null)
                coinText = GetComponent<TMP_Text>();
            if (wallet == null)
                wallet = GetComponentInParent<CoinWallet>();

            // Remember the source we actually subscribed to, so disabling always disconnects it.
            subscribedWallet = wallet;
            if (subscribedWallet != null)
                subscribedWallet.CoinsChanged += UpdateDisplay;

            // Refresh immediately, including coins earned while the display was hidden.
            UpdateDisplay(subscribedWallet != null ? subscribedWallet.Coins : 0);
        }

        private void OnDisable()
        {
            if (subscribedWallet != null)
                subscribedWallet.CoinsChanged -= UpdateDisplay;
            subscribedWallet = null;
        }

        private void UpdateDisplay(int coins)
        {
            if (coinText != null)
                coinText.text = coins.ToString(CultureInfo.InvariantCulture) + "$";
        }
    }
}
