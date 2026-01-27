Shader "ChroMapper/Lit"
{
    Properties
    {
        [Space(10)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(None,PP,Frag)] _BloomWhite ("Bloom White", float) = 0
        _BloomWhiteMultiplier ("White Multiplier", float) = 1
        [KeywordEnum(Before Emissive, After Emissive)] _AcesTonemap ("ACES Tonemapping", float) = 1

        [Space(10)]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", float) = 1
        [Toggle(SPECULAR)] _EnableSpecular ("Specular", float) = 1
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Light Falloff", float) = 0
        [Toggle(BOTH_SIDES_DIFFUSE)] _EnableBothSidesDiffuse ("Both Sides Diffuse", float) = 0
        _BothSidesDiffuseMultiplier ("Other Diffuse Multiplier", Float) = 1

        [Space(10)]
        [Toggle(RIM_DIM)] _EnableRimDim ("Rim Dim", Float) = 0
        _RimScale ("Rim Scale", Float) = 1
        _RimOffset ("Rim Offset", Float) = 1
        _RimDistanceOffset ("Rim Distance Offset", Float) = 2
        _RimDistanceScale ("Rim Distance Scale", Float) = 0.3
        _RimSmoothness ("Rim Smoothness", Float) = 1
        _RimDarkening ("Rim Darkening", Float) = 0
        [Toggle(INVERT_RIM_DIM)] _InvertRimDim ("Invert Rim Dim", Float) = 0

        [Header(Lighting)] [Space]
        [Toggle(PRIVATE_POINT_LIGHT)] _EnablePrivatePointLight ("Private Point Light", float) = 0
        _PrivatePointLightColor ("Color", Color) = (0,0.5,1,1)
        [Toggle(POINT_LIGHT_IS_LOCAL)] _PointLightPositionLocal ("Make Position Local", Float) = 0
        _PrivatePointLightIntensity ("Intensity Multiplier", Float) = 1
        _PrivatePointLightPosition ("Light World Position", Vector) = (0,0,0,1)

        [Header(Fog Settings)] [Space]
        [Toggle(ENABLE_FOG)] _EnableFog ("Enable Fog", Float) = 1
        _FogStartOffset ("Fog Start Offset", Float) = 1
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1

        [Header(Settings)] [Space]
        [Toggle(ALPHA_CUTOUT)] _AlphaCutout ("Alpha Cutout", Float) = 0
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
            #pragma multi_compile _ ENABLE_FOG
            #pragma multi_compile _ ENABLE_HEIGHT_FOG
            #pragma shader_feature ALPHA_CUTOUT
            #pragma shader_feature DIFFUSE
            #pragma shader_feature SPECULAR
            #pragma shader_feature LIGHT_FALLOFF
            #pragma shader_feature BOTH_SIDES_DIFFUSE
            #pragma shader_feature RIM_DIM
            #pragma shader_feature INVERT_RIM_DIM
            #pragma shader_feature PRIVATE_POINT_LIGHT
            #pragma shader_feature POINT_LIGHT_IS_LOCAL
            #pragma multi_compile _BLOOMWHITE_NONE _BLOOMWHITE_PP _BLOOMWHITE_FRAG
            #pragma multi_compile _ACESTONEMAP_BEFORE_EMISSIVE _ACESTONEMAP_AFTER_EMISSIVE
            #pragma multi_compile ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/CustomLighting.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                #if PRIVATE_POINT_LIGHT
                UNITY_DEFINE_INSTANCED_PROP(float4, _PrivatePointLightColor)
                #endif
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

            float _BloomWhiteMultiplier;

            float _Smoothness;
            float _Metallic;

            float _RimScale;
            float _RimOffset;
            float _RimDistanceOffset;
            float _RimDistanceScale;
            float _RimSmoothness;
            float _RimDarkening;

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

                float4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));

                #ifdef ALPHA_CUTOUT
                if (albedo.a == 0) discard;
                #endif

                float3 worldPos = i.worldPos;
                float3 worldNormal = normalize(i.worldNormal);
                float3 calculated = 0;
                CUSTOM_LIGHTING_APPLY(calculated, albedo, _Metallic, _Smoothness, worldPos, worldNormal);
                albedo.rgb = calculated;

                albedo = ApplyCustomBloom(albedo, _BloomWhiteMultiplier);

                #if ENABLE_FOG
                #if ENABLE_HEIGHT_FOG
                BLOOM_FOG_HEIGHT_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                return albedo;
            }
            ENDCG
        }
    }
}