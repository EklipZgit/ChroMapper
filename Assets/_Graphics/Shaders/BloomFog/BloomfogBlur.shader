/*
TODO:

Okay so I admit that a lot of this can essentially be copy/pasted from CustomBloom.shader
I reimplemented all of this not knowing how much could be reused, so now we're left with
one nicely implemented shader (CustomBloom) and whatever mess this shader turned out to be.

Whoops!
*/

Shader "Hidden/BloomfogBlurring"
{
    Properties {}
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        // Shared shader logic
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "../ShaderLibrary/Blurs.hlsl"
        #include "../ShaderLibrary/CustomTonemapping.hlsl"

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

        v2f vert(appdata v)
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
            // sample in the middle to get a free 1x1 mip
            float4 globalIntensity = tex2D(_BloomfogGlobalIntensityTex, float2(0.5, 0.5));

            // according to owen: saves 1 multiplication instruction
            // we multiply the luminance constant by 49 bc we are soon to sqrt it to effectively equal 7 / sqrt(luminance)
            // Performs better doing a 7x multiplication plus the 0.005 in the other spot for whatever reason
            float luminance = dot(globalIntensity, float3(0.299, 0.587, 0.114) * 49);

            // Beat Saber's actual equation is 7 / sqrt(luminance)
            color /= sqrt(luminance);

            return color;
        }
        ENDHLSL

        // Downscale pass - 4-point box downsample
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x >= 0.0)
                {
                    uv.y = 1.0 - uv.y;
                }
                #endif

                float2 texelSize = abs(_BloomfogSrcTex_TexelSize.xy);

                float4 downsampled = downsample4(_BloomfogSrcTex, uv, _BloomfogBlurRadius, texelSize);
                return downsampled;
            }
            ENDHLSL
        }

        // Upscale pass - tent upsample
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x >= 0.0)
                {
                    uv.y = 1.0 - uv.y;
                }
                #endif

                float4 srcColor = tex2D(_BloomfogSrcTex, uv);
                float2 texelSize = abs(_BloomfogSrcTex_TexelSize.xy);

                float4 upsampledBlur = upsampleTent(_BloomfogSrcTex, uv, _BloomfogBlurRadius, texelSize);

                float4 bloomColor = tex2D(_BloomfogPrevTex, uv);
                float4 combined = combine(bloomColor, upsampledBlur);

                return combined;
            }
            ENDHLSL
        }

        // Final upscale pass - tent upsample + ACES tone mapping
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x >= 0.0)
                {
                    uv.y = 1.0 - uv.y;
                }
                #endif

                // Use tent filter for high-quality upsampling
                float4 srcColor = tex2D(_BloomfogSrcTex, uv);
                float2 texelSize = abs(_BloomfogSrcTex_TexelSize.xy);

                float4 upsampledBlur = upsampleTent(_BloomfogSrcTex, uv, _BloomfogBlurRadius, texelSize);

                float4 bloomColor = tex2D(_BloomfogPrevTex, uv);
                float4 combined = combine(bloomColor, upsampledBlur);

                combined = autoExposure(combined, uv);
                ACES_TONE_MAPPING_APPLY(combined);
                return saturate(combined);
            }
            ENDHLSL
        }
    }
}