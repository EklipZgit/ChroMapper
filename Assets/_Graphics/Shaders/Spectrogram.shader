Shader "ChroMapper/Spectrogram"
{
    Properties
    {
        [Space(10)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(None, PP, Frag)] _BloomType ("Bloom Type", float) = 0
        [KeywordEnum(Before Emissive, After Emissive)] _AcesTonemap ("ACES Tonemapping", float) = 1

        [Header(Lighting)] [Space]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", float) = 1
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Light Falloff", float) = 0
        
        [Toggle(SPECULAR)] _EnableSpecular ("Specular", float) = 1
        _SpecularIntensity ("Specular Intensity", float) = 1

        [Header(Fog Settings)] [Space]
        _FogStartOffset ("Fog Start Offset", Float) = 1
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1

        [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
        [Toggle] _ZWrite ("Z Write", Float) = 1
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ ENABLE_HEIGHT_FOG
            #pragma shader_feature DIFFUSE
            #pragma shader_feature SPECULAR
            #pragma shader_feature LIGHT_FALLOFF
            #pragma multi_compile _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG
            #pragma multi_compile _ACESTONEMAP_BEFORE_EMISSIVE _ACESTONEMAP_AFTER_EMISSIVE
            #pragma multi_compile ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/CustomLighting.cginc"

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
                float4 customScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Smoothness;
            float _Metallic;
            
            float _SpecularIntensity;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;
                o.worldNormal = UnityObjectToWorldNormal(i.normal);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                fixed4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                float3 worldPos = i.worldPos;
                float3 worldNormal = normalize(i.worldNormal);
                float3 calculated = 0;
                CUSTOM_LIGHTING_APPLY(calculated, albedo, _Metallic, _Smoothness, _SpecularIntensity, worldPos, worldNormal);
                albedo.rgb = calculated;
                
                #if _BLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(albedo, 1);
                #elif _BLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(albedo, 1);
                #else
                CUSTOM_BLOOM_NONE_APPLY(albedo);
                #endif

                #if ENABLE_HEIGHT_FOG
                BLOOM_FOG_HEIGHT_FOG_APPLY(bloomfog_color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif

                return albedo;
            }
            ENDCG
        }
    }
}