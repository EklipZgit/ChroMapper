Shader "ChroMapper/Post Process/Tonemapping"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    #include "../CGIncludes/CustomTonemapping.cginc"
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    #pragma multi_compile ACES_TONE_MAPPING

    float4 Frag(VaryingsDefault i) :
    SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        ACES_TONE_MAPPING_APPLY(color);
        return color;
    }
    ENDHLSL
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}