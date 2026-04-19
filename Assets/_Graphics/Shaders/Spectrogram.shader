Shader "ChroMapper/Spectrogram"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _PeakOffset ("Peak Offset", Vector) = (0,0,8,1)

        [Header(Lighting)] [Space]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", float) = 1
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Light Falloff", float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Toggle(SPECULAR)] _EnableSpecular ("Specular", float) = 1
        _SpecularIntensity ("Specular Intensity", float) = 1

        [Header(Fog Settings)] [Space]
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment SPECULAR
            #pragma shader_feature_local_fragment LIGHT_FALLOFF

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomBloom.hlsl"
            #include "ShaderLibrary/CustomLighting.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"
            #include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"

            float _SpectrogramData[64];
            float3 _PeakOffset;

            float _Smoothness;
            float _Metallic;
            float _SpecularIntensity;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float segment = floor(i.uv.x * 64);
                float audioData = _SpectrogramData[segment];
                i.vertex.xyz += i.uv.y * (audioData * _PeakOffset.xyz - _PeakOffset.xyz);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                o.uv.xy = i.uv.xy;
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                albedo.rgb *= calculate_global_diffuse_lighting(i.worldPos, i.worldNormal);

                ACES_TONE_MAPPING_APPLY(albedo);

                #if defined(BLOOM_FOG)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset,
                                       _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}