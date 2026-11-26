using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Draws smooth menu panels without stretching a tiny background texture.
    [AddComponentMenu("UI/Menu Rounded Panel")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuRoundedPanel : MaskableGraphic
    {
        [SerializeField, Min(0)] private float cornerRadius = 14;
        [SerializeField, Min(0)] private float borderWidth = 2;
        [SerializeField] private Color borderColor = new Color32(75, 71, 86, 255);
        private const int StepsPerCorner = 12;
        private const int RingSize = 4 * (StepsPerCorner + 1);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = GetPixelAdjustedRect();
            float radius = Mathf.Clamp(cornerRadius, 0, Mathf.Min(bounds.width, bounds.height) * 0.5f);
            float border = Mathf.Clamp(borderWidth, 0, Mathf.Min(bounds.width, bounds.height) * 0.5f);
            Color transparent = borderColor;
            transparent.a = 0;
            // The transparent outer edge softens the silhouette at different canvas scales.
            AddRing(mesh, bounds, radius, 1, transparent);
            AddRing(mesh, bounds, radius, 0, borderColor);
            AddRing(mesh, bounds, radius, -border, color);
            for (int ring = 0; ring < 2; ring++)
                for (int index = 0; index < RingSize; index++)
                {
                    int next = (index + 1) % RingSize;
                    int start = ring * RingSize;
                    mesh.AddTriangle(start + index, start + next, start + RingSize + index);
                    mesh.AddTriangle(start + next, start + RingSize + next, start + RingSize + index);
                }
            mesh.AddVert(bounds.center, color, Vector2.zero);
            for (int index = 0; index < RingSize; index++)
                mesh.AddTriangle(3 * RingSize, 2 * RingSize + index, 2 * RingSize + (index + 1) % RingSize);
        }

        private static void AddRing(VertexHelper mesh, Rect bounds, float radius, float expansion, Color tint)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? bounds.xMax - radius : bounds.xMin + radius,
                    corner < 2 ? bounds.yMax - radius : bounds.yMin + radius);
                for (int step = 0; step <= StepsPerCorner; step++)
                {
                    float angle = (corner * 90f + step * 90f / StepsPerCorner) * Mathf.Deg2Rad;
                    Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Max(0, radius + expansion);
                    mesh.AddVert(point, tint, Vector2.zero);
                }
            }
        }
    }
}
