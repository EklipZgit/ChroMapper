Shader "ChroMapper/BloomfogSkybox"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Background" "Queue"="Background"
        }
        LOD 100
        Cull Off
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            uniform sampler2D _BloomPrePassTexture;
            uniform float2 _CustomFogTextureToScreenRatio;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Convert screen position to UV coordinates
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Apply custom ratio to screen space UVs
                float2 modifiedUV = (screenUV - 0.5) * _CustomFogTextureToScreenRatio + 0.5;

                // Sample the bloom prepass texture
                half4 col = tex2D(_BloomPrePassTexture, modifiedUV);
                col.a = 0;
                return col;
            }
            ENDHLSL
        }
    }
}