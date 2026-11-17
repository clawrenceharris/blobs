Shader "AllIn1SpringsToolkit/Demo/ScrollTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Global Color", Color) = (1,1,1,1)
        _ScrollXSpeed("Scroll speed X", Float) = 0.1
        _ScrollYSpeed("Scroll speed Y", Float) = 0.1
    }

	SubShader
	{
		Tags
		{
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
		}

		Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

		Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color, _MainTex_ST;
            float _ScrollXSpeed, _ScrollYSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 textureUvs = i.uv;
                const float screenAspect = _ScreenParams.x / _ScreenParams.y;
                textureUvs.x *= screenAspect;

                textureUvs.x += (_Time.x * _ScrollXSpeed) % 1;
                textureUvs.y += (_Time.x * _ScrollYSpeed) % 1;
                
                float4 col = tex2D(_MainTex, textureUvs);
                col *= i.color;
                col *= _Color;
                return col;
            }
            ENDHLSL
        }
	}

    SubShader
    {
        Tags {"RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color, _MainTex_ST;
            float _ScrollXSpeed, _ScrollYSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 textureUvs = i.uv;
                const float screenAspect = _ScreenParams.x / _ScreenParams.y;
                textureUvs.x *= screenAspect;

                textureUvs.x += (_Time.x * _ScrollXSpeed) % 1;
                textureUvs.y += (_Time.x * _ScrollYSpeed) % 1;
                
                float4 col = tex2D(_MainTex, textureUvs);
                col *= i.color;
                col *= _Color;
                return col;
            }
            ENDCG
        }
    }
}