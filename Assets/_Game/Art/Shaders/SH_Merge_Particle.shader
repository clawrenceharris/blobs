Shader "Blobs/MergeParticle"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _ShadowBlend ("Shadow Blend", Range(0, 1)) = 0.44
        _HighlightBlend ("Highlight Blend", Range(0, 1)) = 0.61
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MergeParticle"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _HighlightColor;
                float _ShadowBlend;
                float _HighlightBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float lum = tex.r;
                float shadowStop = max(_ShadowBlend, 0.001);
                float highlightStop = min(max(_HighlightBlend, shadowStop + 0.001), 0.999);

                float3 mapped = _ShadowColor.rgb;
                mapped = lerp(mapped, _BaseColor.rgb, saturate(lum / shadowStop));
                mapped = lerp(mapped, _HighlightColor.rgb, saturate((lum - highlightStop) / max(1.0 - highlightStop, 0.001)));

                float alpha = tex.a * input.color.a * _BaseColor.a;
                return float4(mapped, alpha);
            }
            ENDHLSL
        }
    }
}
