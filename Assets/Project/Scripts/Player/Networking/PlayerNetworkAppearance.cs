#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Colors the coat, sleeves, hat and floor ring from the player's replicated palette.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerNetworkAppearance : NetworkBehaviour
    {
        [SerializeField] private Renderer[] tintRenderers;
        private readonly NetworkVariable<byte> palette = new NetworkVariable<byte>();
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public byte PaletteIndex => palette.Value;

        public override void OnNetworkSpawn()
        {
            palette.OnValueChanged += OnPaletteChanged;
            Apply();
        }

        public void SetPalette(int index)
        {
            if (IsServer && IsSpawned && index >= 0 && index < LobbyPlayerColors.Count) palette.Value = (byte)index;
        }

        private void OnPaletteChanged(byte previous, byte current) => Apply();

        private void Apply()
        {
            if (tintRenderers == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            foreach (Renderer renderer in tintRenderers)
            {
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] == null || !materials[index].HasProperty(BaseColor)) continue;
                    renderer.GetPropertyBlock(properties, index);
                    bool teamSlot = index == 0 || (index == 1 && renderer is SkinnedMeshRenderer);
                    Color color = teamSlot ? LobbyPlayerColors.Get(palette.Value) : materials[index].GetColor(BaseColor);
                    if (index == 0 && renderer is MeshRenderer) color = Color.Lerp(color, Color.white, 0.35f);
                    properties.SetColor(BaseColor, color);
                    if (teamSlot && renderer is SkinnedMeshRenderer && materials[index].HasProperty("_EmissionColor"))
                        properties.SetColor("_EmissionColor", color * 0.20f);
                    renderer.SetPropertyBlock(properties, index);
                }
            }
        }

        public override void OnNetworkDespawn() => palette.OnValueChanged -= OnPaletteChanged;
    }
}
#endif
