// Blurs adjacent pixels in two separate directions to avoid spaced, blocky samples.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

half4 GaussianBlur(float2 uv, float2 pixelStep)
{
    half3 color = 0;
    float totalWeight = 0;
    // Sigma 5 at half resolution gives a smooth blur about ten screen pixels wide.
    [unroll] for (int offset = -15; offset <= 15; offset++)
    {
        float weight = exp(-0.5 * offset * offset / 25.0);
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
            saturate(uv + pixelStep * offset)).rgb * weight;
        totalWeight += weight;
    }
    return half4(color / totalWeight, 1);
}
