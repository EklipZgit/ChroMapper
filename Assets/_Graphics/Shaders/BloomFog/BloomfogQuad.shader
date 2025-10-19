Shader "ChroMapper/BloomfogQuad"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off Cull Off
        Blend One OneMinusSrcAlpha

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
            };

            uniform sampler2D _BloomfogTex;
            float4 _BloomfogTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 textureCoordinate = i.screenPosition.xy / i.screenPosition.w;
                return tex2D(_BloomfogTex, textureCoordinate);
            }
            ENDCG
        }
    }
}
