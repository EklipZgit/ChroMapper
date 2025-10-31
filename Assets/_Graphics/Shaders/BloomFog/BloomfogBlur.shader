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
            float _Bloomfog_Brightness;
            sampler2D _BloomfogPrevTex;
            float4 _BloomfogPrevTex_TexelSize;
            float4 _BloomfogPrevTex_ST;

            float4 frag (v2f i) : SV_Target
            {
                float4 res = float4(_BloomfogPrevTex_TexelSize.xy, 0, 0);
                
                // Sample at current mip level
                float4 mipBaseUV = float4(i.uv, 0, 0);
                float4 mipColor = float4(0,0,0,0);
                    
                // Box blur sampling
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-2, -2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-1, -2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(0, -2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(1, -2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(2, -2, 0, 0) * res).rgb);

                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-2, -1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-1, -1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(0, -1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(1, -1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(2, -1, 0, 0) * res).rgb);

                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-2, 0, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-1, 0, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(0, 0, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(1, 0, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(2, 0, 0, 0) * res).rgb);

                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-2, 1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-1, 1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(0, 1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(1, 1, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(2, 1, 0, 0) * res).rgb);

                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-2, 2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(-1, 2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(0, 2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(1, 2, 0, 0) * res).rgb);
                mipColor.rgb += saturate(tex2Dlod(_BloomfogPrevTex, mipBaseUV + float4(2, 2, 0, 0) * res).rgb);
                mipColor.rgb /= 25.0;

                // Contribution factor based on mip level (taken from ArcViewer)
                //float contribution = pow(0.5, (float)i / (float)_BloomfogPass);
                mipColor.a = _BloomfogAlpha;
                
                return mipColor;
            }
            ENDCG
        }
    }
}
