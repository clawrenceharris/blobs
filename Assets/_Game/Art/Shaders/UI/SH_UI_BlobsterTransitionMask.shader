Shader "Blobs/UI/Blobster Transition Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _MaskCenter ("Mask Center", Vector) = (0.5,0.42,0,0)
        _MouthRadii ("Mouth Radii", Vector) = (0.3,0.01,0,0)
        _Softness ("Edge Softness", Float) = 0.008
        _LipColor ("Lip Color", Color) = (0,0,0,1)
        _LipWidthPixels ("Lip Width (Pixels)", Float) = 20
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float2 _MaskCenter;
            float2 _MouthRadii;
            float _Softness;
            fixed4 _LipColor;
            float _LipWidthPixels;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.screenPosition = ComputeScreenPos(output.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;

                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float2 delta = screenUv - _MaskCenter;

                // Express both axes relative to screen height.
                delta.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);

                float2 safeRadii = max(_MouthRadii, float2(0.0001, 0.0001));
                float2 normalizedDelta = delta / safeRadii;
                float ellipseDistance = length(normalizedDelta);

                float coverage = smoothstep(
                    1.0 - _Softness,
                    1.0 + _Softness,
                    ellipseDistance);

                // Convert the implicit ellipse distance to approximate screen pixels. Unlike a
                // scaled ring sprite, this keeps the black lip the same visual weight at every
                // mouth radius and through the slit-to-circle reshape.
                float gradientLength = length(delta / (safeRadii * safeRadii))
                    / max(ellipseDistance, 0.0001);
                float boundaryDistancePixels = abs(ellipseDistance - 1.0)
                    / max(gradientLength, 0.0001)
                    * _ScreenParams.y;
                float halfLipWidth = max(0.0, _LipWidthPixels * 0.5);
                float lipCoverage = 1.0 - smoothstep(
                    max(0.0, halfLipWidth - 1.0),
                    halfLipWidth + 1.0,
                    boundaryDistancePixels);
                lipCoverage *= step(0.001, _LipWidthPixels);

                color.rgb = lerp(color.rgb, _LipColor.rgb, lipCoverage * _LipColor.a);
                color.a = max(color.a * coverage, lipCoverage * _LipColor.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
