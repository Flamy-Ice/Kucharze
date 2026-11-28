// Fullscreen filtering belongs to the renderer, never to the Canvas material.
Shader "Hidden/Kucharze/Menu Gaussian Blur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "Gaussian Horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurHorizontal
            #include "MenuGaussianBlur.hlsl"
            half4 BlurHorizontal(Varyings input) : SV_Target
            {
                return GaussianBlur(input.texcoord, float2(abs(_BlitTexture_TexelSize.x), 0));
            }
            ENDHLSL
        }
        Pass
        {
            Name "Gaussian Vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurVertical
            #include "MenuGaussianBlur.hlsl"
            half4 BlurVertical(Varyings input) : SV_Target
            {
                return GaussianBlur(input.texcoord, float2(0, abs(_BlitTexture_TexelSize.y)));
            }
            ENDHLSL
        }
    }
}
