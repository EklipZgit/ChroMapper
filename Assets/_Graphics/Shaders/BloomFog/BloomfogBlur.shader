Shader "Hidden/BloomfogBlurring"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        // Shared shader logic
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "../CGIncludes/Blurs.cginc"

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

        float _AutoExposureLimit;
        float _BloomfogCombineSrc;
        float _BloomfogCombineDst;
        float _BloomfogBlurRadius;
        
        sampler2D _BloomfogPrevTex;
        float4 _BloomfogPrevTex_TexelSize;
        float4 _BloomfogPrevTex_ST;

        sampler2D _BloomfogSrcTex;
        float4 _BloomfogSrcTex_TexelSize;
        float4 _BloomfogSrcTex_ST;

        sampler2D _BloomfogGlobalIntensityTex;
        float4 _BloomfogGlobalIntensityTex_TexelSize;
        float4 _BloomfogGlobalIntensityTex_ST;
        
        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        float4 combine(float4 source, float4 dest)
        {
            return (source * _BloomfogCombineSrc) + (dest * _BloomfogCombineDst);
        }

        float4 autoExposure(float4 color, float2 uv)
        {
            float4 globalIntensity = tex2D(_BloomfogGlobalIntensityTex, uv);

            // Calculate luminance
            float luminance = dot(globalIntensity.rgb, unity_ColorSpaceLuminance.rgb);

            // Normalize auto exposure limit since it goes from 0-1000+
            //float normalizedLimit = 0.5;
            float normalizedLimit = lerp(0.001, 0.5, _AutoExposureLimit / 1000.0);
            //float normalizedLimit = _AutoExposureLimit / 1000.0;

            // Smooth transition using smoothstep
            // Below the limit, gradually reduce the multiplier
            float exposureMultiplier = smoothstep(0.0, normalizedLimit, luminance);

            // Ensure we don't completely kill the bloom, keep a minimum
            // Scale minimum based on the limit to maintain consistency
            float minMultiplier = saturate(normalizedLimit * 0.1);
            exposureMultiplier = max(exposureMultiplier, minMultiplier);

            color *= exposureMultiplier;
 
            return color;
        }
        ENDCG

        // Downscale pass - kawase blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {   
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x >= 0.0)
                {
                    uv.y = 1.0 - uv.y;
                }
                #endif

                float2 texelSize = abs(_BloomfogSrcTex_TexelSize.xy);
                float4 originalColor = tex2D(_BloomfogSrcTex, uv);

                float4 blurColor = kawase(_BloomfogSrcTex, uv, _BloomfogBlurRadius, texelSize);

                return combine(originalColor, blurColor);
            }
            ENDCG
        }

        // Upscale pass - box blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x >= 0.0)
                {
                    uv.y = 1.0 - uv.y;
                }
                #endif

                float4 originalColor = tex2D(_BloomfogPrevTex, uv);
                float2 texelSize = abs(_BloomfogSrcTex_TexelSize.xy);

                // Sample 5x5 box blur
                float4 blurColor = box(_BloomfogSrcTex, uv, _BloomfogBlurRadius, texelSize, 1);
                blurColor = autoExposure(blurColor, uv);

                return combine(originalColor, blurColor);
            }
            ENDCG
        }

        // Final upscale pass - box blur + gamma correction
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x >= 0.0)
                {
                    uv.y = 1.0 - uv.y;
                }
                #endif

                float4 originalColor = tex2D(_BloomfogPrevTex, uv);
                float2 texelSize = abs(_BloomfogSrcTex_TexelSize.xy);

                // Sample 5x5 box blur
                float4 blurColor = box(_BloomfogSrcTex, uv, _BloomfogBlurRadius, texelSize, 1);
                blurColor = autoExposure(blurColor, uv);
                blurColor.rgb = GammaToLinearSpace(blurColor.rgb);

                return combine(originalColor, blurColor);
            }
            ENDCG
        }
    }
}
