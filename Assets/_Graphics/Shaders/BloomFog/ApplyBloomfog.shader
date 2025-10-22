Shader "ChroMapper/Post Process/ApplyBloomfog"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    
    TEXTURE2D_SAMPLER2D(_BloomPrePassTexture, sampler_BloomPrePassTexture);

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        return SAMPLE_TEXTURE2D(_BloomPrePassTexture, sampler_BloomPrePassTexture, i.texcoord);
    }
    ENDHLSL
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend One One
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
