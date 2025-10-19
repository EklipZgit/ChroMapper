Shader "Hidden/BloomfogUpscale"
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

            // Global properties
            uniform int _BloomfogUpscalePasses = 5;
            sampler2D _BloomfogPrePassTex;
            float4 _BloomfogPrePassTex_TexelSize;
            float4 _BloomfogPrePassTex_ST;

            float4 frag (v2f i) : SV_Target
            {
                // Start with mip 0 as base color
                float4 color = tex2Dlod(_BloomfogPrePassTex, float4(i.uv, 0, 0));
                float4 res = float4(_BloomfogPrePassTex_TexelSize.xy, 0, 0);
                
                // Loop through mip levels 1 to _BloomfogUpscalePasses
                for (int mip = 1; mip <= 10; mip++)
                {
                    // Sample at current mip level
                    float4 mipBaseUV = float4(i.uv, 0, mip);
                    float4 mipColor = float4(0,0,0,0);
                    
                    // Box blur sampling
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-2, -2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-1, -2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(0, -2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(1, -2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(2, -2, 0, 0) * res).rgb;

                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-2, -1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-1, -1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(0, -1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(1, -1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(2, -1, 0, 0) * res).rgb;

                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-2, 0, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-1, 0, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(0, 0, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(1, 0, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(2, 0, 0, 0) * res).rgb;

                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-2, 1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-1, 1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(0, 1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(1, 1, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(2, 1, 0, 0) * res).rgb;

                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-2, 2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(-1, 2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(0, 2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(1, 2, 0, 0) * res).rgb;
                    mipColor.rgb += tex2Dlod(_BloomfogPrePassTex, mipBaseUV + float4(2, 2, 0, 0) * res).rgb;
                    mipColor.rgb /= 25.0;

                    // Contribution factor based on mip level (taken from ArcViewer)
                    float contribution = pow(0.5, (float)i / (float)_BloomfogUpscalePasses);
                    mipColor.a = contribution;

                    // Add weighted mip level to aggregate color
                    color += mipColor;
                }
                
                return color;
            }
            ENDCG
        }
    }
}
