Shader "ChroMapper/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [KeywordEnum(Before Emissive, After Emissive)] _AcesTonemap ("ACES Tonemapping", float) = 1

        [Header(Lighting)] [Space]
        [Toggle(METAL_SMOOTHNESS_TEXTURE)] _EnableMetalSmoothnessTex ("Multi Purpose Map", float) = 0
        _MetalSmoothnessTex ("MPM Texture", 2D) = "white" {}
        [KeywordEnum(None, MPM R, MPM A)] _Metallic_Texture_Source ("Metallic Source", float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 1
        [KeywordEnum(None, MPM A, MPM G Roughness)] _Smoothness_Texture_Source ("Smoothness Source", float) = 0
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
        _DiffuseTex ("Diffuse Texture", 2D) = "white" {}
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
        [KeywordEnum(Texture, MPM G)] _Emission_Texture_Source ("Emission Source", float) = 0
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

        [Toggle(EMISSION_MASK)] _EnableEmissionMask ("Layer 2", float) = 0
        [KeywordEnum(Multiply, Add, Masked Add)] _MaskBlend ("Layer Blend", float) = 0
        _EmissionMask ("Layer Texture", 2D) = "white" {}
        [Toggle(SECONDARY_UVS_EMISSION_MASK)] _SecondaryUVsMask ("Use Secondary UVs", float) = 0
        _EmissionMaskSpeed ("Layer Texture Speed", Vector) = (0,1,0,0)
        _EmissionMaskIntensity ("Layer Intensity", float) = 1
        [Toggle(SECONDARY_EMISSION_MASK)] _EnableSecondaryEmissionMask ("Layer 3", float) = 0
        [KeywordEnum(Multiply, Add, Masked Add)] _Secondary_MaskBlend ("Layer Blend", float) = 0
        _SecondaryEmissionMask ("Layer Texture", 2D) = "white" {}
        [Toggle(SECONDARY_UVS_EMISSION_MASK2)] _SecondaryUVsMask2 ("Use Secondary UVs", float) = 0
        _SecondaryEmissionMaskSpeed ("Texture Speed", Vector) = (0,1,0,0)
        _SecondaryEmissionMaskIntensity ("Layer Intensity", float) = 1

        _EmissionMaskStepValue ("Step Value", Range(0, 1)) = 0.5
        _EmissionMaskStepWidth ("Step Width", Range(0, 0.5)) = 0.1

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
            #pragma multi_compile _ _METALLIC_TEXTURE_SOURCE_MPM_R _METALLIC_TEXTURE_SOURCE_MPM_A
            #pragma multi_compile _ _SMOOTHNESS_TEXTURE_SOURCE_MPM_A _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS

            #pragma multi_compile _ _VERTEX_COLOR _VERTEX_EMISSION _VERTEX_METAL_SMOOTHNESS _VERTEX_SPECIAL _VERTEX_DISPLACEMENT _VERTEX_EMISSIVE_MULT_ADD
            #pragma multi_compile _ _VERTEX_BLOOMTYPE_PP _VERTEX_BLOOMTYPE_FRAG

            #pragma shader_feature DIFFUSE
            #pragma shader_feature DIFFUSE_TEXTURE
            #pragma multi_compile _ _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS
            #pragma shader_feature BOTH_SIDES_DIFFUSE
            #pragma shader_feature LIGHT_FALLOFF
            #pragma shader_feature SPECULAR
            #pragma shader_feature RIM_DIM
            #pragma shader_feature INVERT_RIM_DIM

            #pragma multi_compile _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_PULSE _EMISSIONTEXTURE_FLIPBOOK
            #pragma multi_compile _EMISSION_TEXTURE_SOURCE_TEXTURE _EMISSION_TEXTURE_SOURCE_MPM_G
            #pragma multi_compile _EMISSIONBLOOMTYPE_FLAT _EMISSIONBLOOMTYPE_FRAG _EMISSIONBLOOMTYPE_GRADIENT _EMISSIONBLOOMTYPE_PP
            #pragma multi_compile _EMISSION_ALPHA_SOURCE_EMISSION_G _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R
            #pragma shader_feature EMISSION_MASK
            #pragma multi_compile _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD
            #pragma shader_feature SECONDARY_UVS_EMISSION_MASK
            #pragma shader_feature SECONDARY_EMISSION_MASK
            #pragma multi_compile _ _SECONDARY_MASKBLEND_ADD _SECONDARY_MASKBLEND_MASKED_ADD
            #pragma shader_feature SECONDARY_UVS_EMISSION_MASK2

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
            float _EmissionTexWhiteBoostMultiplier;

            #define USE_EMISSION_MASK defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_SIMPLE)
            #if USE_EMISSION_MASK

            #ifdef EMISSION_MASK
            sampler2D _EmissionMask;
            float _EmissionMaskSpeed;
            float _EmissionMaskIntensity;
            #endif

            #ifdef SECONDARY_EMISSION_MASK
            sampler2D _SecondaryEmissionMask;
            float _SecondaryEmissionMaskSpeed;
            float _SecondaryEmissionMaskIntensity;
            #endif

            float _EmissionMaskStepValue;
            float _EmissionMaskStepWidth;
            #endif

            #define USE_VERTEX_EMISSION defined(_VERTEX_EMISSION) || defined(_VERTEX_SPECIAL) || defined(_VERTEX_EMISSIVE_MULT_ADD)
            #define USE_VERTEX_COLOR defined(_VERTEX_COLOR) || USE_VERTEX_EMISSION
            #if USE_VERTEX_COLOR
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
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if USE_VERTEX_COLOR
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
                #if USE_VERTEX_COLOR
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #if _VERTEX_BLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(o.color, 1);
                #elif _VERTEX_BLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(o.color, _EmissionBloomIntensity);
                #else
                CUSTOM_BLOOM_NONE_APPLY(o.color);
                #endif
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

                float4 baseColor = 0;
                #if USE_VERTEX_EMISSION
                baseColor = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
                #elif USE_VERTEX_COLOR
                baseColor = i.color;
                #else
                #ifdef UNITY_INSTANCED_ENABLED
                baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #endif
                baseColor.a = 0;
                #endif

                float4 albedo = baseColor;
                #ifdef DIFFUSE_TEXTURE
                #ifdef _DIFFUSE_TEXTURE_SOURCE_MPM_R
                albedo *= tex2D(_DiffuseTex, i.uv).r * _AlbedoMultiplier;
                #elifdef _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS
                albedo *= tex2D(_DiffuseTex, i.uv).a * _AlbedoMultiplier;
                #else
                albedo *= tex2D(_DiffuseTex, i.uv) * _AlbedoMultiplier;
                #endif
                #endif

                #ifdef ALPHA_CUTOUT
                if (albedo.a == 0) discard;
                #endif

                float3 worldPos = i.worldPos;
                float3 worldNormal = normalize(i.worldNormal);

                float3 calculated = 0;

                float metallic = _Metallic;
                float smoothness = _Smoothness;
                #ifdef METAL_SMOOTHNESS_TEXTURE
                #ifdef _METALLIC_TEXTURE_SOURCE_MPM_R
                metallic *= tex2D(_MetalSmoothnessTex, i.uv).r;
                #elif _METALLIC_TEXTURE_SOURCE_MPM_A
                metallic *= tex2D(_MetalSmoothnessTex, i.uv).a;
                #endif
                #ifdef _SMOOTHNESS_TEXTURE_SOURCE_MPM_A
                smoothness *= tex2D(_MetalSmoothnessTex, i.uv).a;
                #elif _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS
                smoothness *= tex2D(_MetalSmoothnessTex, i.uv).g;
                #endif
                #endif

                CUSTOM_LIGHTING_APPLY(calculated, albedo, metallic, smoothness, _SpecularIntensity, worldPos,
                                      worldNormal);
                albedo.rgb += calculated;

                #ifdef RIM_DIM
                float rim = 1 - saturate(dot(worldNormal, normalize(_WorldSpaceCameraPos - worldPos)));
                #ifdef INVERT_RIM_DIM
                rim = 1 - rim;
                #endif
                // float distFactor = (i.dist + _RimDistanceOffset) * _RimDistanceScale;
                float finalRim = saturate((rim + _RimOffset) * _RimScale);
                albedo *= (1 - finalRim * _RimDarkening);
                #endif

                #if USE_EMISSION_COLOR

                #if USE_EMISSION_TEXTURE
                float4 emissionTex = tex2D(_EmissionTex, i.uv);
                #if defined(METAL_SMOOTHNESS_TEXTURE) && defined(_EMISSION_TEXTURE_SOURCE_MPM_G)
                emissionTex.rgb *= tex2D(_MetalSmoothnessTex, i.uv).g;
                #endif

                #if defined(_EMISSION_ALPHA_SOURCE_EMISSION_G)
                emissionTex.a *= emissionTex.g;
                #elif defined(_EMISSION_ALPHA_SOURCE_COPY_EMISSION)
                emissionTex.a = emissionTex;
                #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_EMISSION_ALPHA_SOURCE_MPM_R)
                emissionTex.a *= tex2D(_MetalSmoothnessTex, i.uv).r;
                #endif

                #if USE_EMISSION_MASK

                #ifdef EMISSION_MASK
                float4 emissionMask = tex2D(_EmissionMask, i.uv) * _EmissionMaskIntensity;
                #ifdef _MASKBLEND_ADD
                emissionTex += emissionMask;
                #elifdef _MASKBLEND_MASKED_ADD
                emissionTex += emissionMask;
                #else
                emissionTex *= emissionMask;
                #endif
                #endif

                #endif

                albedo *= emissionTex * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor);

                #else
                albedo *= UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor);
                #endif

                #if _EMISSIONBLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(albedo, _EmissionTexBloomIntensity);
                #elif _EMISSIONBLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _EmissionTexWhiteBoostMultiplier);
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