Shader "ChroMapper/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        [KeywordEnum(None, Import, External Scale, Object Space, Additive Offset)] _Secondary_UVs ("Secondary UVs", float) = 0
        _UVScale ("UV Scale", Vector) = (1,1,1,1)
        _AdditiveUVOffset ("UV Offset", Vector) = (0,0,0,0)
        _InputUvMultiplier ("UV Multiplier", Vector) = (1,1,0,0)



        [Header(Texture)] [Space]
        [Toggle(METAL_SMOOTHNESS_TEXTURE)] _EnableMetalSmoothnessTex ("Multi Purpose Map", float) = 0
        _MetalSmoothnessTex ("MPM Texture", 2D) = "white" {}
        [KeywordEnum(None, MPM R, MPM A)] _Metallic_Texture_Source ("Metallic Source", float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 1
        [KeywordEnum(None, MPM A, MPM G Roughness)] _Smoothness_Texture_Source ("Smoothness Source", float) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Toggle(PRECISE_NORMAL)] _PreciseNormal ("Precise Normal", float) = 0



        [Header(Vertex)] [Space]
        [KeywordEnum(None, Color, Emission, Metal Smoothness, Special, Displacement, Emissive Mult Add)] _Vertex ("Vertex Color Mode", float) = 0
        _EmissionThreshold ("Emission Threshold", Range(0, 1)) = 0
        _EmissionColor ("Emission Color", Color) = (1,1,1,0)
        _EmissionStrength ("Emission Strength", float) = 1
        _EmissionBloomIntensity ("Emission Bloom Intensity", float) = 1
        [KeywordEnum(None, PP, Frag)] _Vertex_BloomType ("Color Treatment", float) = 0
        [Space]
        [Toggle(DISPLACEMENT_SPATIAL)] _DisplacementSpatial ("RGB Direction", float) = 0
        [Toggle(DISPLACEMENT_BIDIRECTIONAL)] _DisplacementBidirectional ("RGB Bidirectional", float) = 0
        [KeywordEnum(None, Flat, Full)] _Spectrogram ("Spectrogram", float) = 0
        _DisplacementStrength ("Displacement Strength", float) = 0.1
        _DisplacementAxisMultiplier ("Axis Multiplier", Vector) = (1,1,1,1)


        [Header(Emission)] [Space]
        [KeywordEnum(None, Simple, Pulse, Flipbook)] _EmissionTexture ("Emission Texture", float) = 0
        [KeywordEnum(Texture, MPM G)] _Emission_Texture_Source ("Source", float) = 0
        _EmissionTex ("Texture", 2D) = "white" {}
        _EmissionTexSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [Toggle(SECONDARY_UVS_EMISSION)] _SecondaryUVsEmissionTex ("Use Secondary UVs", float) = 0
        [KeywordEnum(Emission G, Copy Emission, MPM R)] _Emission_Alpha_Source ("Alpha Source", float) = 0
        _EmissionBrightness ("Brightness", float) = 1
        [Toggle(EMISSION_ANGLE_DISAPPEAR)] _EnableEmissionAngleDisappear ("Angle Disappear", float) = 0
        _EmissionThresholdAngle ("Threshold Angle", float) = 0
        [KeywordEnum(Flat, Frag, Gradient, PP)] _EmissionBloomType ("Color Treatment", float) = 0
        _EmissionTexColor ("Color", Color) = (1,1,1,1)

        [Space(20)]
        _EmissionGradientTex ("Gradient LUT", 2D) = "white" {}
        _EmissionGradientPosition ("LUT Position", float) = 0.5
        _EmissionGradientPanningSpeed ("LUT Panning", float) = 0
        _EmissionGradientIntensity ("LUT Intensity", float) = 1

        [Space(20)]
        [Toggle(EMISSION_MASK)] _EnableEmissionMask ("Emission Mask", float) = 0
        [KeywordEnum(Multiply, Add, Masked Add)] _MaskBlend ("Blend", float) = 0
        _EmissionMask ("Texture", 2D) = "white" {}
        [Toggle(SECONDARY_UVS_EMISSION_MASK)] _SecondaryUVsMask ("Use Secondary UVs", float) = 0
        _EmissionMaskSpeed ("Texture Speed", Vector) = (0,1,0,0)
        _EmissionMaskIntensity ("Intensity", float) = 1

        [Space(20)]
        [Toggle(SECONDARY_EMISSION_MASK)] _EnableSecondaryEmissionMask ("Secondary Emission Mask", float) = 0
        [KeywordEnum(Multiply, Add, Masked Add)] _Secondary_MaskBlend ("Blend", float) = 0
        _SecondaryEmissionMask ("Texture", 2D) = "white" {}
        [Toggle(SECONDARY_UVS_EMISSION_MASK2)] _SecondaryUVsMask2 ("Use Secondary UVs", float) = 0
        _SecondaryEmissionMaskSpeed ("Texture Speed", Vector) = (0,1,0,0)
        _SecondaryEmissionMaskIntensity ("Intensity", float) = 1

        [Space(20)]
        _EmissionMaskStepValue ("Step Value", Range(0, 1)) = 0.5
        _EmissionMaskStepWidth ("Step Width", Range(0, 0.5)) = 0.1

        [Space(20)]
        _EmissionTexBloomIntensity ("Bloom Intensity", float) = 1
        _EmissionTexWhiteBoostMultiplier ("White Boost Multiplier", float) = 1

        [Space(20)]
        _FlipbookColumns ("Flipbook Columns", float) = 8
        _FlipbookRows ("Flipbook Rows", float) = 8
        _FlipbookNonloopableFrames ("Full Non-loopable frames", float) = 0
        _FlipbookSpeed ("Flipbook Speed", float) = 1
        [Toggle(FLIPBOOK_BLENDING_OFF)] _FlipbookBlendingOff ("No Frame Blending", float) = 0



        [Header(Lighting)] [Space]
        _AmbientMinimalValue ("Ambient Minimum", Range(0, 1)) = 0
        _NominalDiffuseLevel ("Ambient Color", Color) = (0, 0, 0, 0)
        _AmbientMultiplier ("Ambient Color Multiplier", float) = 1

        [Space(20)]
        [Toggle(PRIVATE_POINT_LIGHT)] _EnablePrivatePointLight ("Private Point Light", float) = 0
        _PrivatePointLightColor ("Color", Color) = (0,0.5,1,1)
        [Toggle(POINT_LIGHT_IS_LOCAL)] _PointLightPositionLocal ("Make Position Local", float) = 0
        _PrivatePointLightIntensity ("Intensity Multiplier", float) = 1
        _PrivatePointLightPosition ("Light World Position", Vector) = (0,0,0,1)

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
        _SpecularIntensity ("Intensity", float) = 1



        [Header(Reflection)] [Space]
        [Toggle(RIM_DIM)] _EnableRimDim ("Rim Dim", float) = 0
        [Toggle(INVERT_RIM_DIM)] _InvertRimDim ("Invert", float) = 0
        _RimScale ("Scale", float) = 1
        _RimOffset ("Offset", float) = 1
        _RimDistanceOffset ("Distance Offset", float) = 2
        _RimDistanceScale ("Distance Scale", float) = 0.3
        _RimSmoothness ("Smoothness", float) = 1
        _RimDarkening ("Darkening", float) = 0



        [Header(Occlusion)] [Space]
        [Toggle(GROUND_FADE)] _EnableGroundFade ("Height Occlusion", Float) = 0
        _GroundFadeScale ("Height Occlusion Scale", Float) = 0.5
        _GroundFadeOffset ("Height Occlusion Offset", Float) = 1



        [Header(Others)] [Space]
        [KeywordEnum(Standard, Song Time, Freeze)] _Custom_Time ("Time Behavior", float) = 0
        [KeywordEnum(After Emissive, Before Emissive)] _ACES_Approach ("ACES Approach", float) = 0
        [Toggle(COLOR_ARRAY)] _UseColorArray ("Color Array", float) = 0



        [Header(Fog Settings)] [Space]
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1
        _EmissionFogSuppression ("Emission Fog Suppression", Range(0, 1)) = 0
        _MainEffectFogSuppression ("Main Effect Fog Suppression", Range(0, 1)) = 0

        [Space(20)]
        [Toggle(DISTANCE_DARKENING)] _EnableDistanceDarkening ("Worldspace Occlusion", float) = 0
        _DarkeningScale ("Scale", float) = 0.35
        _DarkeningIntensity ("Intensity", float) = 1
        _DarkeningCenter ("Center", Vector) = (0,0,0,0)
        _DarkeningDirection ("Axes", Vector) = (1,1,1,1)



        [Header(Settings)] [Space]
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

            #pragma shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE _SECONDARY_UVS_OBJECT_SPACE _SECONDARY_UVS_ADDITIVE_OFFSET

            #pragma shader_feature_local_fragment METAL_SMOOTHNESS_TEXTURE
            #pragma shader_feature_local_fragment _ _METALLIC_TEXTURE_SOURCE_MPM_R _METALLIC_TEXTURE_SOURCE_MPM_A
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_SOURCE_MPM_A _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS
            #pragma shader_feature_local_fragment PRECISE_NORMAL

            #pragma shader_feature_local _ _VERTEX_COLOR _VERTEX_EMISSION _VERTEX_METAL_SMOOTHNESS _VERTEX_SPECIAL _VERTEX_DISPLACEMENT _VERTEX_EMISSIVE_MULT_ADD
            #pragma shader_feature_local_vertex _ _VERTEX_BLOOMTYPE_PP _VERTEX_BLOOMTYPE_FRAG

            #pragma shader_feature_local_vertex DISPLACEMENT_SPATIAL
            #pragma shader_feature_local_vertex DISPLACEMENT_BIDIRECTIONAL
            #pragma shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL

            #pragma shader_feature_local_fragment _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_FLIPBOOK
            #pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_MPM_G
            #pragma shader_feature_local_fragment SECONDARY_UVS_EMISSION

            #pragma shader_feature_local_fragment _ _EMISSIONBLOOMTYPE_FRAG _EMISSIONBLOOMTYPE_GRADIENT _EMISSIONBLOOMTYPE_PP
            #pragma shader_feature_local_fragment EMISSION_ANGLE_DISAPPEAR
            #pragma shader_feature_local_fragment _ _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R

            #pragma shader_feature_local_fragment EMISSION_MASK
            #pragma shader_feature_local_fragment _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD
            #pragma shader_feature_local_fragment SECONDARY_UVS_EMISSION_MASK

            #pragma shader_feature_local_fragment SECONDARY_EMISSION_MASK
            #pragma shader_feature_local_fragment _ _SECONDARY_MASKBLEND_ADD _SECONDARY_MASKBLEND_MASKED_ADD
            #pragma shader_feature_local_fragment SECONDARY_UVS_EMISSION_MASK2

            #pragma shader_feature_local_fragment FLIPBOOK_BLENDING_OFF

            #pragma shader_feature_local_fragment PRIVATE_POINT_LIGHT
            #pragma shader_feature_local_fragment POINT_LIGHT_IS_LOCAL

            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment BOTH_SIDES_DIFFUSE
            #pragma shader_feature_local_fragment LIGHT_FALLOFF
            #pragma shader_feature_local_fragment DIFFUSE_TEXTURE
            #pragma shader_feature_local_fragment _ _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS

            #pragma shader_feature_local_fragment SPECULAR

            #pragma shader_feature_local RIM_DIM
            #pragma shader_feature_local_fragment INVERT_RIM_DIM

            #pragma shader_feature_local_fragment GROUND_FADE

            #pragma shader_feature_local_fragment _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE
            #pragma shader_feature_local_fragment _ _ACES_APPROACH_BEFORE_EMISSIVE
            #pragma shader_feature_local_fragment COLOR_ARRAY

            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment DISTANCE_DARKENING
            
            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/CustomLighting.cginc"
            #include "CGIncludes/CustomTime.cginc"
            #include "CGIncludes/CustomTonemapping.cginc"

            #define USE_UV_SCALE defined(_SECONDARY_UVS_EXTERNAL_SCALE) || defined(_SECONDARY_UVS_OBJECT_SPACE)
            #define USE_SECONDARY_UV USE_UV_SCALE || defined(_SECONDARY_UVS_IMPORT) || defined(_SECONDARY_UVS_ADDITIVE_OFFSET)
            // USE_SECONDARY_UV
            // USE_UV_SCALE
            float4 _UVScale;
            // --
            // _SECONDARY_UVS_ADDITIVE_OFFSET
            float4 _AdditiveUVOffset;
            // --
            float2 _InputUvMultiplier;
            // --

            // METAL_SMOOTHNESS_TEXTURE
            sampler2D _MetalSmoothnessTex;
            float4 _MetalSmoothnessTex_ST;
            // --
            float _Smoothness;
            float _Metallic;

            #define USE_VERTEX_EMISSION defined(_VERTEX_EMISSION) || defined(_VERTEX_SPECIAL) || defined(_VERTEX_EMISSIVE_MULT_ADD)
            #define USE_VERTEX_COLOR USE_VERTEX_EMISSION || defined(_VERTEX_COLOR) || defined(_VERTEX_METAL_SMOOTHNESS) || defined(_VERTEX_DISPLACEMENT)
            // USE_VERTEX_EMISSION
            float _EmissionThreshold;
            float _EmissionStrength;
            float _EmissionBloomIntensity;
            // --

            #define ENABLE_EMISSION_TEXTURE defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK)
            #define USE_EMISSION_TEXTURE !defined(_EMISSION_TEXTURE_SOURCE_MPM_G) && (defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_FLIPBOOK))
            // USE_EMISSION_TEXTURE
            sampler2D _EmissionTex;
            float4 _EmissionTex_ST;
            // _EMISSIONTEXTURE_SIMPLE
            float2 _EmissionTexSpeed;
            // --
            // --

            // EMISSION_ANGLE_DISAPPEAR && ENABLE_EMISSION_TEXTURE
            float _EmissionThresholdAngle;
            // --

            #define USE_EMISSION_TEXTURE_COLOR !defined(_EMISSIONBLOOMTYPE_GRADIENT) && ENABLE_EMISSION_TEXTURE

            #define USE_EMISSION_GRADIENT_TEXTURE defined(_EMISSIONBLOOMTYPE_GRADIENT) && ENABLE_EMISSION_TEXTURE
            // USE_EMISSION_GRADIENT_TEXTURE
            sampler2D _EmissionGradientTex;
            float4 _EmissionGradientTex_ST;
            // --
            // _EMISSIONBLOOMTYPE_GRADIENT
            float _EmissionGradientPanningSpeed;
            float _EmissionGradientIntensity;
            // --

            // _EMISSIONTEXTURE_FLIPBOOK
            float _FlipbookColumns;
            float _FlipbookRows;
            float _FlipbookNonloopableFrames;
            float _FlipbookSpeed;
            // --

            float _EmissionTexBloomIntensity;
            float _EmissionTexWhiteBoostMultiplier;

            #define USE_EMISSION_MASK defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_SIMPLE)
            // USE_EMISSION_MASK
            // EMISSION_MASK
            sampler2D _EmissionMask;
            float4 _EmissionMask_ST;
            float2 _EmissionMaskSpeed;
            // --
            // SECONDARY_EMISSION_MASK
            sampler2D _SecondaryEmissionMask;
            float4 _SecondaryEmissionMask_ST;
            float2 _SecondaryEmissionMaskSpeed;
            // --
            float _EmissionMaskStepValue;
            float _EmissionMaskStepWidth;
            // --

            float _AmbientMinimalValue;
            float4 _NominalDiffuseLevel;
            float _AmbientMultiplier;

            // DIFFUSE_TEXTURE
            sampler2D _DiffuseTex;
            float4 _DiffuseTex_ST;
            float _AlbedoMultiplier;
            // --

            // DIFFUSE
            float _BothSidesDiffuseMultiplier;
            // --

            // SPECULAR
            float _SpecularIntensity;
            // --

            // RIM_DIM
            float _RimScale;
            float _RimOffset;
            float _RimDistanceOffset;
            float _RimDistanceScale;
            float _RimSmoothness;
            float _RimDarkening;
            // --

            // GROUND_FADE
            float _GroundFadeScale;
            float _GroundFadeOffset;
            // --

            #if !defined(UNITY_INSTANCING_ENABLED)
            #endif

            #define USE_FOG_SUPPRESSION defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK) || defined(_VERTEX_EMISSION) || defined(_VERTEX_SPECIAL)
            // BLOOM_FOG && FOG
            float _FogStartOffset;
            float _FogScale;
            // HEIGHT_FOG
            float _FogHeightOffset;
            float _FogHeightScale;
            // --
            // USE_FOG_SUPPRESSION
            float _EmissionFogSuppression;
            float _MainEffectFogSuppression;
            // --
            // --

            // DISTANCE_DARKENING
            float _DarkeningScale;
            float _DarkeningIntensity;
            float3 _DarkeningCenter;
            float3 _DarkeningDirection;
            // --

            #if defined(UNITY_INSTANCING_ENABLED)
            UNITY_INSTANCING_BUFFER_START (Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _EmissionBrightness)
            UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
            UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTexColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _EmissionGradientPosition)
            UNITY_DEFINE_INSTANCED_PROP(float, _EmissionMaskIntensity)
            UNITY_DEFINE_INSTANCED_PROP(float, _SecondaryEmissionMaskIntensity)
            UNITY_DEFINE_INSTANCED_PROP(float4, _PrivatePointLightColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _TimeOffset)
            UNITY_INSTANCING_BUFFER_END (Props)
            #else
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float4 _EmissionTexColor;
                float _EmissionBrightness;
                float _EmissionGradientPosition;
                float _EmissionMaskIntensity;
                float _SecondaryEmissionMaskIntensity;
                float4 _PrivatePointLightColor;
                float _TimeOffset;
            CBUFFER_END
            #endif

            #define USE_WORLD_NORMAL defined(DIFFUSE) || defined(SPECULAR) || defined(RIM_DIM)

            struct appdata
            {
                float4 vertex : POSITION;
                #if USE_VERTEX_COLOR
                float4 color : COLOR;
                #endif
                float2 uv1 : TEXCOORD0;
                #if USE_SECONDARY_UV
                float2 uv2 : TEXCOORD1;
                #endif
                #if defined(_SPECTROGRAM_FULL)
                float2 uv3 : TEXCOORD2;
                #endif
                #if USE_WORLD_NORMAL
                float3 normal : NORMAL;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if USE_VERTEX_COLOR
                float4 color : COLOR0;
                #endif
                #if USE_VERTEX_EMISSION
                float4 emission : COLOR1;
                #endif
                #if USE_SECONDARY_UV
                float4 uv : TEXCOORD0;
                #else
                float2 uv : TEXCOORD0;
                #endif
                #if defined(RIM_DIM)
                float4 worldPos : TEXCOORD1;
                #else
                float3 worldPos : TEXCOORD1;
                #endif
                float4 screenPos : TEXCOORD2;
                #if USE_WORLD_NORMAL
                float3 worldNormal : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID};

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                #if USE_VERTEX_COLOR
                // TODO: i dont think this does what i think it odes
                o.color = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                // TODO: wtf does this do
                #if USE_VERTEX_EMISSION
                o.emission = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
                o.emission *= max(0, o.color.g - _EmissionThreshold) * _EmissionStrength;
                #if defined(_VERTEX_BLOOMTYPE_PP)
                CUSTOM_BLOOM_PP_APPLY(o.emission, _EmissionBloomIntensity);
                #elif defined(_VERTEX_BLOOMTYPE_FRAG)
                CUSTOM_BLOOM_FRAG_APPLY(o.emission, _EmissionBloomIntensity);
                #else
                CUSTOM_BLOOM_NONE_APPLY(o.emission);
                #endif
                #if !defined(_VERTEX_SPECIAL)
                o.emission *= o.color.a;
                #endif
                #endif
                #endif

                o.uv.xy = i.uv1.xy;
                #if USE_SECONDARY_UV
                o.uv.zw = i.uv2.xy;
                #if USE_UV_SCALE
                o.uv.zw *= _UVScale.xy;
                #endif
                #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET)
                o.uv.zw += _AdditiveUVOffset.xy;
                #endif
                o.uv.zw *= _InputUvMultiplier.xy;
                #endif

                #if USE_WORLD_NORMAL
                #if defined(PRECISE_NORMAL)
                o.worldNormal = UnityObjectToWorldNormal(i.normal);
                #else
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                #endif
                #endif
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                #if defined(RIM_DIM)
                o.worldPos.w = distance(o.worldPos.xyz, _WorldSpaceCameraPos);
                #endif
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 time = GET_TIME(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset));

                #if USE_SECONDARY_UV
                float2 uv2 = i.uv.zw;
                #else
                float2 uv2 = i.uv.xy;
                #endif

                #if defined(_VERTEX_COLOR)
                float4 baseColor = i.color;
                #else
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #endif

                float4 albedo = baseColor;
                #if defined(DIFFUSE_TEXTURE)
                #if defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_R)
                albedo.rgb = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).r;
                #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS)
                albedo.rgb = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).a * _Smoothness;
                #else
                albedo.rgb = tex2D(_DiffuseTex, TRANSFORM_TEX(i.uv, _DiffuseTex));
                #endif
                albedo.rgb *= _AlbedoMultiplier;
                #endif

                #if USE_VERTEX_EMISSION
                albedo += i.emission;
                #endif

                float3 worldPos = i.worldPos;
                #if USE_WORLD_NORMAL
                #if defined(PRECISE_NORMAL)
                float3 worldNormal = normalize(i.worldNormal);
                #else
                float3 worldNormal = i.worldNormal;
                #endif
                #endif

                // LIGHTING
                #if defined(DIFFUSE) || defined(SPECULAR)
                float3 calculated = 0;
                #if defined(_VERTEX_SPECIAL) || defined(_VERTEX_METAL_SMOOTHNESS)
                float metallic = i.color.r;
                float smoothness = i.color.a;
                #else
                float metallic = _Metallic;
                float smoothness = _Smoothness;
                #endif
                #if defined(METAL_SMOOTHNESS_TEXTURE)
                #if defined(_METALLIC_TEXTURE_SOURCE_MPM_R)
                metallic = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).r;
                #elif defined(_METALLIC_TEXTURE_SOURCE_MPM_A)
                metallic = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).a;
                #endif
                #if defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_A)
                smoothness = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).a;
                #elif defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS)
                smoothness = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).g;
                #endif
                #endif

                #if defined(DIFFUSE) && defined(BOTH_SIDES_DIFFUSE)
                float diffuseBothSides = _BothSidesDiffuseMultiplier;
                #else
                float diffuseBothSides = 0;
                #endif
                #if defined(SPECULAR)
                float specIntensity = _SpecularIntensity;
                #else
                float specIntensity = 0;
                #endif
                CUSTOM_LIGHTING_APPLY(calculated, albedo, metallic, smoothness, specIntensity,
                                      diffuseBothSides, worldPos, worldNormal);
                albedo = max(_NominalDiffuseLevel * albedo, _AmbientMinimalValue) * _AmbientMultiplier + float4(
                    calculated.rgb, 0);
                #else
                albedo = max(_NominalDiffuseLevel * albedo, _AmbientMinimalValue) * _AmbientMultiplier;
                #endif

                // EMISSION
                #if defined(_ACES_APPROACH_BEFORE_EMISSIVE)
                ACES_TONE_MAPPING_APPLY(albedo);
                #endif

                #if USE_EMISSION_TEXTURE_COLOR

                #if USE_EMISSION_TEXTURE
                float2 emissionUv = i.uv;
                #if defined(_EMISSIONTEXTURE_FLIPBOOK)
                emissionUv.x /= _FlipbookColumns;
                emissionUv.y /= _FlipbookRows;
                float flipbookTime = time.y * _FlipbookSpeed;
                emissionUv += float2(floor(flipbookTime % _FlipbookColumns) / _FlipbookColumns,
                                     floor(flipbookTime / _FlipbookColumns) % _FlipbookRows /
                                     _FlipbookRows);
                #endif
                #if defined(_EMISSIONTEXTURE_SIMPLE)
                float4 emissionTex = tex2D(_EmissionTex,
                                           TRANSFORM_TEX(emissionUv, _EmissionTex) +
                                           _EmissionTexSpeed * time.yy);
                #else
                float4 emissionTex = tex2D(_EmissionTex, TRANSFORM_TEX(emissionUv, _EmissionTex));
                #endif
                #if defined(_EMISSIONTEXTURE_FLIPBOOK) && !defined(FLIPBOOK_BLENDING_OFF)
                // TODO: im not sure if it's next or previous
                float2 emissionUv2 = i.uv + float2(floor((flipbookTime + 1) % _FlipbookColumns) / _FlipbookColumns,
                                                   floor((flipbookTime + 1) /
                                                       _FlipbookColumns)
                                                   %
                                                   _FlipbookRows /
                                                   _FlipbookRows);
                emissionTex = lerp(emissionTex, tex2D(_EmissionTex, TRANSFORM_TEX(emissionUv2, _EmissionTex)),
                                   flipbookTime % 1);
                #endif
                #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_EMISSION_TEXTURE_SOURCE_MPM_G)
                float4 emissionTex = float4(tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).ggg,
                                            0);
                #endif

                #if defined(_EMISSION_ALPHA_SOURCE_COPY_EMISSION)
                emissionTex.a = emissionTex.a;
                #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_EMISSION_ALPHA_SOURCE_MPM_R)
                emissionTex.a = tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).r;
                #else
                emissionTex.a = emissionTex.g;
                #endif

                #if USE_EMISSION_MASK

                #if defined(EMISSION_MASK)
                #if defined(SECONDARY_UVS_EMISSION_MASK)
                float4 emissionMask = tex2D(_EmissionMask,
                                            TRANSFORM_TEX(uv2, _EmissionMask) +
                                            _EmissionMaskSpeed * time.yy);
                #else
                float4 emissionMask = tex2D(_EmissionMask,
                                            TRANSFORM_TEX(i.uv, _EmissionMask) +
                                            _EmissionMaskSpeed *
                                            time
                                            .yy);
                #endif
                emissionMask *= UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionMaskIntensity);

                // TODO: ok what are the difference between the 2
                #if defined(_MASKBLEND_ADD)
                emissionTex += emissionMask;
                #elif defined(_MASKBLEND_MASKED_ADD)
                emissionTex += emissionTex * emissionMask;
                #else
                emissionTex *= emissionMask;
                #endif
                #endif

                #if defined(SECONDARY_EMISSION_MASK)
                #if defined(SECONDARY_UVS_EMISSION_MASK2)
                float4 emissionMask2 = tex2D(_SecondaryEmissionMask,
                                             TRANSFORM_TEX(uv2, _SecondaryEmissionMask)
                                             +
                                             _SecondaryEmissionMaskSpeed *
                                             time.yy);
                #else
                float4 emissionMask2 = tex2D(_SecondaryEmissionMask,
                                             TRANSFORM_TEX(i.uv, _SecondaryEmissionMask) +
                                             _SecondaryEmissionMaskSpeed *
                                             time.yy);
                #endif
                emissionMask2 *= UNITY_ACCESS_INSTANCED_PROP(Props, _SecondaryEmissionMaskIntensity);

                #if defined(_SECONDARY_MASKBLEND_ADD)
                emissionTex += emissionMask2;
                #elif defined(_SECONDARY_MASKBLEND_MASKED_ADD)
                emissionTex += emissionTex * emissionMask2;
                #else
                emissionTex *= emissionMask2;
                #endif
                #endif

                #endif

                float4 finalEmission = emissionTex * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor) *
                    UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionBrightness);
                albedo += finalEmission;

                #if defined(_EMISSIONBLOOMTYPE_PP)
                CUSTOM_BLOOM_PP_APPLY(albedo, _EmissionTexBloomIntensity);
                #elif defined(_EMISSIONBLOOMTYPE_FRAG)
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _EmissionTexWhiteBoostMultiplier);
                #else
                CUSTOM_BLOOM_NONE_APPLY(albedo);
                #endif

                #elif USE_EMISSION_GRADIENT_TEXTURE
                float4 finalEmission = tex2D(_EmissionGradientTex,
                                             TRANSFORM_TEX(i.uv, _EmissionGradientTex) +
                                             UNITY_ACCESS_INSTANCED_PROP(
                                                 Props, _EmissionGradientPosition) *
                                             _EmissionGradientPanningSpeed * time
                                             .yy) * _EmissionGradientIntensity;
                albedo += finalEmission;

                #endif

                #if defined(RIM_DIM)
                float rim = 1 - saturate(dot(worldNormal, normalize(_WorldSpaceCameraPos - worldPos)));
                #if defined(INVERT_RIM_DIM)
                rim = 1 - rim;
                #endif
                float distFactor = (i.worldPos.w + _RimDistanceOffset) * _RimDistanceScale;
                float finalRim = saturate((rim + _RimOffset) * _RimScale) * distFactor;
                albedo *= (1 - finalRim * _RimDarkening);
                #endif

                #if defined(GROUND_FADE)
                albedo *= saturate((worldPos.y + _GroundFadeOffset) * _GroundFadeScale);
                #endif

                #if !defined(_ACES_APPROACH_BEFORE_EMISSIVE)
                ACES_TONE_MAPPING_APPLY(albedo);
                #endif

                #if defined(BLOOM_FOG) && defined(FOG)
                #if HEIGHT_FOG
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset,
                                       _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                #if defined(DISTANCE_DARKENING)
                float darkeningOffset = worldPos - _DarkeningCenter;
                float dist = max(0, dot(darkeningOffset, normalize(_DarkeningDirection)));
                float darkeningFactor = saturate(dist * _DarkeningScale) * _DarkeningIntensity;
                albedo.rgb = lerp(albedo.rgb, 0, darkeningFactor);
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}