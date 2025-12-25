Shader "Hidden/BloomfogBlurring"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            // No culling or depth
            Cull Off ZWrite Off ZTest Always
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _BloomfogAlpha;
            float _BloomfogBrightness;
            float _BloomfogBlurRadius;
            sampler2D _BloomfogPrevTex;
            float4 _BloomfogPrevTex_TexelSize;
            float4 _BloomfogPrevTex_ST;

            float4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _BloomfogPrevTex_TexelSize.xy;
                float radius = _BloomfogBlurRadius + 0.5;
                
                // Kawase blur - 4 diagonal samples
                float4 mipColor = float4(0,0,0,0);
                
                mipColor.rgb += tex2D(_BloomfogPrevTex, i.uv + float2(radius, radius) * texelSize).rgb;
                mipColor.rgb += tex2D(_BloomfogPrevTex, i.uv + float2(-radius, radius) * texelSize).rgb;
                mipColor.rgb += tex2D(_BloomfogPrevTex, i.uv + float2(radius, -radius) * texelSize).rgb;
                mipColor.rgb += tex2D(_BloomfogPrevTex, i.uv + float2(-radius, -radius) * texelSize).rgb;
                
                mipColor.rgb /= 4.0; // Average the 4 samples
                //mipColor.rgb += _BloomfogBrightness;

                mipColor.a = _BloomfogAlpha + _BloomfogBrightness;
                
                return mipColor;
            }
            ENDCG
        }
    }
}
