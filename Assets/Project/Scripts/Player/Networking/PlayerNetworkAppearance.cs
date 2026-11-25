#if KUCHARZE_ONLINE
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Gives each player a readable coat and floor ring without changing movement or animation.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerNetworkAppearance : NetworkBehaviour
    {
        [SerializeField] private Renderer[] tintRenderers;
        private readonly NetworkVariable<byte> palette = new NetworkVariable<byte>();
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color[] Colors =
        {
            new Color(0.23f, 0.57f, 0.77f), new Color(0.83f, 0.36f, 0.23f),
            new Color(0.57f, 0.40f, 0.73f), new Color(0.85f, 0.63f, 0.18f)
        };

        public byte PaletteIndex => palette.Value;

        public override void OnNetworkSpawn()
        {
            palette.OnValueChanged += OnPaletteChanged;
            Apply();
        }

        public void SetPalette(int index)
        {
            if (IsServer && IsSpawned && index >= 0 && index < Colors.Length) palette.Value = (byte)index;
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
                    Color color = index == 0 ? Colors[palette.Value % Colors.Length] : materials[index].GetColor(BaseColor);
                    if (index == 0 && renderer is MeshRenderer) color = Color.Lerp(color, Color.white, 0.35f);
                    properties.SetColor(BaseColor, color);
                    if (index == 0 && renderer is SkinnedMeshRenderer && materials[index].HasProperty("_EmissionColor"))
                        properties.SetColor("_EmissionColor", color * 0.20f);
                    renderer.SetPropertyBlock(properties, index);
                }
            }
        }

        public override void OnNetworkDespawn() => palette.OnValueChanged -= OnPaletteChanged;
    }
}
#endif
