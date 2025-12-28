Shader "Hidden/BloomfogAutoExposure"
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
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _BloomfogPrevTex;
            float _AutoExposureLimit;

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_BloomfogPrevTex, i.uv);
                
                // Calculate luminance
                float luminance = dot(col.rgb, unity_ColorSpaceLuminance.rgb);

                // Apply auto-exposure limit
                
                // Normalize auto exposure limit since it goes from 0-1000+
                float normalizedLimit = lerp(0.1, 0.5, _AutoExposureLimit / 1000.0);
                //float normalizedLimit = _AutoExposureLimit / 1000.0;

                // Smooth transition using smoothstep
                // Below the limit, gradually reduce the multiplier
                float exposureMultiplier = smoothstep(0.0, normalizedLimit, luminance);

                // Ensure we don't completely kill the bloom, keep a minimum
                // Scale minimum based on the limit to maintain consistency
                float minMultiplier = saturate(normalizedLimit * 0.1);
                exposureMultiplier = max(exposureMultiplier, minMultiplier);

                col *= exposureMultiplier;

                return col;
            }
            ENDCG
        }
    }
}
