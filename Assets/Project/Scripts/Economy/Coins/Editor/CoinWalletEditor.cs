using UnityEditor;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Shows the visual counter settings without changing its runtime total.
    [CustomEditor(typeof(CoinWallet)), CanEditMultipleObjects]
    public sealed class CoinWalletEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = CoinInspectorPanel.Create("Coin Wallet",
                "Tracks earned coins for display only. No spending.", out VisualElement settings);
            CoinInspectorPanel.AddField(settings, serializedObject, "startingCoins", "Starting Coins",
                "Initial value of the visual coin counter. Usually zero. Changes apply to the next initialization.");
            return root;
        }
    }
}
