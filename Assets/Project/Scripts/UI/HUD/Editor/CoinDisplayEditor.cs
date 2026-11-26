using UnityEditor;
using UnityEngine.UIElements;

namespace ModularMovement.Editor
{
    // Shows the display references separately from the rules for earning coins.
    [CustomEditor(typeof(CoinDisplay)), CanEditMultipleObjects]
    public sealed class CoinDisplayEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = CoinInspectorPanel.Create("Coin Display",
                "Shows the earned coin total in TextMeshPro.", out VisualElement settings);
            CoinInspectorPanel.AddField(settings, serializedObject, "wallet", "Wallet",
                "Wallet to display. If empty, looks on this object and its parents. Assign before enabling the display.");
            CoinInspectorPanel.AddField(settings, serializedObject, "coinText", "Coin Text",
                "Text that shows the coins. If empty, uses TMP text on this object.");
            return root;
        }
    }
}
