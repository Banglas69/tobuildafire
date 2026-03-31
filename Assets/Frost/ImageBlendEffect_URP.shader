Shader "Custom/ImageBlendEffect_URP"
{
    Properties
    {
        _BlendTex ("Image", 2D) = "" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}

        // URP fullscreen pass provides the source in _BlitTexture
        _BlitTexture ("Blit Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _BlitTexture;
            sampler2D _BlendTex;
            sampler2D _BumpMap;

            float _BlendAmount;
            float _EdgeSharpness;
            float _SeeThroughness;
            float _Distortion;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float4 blendColor = tex2D(_BlendTex, i.uv);

                blendColor.a = blendColor.a + (_BlendAmount * 2 - 1);
                blendColor.a = saturate(blendColor.a * _EdgeSharpness - (_EdgeSharpness - 1) * 0.5);

                half2 bump = UnpackNormal(tex2D(_BumpMap, i.uv)).rg;

                float4 mainColor = tex2D(_BlitTexture, i.uv + bump * blendColor.a * _Distortion);

                float4 overlayColor = blendColor;
                overlayColor.rgb = mainColor.rgb * (blendColor.rgb + 0.5) * (blendColor.rgb + 0.5);

                blendColor = lerp(blendColor, overlayColor, _SeeThroughness);

                return lerp(mainColor, blendColor, blendColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
