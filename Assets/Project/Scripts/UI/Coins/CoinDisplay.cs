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

        // Unity calls this whenever the display is enabled, including after it was hidden.
        private void OnEnable()
        {
            if (coinText == null)
                coinText = GetComponent<TMP_Text>();
            if (wallet == null)
                wallet = GetComponentInParent<CoinWallet>();

            // Remember which wallet we listen to, so we can stop listening to that same wallet later.
            subscribedWallet = wallet;
            if (subscribedWallet != null)
                subscribedWallet.CoinsChanged += UpdateDisplay;

            // Refresh immediately, including coins earned while the display was hidden.
            UpdateDisplay(subscribedWallet != null ? subscribedWallet.Coins : 0);
        }

        // Stop receiving changes while this display is disabled or being removed.
        private void OnDisable()
        {
            if (subscribedWallet != null)
                subscribedWallet.CoinsChanged -= UpdateDisplay;
            subscribedWallet = null;
        }

        private void UpdateDisplay(int coins)
        {
            // Use the same number format on every computer and keep the currency sign at the end.
            if (coinText != null)
                coinText.text = coins.ToString(CultureInfo.InvariantCulture) + "$";
        }
    }
}
