// Modified Unity's Post Processing bloom shader to match Beat Saber bloom behaviour
Shader "ChroMapper/Post Process/Bloom"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Colors.hlsl"
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Sampling.hlsl"
    #include "../ShaderLibrary/CustomTonemapping.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);

    float4 _MainTex_TexelSize;
    float _SampleScale;
    float _Intensity;

    float4 FragPrefilter(VaryingsDefault i) : SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        color.rgb *= color.a;
        return color;
    }

    float4 FragDownsample13(VaryingsDefault i) : SV_Target
    {
        float4 color = DownsampleBox13Tap(
            TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord,
            UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy);
        return color;
    }

    float4 FragDownsample4(VaryingsDefault i) : SV_Target
    {
        float4 color = DownsampleBox4Tap(
            TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord,
            UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy);
        return color;
    }

    float4 Combine(float4 bloom, float2 uv)
    {
        float4 color = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, uv);
        return bloom + color;
    }

    float4 FragUpsampleTent(VaryingsDefault i) : SV_Target
    {
        float4 bloom = UpsampleTent(
            TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy,
            _SampleScale);
        return Combine(bloom, i.texcoordStereo);
    }

    float4 FragUpsampleBox(VaryingsDefault i) : SV_Target
    {
        float4 bloom = UpsampleBox(
            TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy,
            _SampleScale);
        return Combine(bloom, i.texcoordStereo);
    }

    float4 FragComposite(VaryingsDefault i) : SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

        // Give whiteness to glowing material, and lets other lights light material show through blooms.
        float alpha = saturate(color.a);
        float4 invert = 1 - color;
        color = alpha * invert + color;

        float4 bloom = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord);
        color = bloom * 0.2 + color;  // Cranking this up produces too much glow around Walls, but more accurately resembles color glow.
        color.rgb = saturate(color.rgb);

        // Either this effect is subtle or this doesn't do as I expect it to do
        // REINHARD_TONE_MAPPING_APPLY(color);

        return color;
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        return color;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragPrefilter
            ENDHLSL
        }
        // 1
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample13
            ENDHLSL
        }
        // 2
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample4
            ENDHLSL
        }
        // 3
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleTent
            ENDHLSL
        }
        // 4
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleBox
            ENDHLSL
        }
        // 5
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragComposite
            ENDHLSL
        }
        // 6 debug
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}