using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ModularMovement
{
    // Copies the completed scene into a menu-owned texture before overlay UI is drawn.
    public sealed class HudMenuCaptureFeature : ScriptableRendererFeature
    {
        private static readonly Dictionary<Camera, RTHandle> Targets = new Dictionary<Camera, RTHandle>();
        private static readonly int SceneCopy = Shader.PropertyToID("_HudMenuSceneCopy");
        [SerializeField] private Material blurMaterial;
        private CapturePass pass;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTargets() => Targets.Clear();

        public static void Register(Camera camera, RTHandle target) => Targets[camera] = target;
        public static void Unregister(Camera camera, RTHandle target)
        {
            if (!object.ReferenceEquals(camera, null) && Targets.TryGetValue(camera, out var current) && current == target) Targets.Remove(camera);
        }

        public override void Create() => pass = new CapturePass(blurMaterial);
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (Targets.ContainsKey(renderingData.cameraData.camera)) renderer.EnqueuePass(pass);
        }

        private sealed class CapturePass : ScriptableRenderPass
        {
            private readonly Material material;
            public CapturePass(Material material)
            {
                this.material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                if (!Targets.TryGetValue(cameraData.camera, out var target) || target == null || target.rt == null) return;
                var resources = frameData.Get<UniversalResourceData>();
                var destination = graph.ImportTexture(target);
                var description = new TextureDesc(target.rt.width, target.rt.height)
                {
                    colorFormat = target.rt.graphicsFormat, filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp, name = "Menu Blur Scene"
                };
                var sceneCopy = graph.CreateTexture(description);
                graph.AddBlitPass(resources.activeColorTexture, sceneCopy, Vector2.one, Vector2.zero,
                    passName: "Pause Menu Scene Copy");
                if (material == null) return;
                description.name = "Menu Blur Horizontal";
                var horizontal = graph.CreateTexture(description);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(sceneCopy, horizontal, material, 0),
                    passName: "Pause Menu Gaussian Horizontal");
                using (var builder = graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(horizontal, destination, material, 1),
                    returnBuilder: true, passName: "Pause Menu Gaussian Vertical"))
                {
                    builder.AllowPassCulling(false);
                    // Overlay UI declares reads of exported textures, keeping the copy before the UI draw.
                    builder.SetGlobalTextureAfterPass(destination, SceneCopy);
                }
            }
        }
    }
}
