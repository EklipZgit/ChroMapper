Shader "ChroMapper/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [KeywordEnum(Before Emissive, After Emissive)] _AcesTonemap ("ACES Tonemapping", float) = 1

        [Header(Lighting)] [Space]
        [Toggle(METAL_SMOOTHNESS_TEXTURE)] _EnableMetalSmoothnessTex ("Multi Purpose Map", float) = 0
        _MetalSmoothnessTex ("MPM Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Space(20)]
        _AmbientMinimalValue ("Ambient Minimum", Range(0, 1)) = 0
        _NominalDiffuseLevel ("Ambient Color", Color) = (0,0,0,0)
        _AmbientMultiplier ("Ambient Color Multiplier", float) = 1

        [Space(20)]
        [KeywordEnum(None, Color, Emission, Metal Smoothness, Special, Displacement, Emissive Mult Add)] _Vertex ("Vertex Color Mode", float) = 0
        _EmissionThreshold ("Emission Threshold", Range(0, 1)) = 0
        _EmissionColor ("Emission Color", Color) = (1,1,1,0)
        _EmissionStrength ("Emission Strength", float) = 1
        _EmissionBloomIntensity ("Bloom Intensity", float) = 1
        [KeywordEnum(None, PP, Frag)] _Vertex_BloomType ("Vertex Color Treatment", float) = 0

        [Space(20)]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", float) = 1
        [Toggle(BOTH_SIDES_DIFFUSE)] _EnableBothSidesDiffuse ("Both Sides Diffuse", float) = 0
        _BothSidesDiffuseMultiplier ("Other Diffuse Multiplier", float) = 1
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Light Falloff", float) = 0
        [Toggle(DIFFUSE_TEXTURE)] _EnableDiffuseTexture ("Albedo Texture", float) = 0
        [KeywordEnum(Texture, MPM R, MPM A Smoothness)] _Diffuse_Texture_Source ("Diffuse Texture Source", float) = 0
        _DiffuseTexture ("Diffuse Texture", 2D) = "white" {}
        _AlbedoMultiplier ("Albedo Multiplier", float) = 1

        [Space(20)]
        [Toggle(SPECULAR)] _EnableSpecular ("Specular", float) = 1
        _SpecularIntensity ("Specular Intensity", float) = 1

        [Space(20)]
        [Toggle(RIM_DIM)] _EnableRimDim ("Rim Dim", float) = 0
        _RimScale ("Rim Scale", float) = 1
        _RimOffset ("Rim Offset", float) = 1
        _RimDistanceOffset ("Rim Distance Offset", float) = 2
        _RimDistanceScale ("Rim Distance Scale", float) = 0.3
        _RimSmoothness ("Rim Smoothness", float) = 1
        _RimDarkening ("Rim Darkening", float) = 0
        [Toggle(INVERT_RIM_DIM)] _InvertRimDim ("Invert Rim Dim", float) = 0

        [Space(20)]
        [KeywordEnum(None, Simple, Pulse, Flipbook)] _EmissionTexture ("Texture Emission", float) = 0
        [KeywordEnum(Texture, Fill, MPM G, SDF)] _Emission_Texture_Source ("Emission Source", float) = 0
        _EmissionTex ("Emission Texture", 2D) = "white" {}
        _EmissionTexSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [Toggle(SECONDARY_UVS_EMISSION)] _SecondaryUVsEmissionTex ("Use Secondary UVs", float) = 0
        [KeywordEnum(Emission G, Copy Emission, MPM R)] _Emission_Alpha_Source ("Alpha Source", float) = 0
        _EmissionBrightness ("Brightness", float) = 1
        [Toggle(EMISSION_ANGLE_DISAPPEAR)] _EnableEmissionAngleDisappear ("Angle Disappear", float) = 0
        _EmissionThresholdAngle ("Threshold Angle", float) = 0
        [KeywordEnum(Flat, Frag, Gradient, PP)] _EmissionBloomType ("Emission Color Treatment", float) = 0
        _EmissionTexColor ("Emission Color", Color) = (1,1,1,1)

        [Space(10)]
        _EmissionGradientTex ("Gradient LUT", 2D) = "white" {}
        _EmissionGradientPosition ("LUT Position", float) = 0.5
        _EmissionGradientPanningSpeed ("LUT Panning", float) = 0
        _EmissionGradientIntensity ("LUT Intensity", float) = 1

        [Space(10)]
        _EmissionTexBloomIntensity ("Bloom Intensity", float) = 1
        _EmissionTexWhiteBoostMultiplier ("White Boost Multiplier", float) = 1

        [Space(20)]
        [Toggle(PRIVATE_POINT_LIGHT)] _EnablePrivatePointLight ("Private Point Light", float) = 0
        _PrivatePointLightColor ("Color", Color) = (0,0.5,1,1)
        [Toggle(POINT_LIGHT_IS_LOCAL)] _PointLightPositionLocal ("Make Position Local", float) = 0
        _PrivatePointLightIntensity ("Intensity Multiplier", float) = 1
        _PrivatePointLightPosition ("Light World Position", Vector) = (0,0,0,1)

        [Header(Fog Settings)] [Space]
        [Toggle(ENABLE_FOG)] _EnableFog ("Enable Fog", float) = 1
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Header(Settings)] [Space]
        [Toggle(ALPHA_CUTOUT)] _AlphaCutout ("Alpha Cutout", float) = 0
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature ALPHA_CUTOUT
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ ENABLE_FOG
            #pragma multi_compile _ ENABLE_HEIGHT_FOG

            #pragma multi_compile _ACESTONEMAP_BEFORE_EMISSIVE _ACESTONEMAP_AFTER_EMISSIVE
            #pragma multi_compile ACES_TONE_MAPPING

            #pragma shader_feature METAL_SMOOTHNESS_TEXTURE

            #pragma multi_compile _ _VERTEX_COLOR _VERTEX_EMISSION _VERTEX_METAL_SMOOTHNESS _VERTEX_SPECIAL _VERTEX_DISPLACEMENT _VERTEX_EMISSIVE_MULT_ADD
            #pragma multi_compile _ _VERTEX_BLOOMTYPE_PP _VERTEX_BLOOMTYPE_FRAG

            #pragma shader_feature DIFFUSE
            #pragma shader_feature DIFFUSE_TEXTURE
            #pragma multi_compile _DIFFUSE_TEXTURE_SOURCE_TEXTURE _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS
            #pragma shader_feature BOTH_SIDES_DIFFUSE
            #pragma shader_feature LIGHT_FALLOFF
            #pragma shader_feature SPECULAR
            #pragma shader_feature RIM_DIM
            #pragma shader_feature INVERT_RIM_DIM

            #pragma multi_compile _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_PULSE _EMISSIONTEXTURE_FLIPBOOK
            #pragma multi_compile _EMISSION_TEXTURE_SOURCE_TEXTURE _EMISSION_TEXTURE_SOURCE_FILL _EMISSION_TEXTURE_SOURCE_MPM_G _EMISSION_TEXTURE_SOURCE_SDF
            #pragma multi_compile _EMISSIONBLOOMTYPE_FLAT _EMISSIONBLOOMTYPE_FRAG _EMISSIONBLOOMTYPE_GRADIENT _EMISSIONBLOOMTYPE_PP
            #pragma multi_compile _EMISSION_ALPHA_SOURCE_EMISSION_G _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R

            #pragma shader_feature PRIVATE_POINT_LIGHT
            #pragma shader_feature POINT_LIGHT_IS_LOCAL

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/CustomLighting.cginc"

            #ifndef UNITY_INSTANCING_ENABLED
            float4 _Color;
            #endif

            #ifdef METAL_SMOOTHNESS_TEXTURE
            sampler2D _MetalSmoothnessTex;
            float4 _MetalSmoothnessTex_ST;
            #endif

            float _Smoothness;
            float _Metallic;
            float _SpecularIntensity;

            float _AmbientMinimalValue;
            float4 _NominalDiffuseLevel;
            float _AmbientMultiplier;

            #ifdef DIFFUSE_TEXTURE
            sampler2D _DiffuseTex;
            float4 _DiffuseTex_ST;
            #endif
            float _BothSidesDiffuseMultiplier;
            float _AlbedoMultiplier;

            #define USE_EMISSION_TEXTURE defined(_EMISSION_TEXTURE_SOURCE_TEXTURE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK)
            #if USE_EMISSION_TEXTURE
            sampler2D _EmissionTex;
            float4 _EmissionTex_ST;
            #endif

            #define USE_EMISSION_COLOR !defined(_EMISSIONBLOOMTYPE_GRADIENT) && (defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK))
            #if !defined(UNITY_INSTANCING_ENABLED) && USE_EMISSION_COLOR
            float4 _EmissionTexColor;
            #endif

            #define USE_EMISSION_GRADIENT_TEXTURE defined(_EMISSIONBLOOMTYPE_GRADIENT) || defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK)
            #if USE_EMISSION_GRADIENT_TEXTURE
            sampler2D _EmissionGradientTex;
            float4 _EmissionGradientTex_ST;
            #endif
            #ifdef _EMISSIONBLOOMTYPE_GRADIENT
            float _EmissionGradientPosition;
            float _EmissionGradientPanningSpeed;
            float _EmissionGradientIntensity;
            #endif

            float _EmissionTexBloomIntensity;
            float _EmissionTexWhiteboostMultiplier;

            #define USE_VERTEX_EMISSION defined(_VERTEX_EMISSION) || defined(_VERTEX_SPECIAL) || defined(_VERTEX_EMISSIVE_MULT_ADD)
            #if USE_VERTEX_EMISSION
            float _EmissionThreshold;
            #ifndef UNITY_INSTANCING_ENABLED
            float4 _EmissionColor;
            #endif
            float _EmissionStrength;
            float _EmissionBloomIntensity;
            #endif

            #ifdef RIM_DIM
            float _RimScale;
            float _RimOffset;
            float _RimDistanceOffset;
            float _RimDistanceScale;
            float _RimSmoothness;
            float _RimDarkening;
            #endif

            #if defined(ENABLE_BLOOM_FOG) && defined(ENABLE_FOG)
            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;
            #endif

            #ifdef UNITY_INSTANCING_ENABLED
            UNITY_INSTANCING_BUFFER_START (Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            #if USE_EMISSION_COLOR
            UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTexColor)
            #endif
            #if USE_VERTEX_EMISSION
            UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
            #endif
            #if PRIVATE_POINT_LIGHT
            UNITY_DEFINE_INSTANCED_PROP(float4, _PrivatePointLightColor)
            #endif
            UNITY_INSTANCING_BUFFER_END (Props)
            #endif

            struct appdata
            {
                float4 vertex : POSITION;
                #if USE_VERTEX_EMISSION
                float4 color : COLOR;
                #endif
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if USE_VERTEX_EMISSION
                float4 color : COLOR;
                #endif
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 customScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                #if USE_VERTEX_EMISSION
                o.color = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
                #endif
                o.uv = i.uv;
                o.worldNormal = UnityObjectToWorldNormal(i.normal);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                #if USE_EMISSION_COLOR
                float4 albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor);
                #elif USE_VERTEX_EMISSION
                float4 albedo = i.color;
                #else
                float4 albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #endif

                #ifdef ALPHA_CUTOUT
                if (albedo.a == 0) discard;
                #endif

                float3 worldPos = i.worldPos;
                float3 worldNormal = normalize(i.worldNormal);
                float3 calculated = 0;
                CUSTOM_LIGHTING_APPLY(calculated, albedo, _Metallic, _Smoothness, _SpecularIntensity, worldPos, worldNormal);
                albedo.rgb = calculated;

                #if USE_EMISSION_COLOR
                #if _EMISSIONBLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(albedo, _EmissionTexBloomIntensity);
                #elif _EMISSIONBLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _EmissionTexWhiteBoostMultiplier);
                #else
                CUSTOM_BLOOM_NONE_APPLY(albedo);
                #endif
                #endif

                #if USE_VERTEX_EMISSION
                #if _VERTEX_BLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(albedo, 1);
                #elif _VERTEX_BLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _EmissionBloomIntensity);
                #else
                CUSTOM_BLOOM_NONE_APPLY(albedo);
                #endif
                #endif

                #if defined(ENABLE_BLOOM_FOG) && defined(ENABLE_FOG)
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