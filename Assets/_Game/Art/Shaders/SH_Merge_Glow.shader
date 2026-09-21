Shader "Blobs/MergeGlow"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (1, 0.5, 0.8, 1)
        _CoreIntensity ("Core Intensity", Range(0, 8)) = 2
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 2.5
        _GlowSize ("Glow Size", Range(0, 0.3)) = 0.09
        _CoreSoftness ("Core Softness", Range(0, 1)) = 0.35
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

        // Additive: bright pixels accumulate, which is what bloom keys on.
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MergeGlow"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CoreColor;
                float4 _GlowColor;
                float _CoreIntensity;
                float _GlowIntensity;
                float _GlowSize;
                float _CoreSoftness;
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

            // Two rings of taps turn a hard-edged sprite into a soft halo without
            // needing a pre-blurred texture.
            static const float2 kTaps[12] =
            {
                float2( 1.000,  0.000), float2( 0.500,  0.866), float2(-0.500,  0.866),
                float2(-1.000,  0.000), float2(-0.500, -0.866), float2( 0.500, -0.866),
                float2( 0.866,  0.500), float2( 0.000,  1.000), float2(-0.866,  0.500),
                float2(-0.866, -0.500), float2( 0.000, -1.000), float2( 0.866, -0.500)
            };

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv)).a;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float centerAlpha = SampleAlpha(input.uv);

                float halo = centerAlpha;
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    halo += SampleAlpha(input.uv + kTaps[i] * _GlowSize);
                    halo += SampleAlpha(input.uv + kTaps[i] * _GlowSize * 0.5);
                }
                halo /= 25.0;

                float core = smoothstep(_CoreSoftness, 1.0, centerAlpha) * centerAlpha;
                float rim = saturate(halo - core * 0.65);

                float3 rgb = _CoreColor.rgb * _CoreIntensity * core
                    + _GlowColor.rgb * _GlowIntensity * rim;

                // SpriteRenderer.color arrives as vertex color; only its alpha drives the
                // fade so the authored HDR core stays white.
                return float4(rgb * input.color.a, 0.0);
            }
            ENDHLSL
        }
    }
}
