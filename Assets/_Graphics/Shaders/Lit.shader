Shader "ChroMapper/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _BaseColorBoost ("Base Color Boost", Float) = 1.0
        _BaseColorBoostThreshold ("Base Color Boost Threshold", Float) = 0.1

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
        [Toggle(VERTEXDISPLACEMENT_MASK)] _EnableVertexDisplacementMask ("Vertex Displacement Mask", float) = 0
        [KeywordEnum(Texture, 3D Texture)] _VertexDisplacement_Mask_Source ("Mask Source", float) = 0
        _VertexDisplacementMask ("Mask Texture", 2D) = "white" {}
        _VertexDisplacementMaskSpeed ("Mask Texture Speed", Vector) = (0, 1, 0, 0)
        _VertexDisplacementMaskMultiplier ("Mask Multiplier", float) = 1
        _VertexDisplacementMaskOffset ("Mask Offset", float) = 0
        _VertexDisplacement3DTexture ("Noise Tex", 3D) = "white" {}
        _VertexDisplacement3DTexOffset ("Texture Offset", Vector) = (0, 0, 0, 0)
        _VertexDisplacement3DTexPanning ("Texture Panning", Vector) = (0, 0, 0, 0)
        _VertexDisplacement3DTexScale ("Texture Scale", float) = 5


        [Header(Emission)] [Space]
        [KeywordEnum(None, Simple, Pulse, Flipbook)] _EmissionTexture ("Emission Texture", float) = 0
        [KeywordEnum(Texture, MPM G, SDF)] _Emission_Texture_Source ("Source", float) = 0
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



        [Header(Parallax)] [Space]
        [KeywordEnum(None, Flexible, RGB)] _Parallax ("Parallax Emission", Float) = 0
        [Toggle(_PARALLAX_FLEXIBLE_REFLECTED)] _EnableReflectedDir ("Reflected Direction", Float) = 0
        [KeywordEnum(Planar, Warped)] _Parallax_Projection ("Parallax Projection", Float) = 0
        _ParallaxColor ("Parallax Color", Color) = (1, 1, 1, 1)
        _ParallaxMap ("Parallax Map", 2D) = "black" {}
        [Toggle(SECONDARY_UVS_PARALLAX)] _SecondaryUVsParallax ("Parallax Texture Secondary UVs", Float) = 0
        _ParallaxTexSpeed ("Parallax Speed", Vector) = (0, 0, 0, 0)
        _ParallaxIntensity ("Parallax Intensity", Float) = 1
        _ParallaxIntensity_Step ("Parallax Intensity Step", Float) = -0.25
        _Layers ("Layers", Range(2, 6)) = 3
        _StartOffset ("Start Offset", Float) = 1
        _OffsetStep ("Offset Step", Float) = 1
        [Toggle(PARALLAX_IRIDESCENCE)] _Parallax_Iridescence ("Iridescence", Float) = 0
        _IridescenceAxesMultiplier ("Axes Multiplier", Vector) = (1, 2, 3, 0)
        _IridescenceTiling ("Iridescence Tiling", Float) = 0.25
        _IridescenceColorInfluence ("Color Influence", Range(0, 1)) = 0
        [KeywordEnum(None, Texture, Vertex Color)] _Parallax_Masking ("Mask by", Float) = 0
        _ParallaxMaskingMap ("Parallax Mask", 2D) = "white" {}
        _ParallaxMaskSpeed ("Mask Speed", Vector) = (0, 0, 0, 0)
        _ParallaxMaskIntensity ("Mask Intensity", Range(0, 1)) = 1

        [Header(Distortion)] [Space]
        [Toggle(DISTORTION_SIMPLE)] _EnableDistortion ("Distortion", float) = 0
        _DistortionTex ("Distortion Texture", 2D) = "white" {}
        _DistortionStrength ("Strength", float) = 0.1
        _DistortionPanning ("Panning", Vector) = (1, 1, 0, 0)
        _DistortionAxes ("Axes", Vector) = (1, 1, 0, 0)

        [Header(Reflection)] [Space]
        [Toggle(MULTIPLY_REFLECTIONS)] _EnableMultiplyReflections ("Multiply Reflections", float) = 0
        [Toggle(REFLECTION_PROBE_BOX_PROJECTION)] _EnableBoxProjection ("Box Projection", float) = 0
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

        [Toggle(MESH_PACKING)] _MeshPacking ("Mesh Packed Instancing", Float) = 0
        _MeshPackingId ("Mesh Packing ID", float) = 0
        _SDFNoiseOffset ("Noise offset", Vector) = (0, 0, 0, 0)
        _SDFNoisePanning ("Noise panning", Vector) = (0, 0, 0, 0)
        _SDFNoiseIntensity ("Noise Intensity", Float) = 1
        _SDFNoiseScale ("Noise Scale", Float) = 5
        _SDFPointIntensity ("Color Intensity", Float) = 1
        _SDFNegativeIntensity ("Negative Intensity", Float) = 0.5
        _SDFNoiseTex ("Noise Tex", 3D) = "white" {}



        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 1

        [Header(Blending)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactor ("Foreground Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactor ("Background Factor", Float) = 0
        [Header(Bloom Blending)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactorA ("Foreground Factor", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactorA ("Background Factor", Float) = 0

        [Header(Stencil)] [Space]
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", Float) = 0
        _BaseColorBoost ("Base Color Boost", float) = 1
        _BaseColorBoostThreshold ("Base Color Boost Threshold", float) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Stencil
        {
            Ref [_StencilRefValue]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

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

            // Aliases for SimpleLit keyword names
            #pragma shader_feature_local _ _VERTEXMODE_DISPLACEMENT
            #pragma shader_feature_local_vertex _ _VERTEX_WHITEBOOSTTYPE_MAINEFFECT

            #pragma shader_feature_local_vertex DISPLACEMENT_SPATIAL
            #pragma shader_feature_local_vertex DISPLACEMENT_BIDIRECTIONAL
            #pragma shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL
            #pragma shader_feature_local_vertex MESH_PACKING
            #pragma shader_feature_local_vertex VERTEXDISPLACEMENT_MASK
            #pragma shader_feature_local_vertex _ _VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE

            #pragma shader_feature_local_fragment _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_FLIPBOOK
            #pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_MPM_G
            #pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_SDF
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

            #pragma shader_feature_local DIFFUSE
            #pragma shader_feature_local_fragment BOTH_SIDES_DIFFUSE
            #pragma shader_feature_local_fragment LIGHT_FALLOFF
            #pragma shader_feature_local_fragment DIFFUSE_TEXTURE
            #pragma shader_feature_local_fragment _ _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS

            #pragma shader_feature_local SPECULAR

            #pragma shader_feature_local RIM_DIM
            #pragma shader_feature_local_fragment INVERT_RIM_DIM

            #pragma shader_feature_local_fragment _ _PARALLAX_FLEXIBLE _PARALLAX_RGB
            #pragma shader_feature_local_fragment _PARALLAX_FLEXIBLE_REFLECTED
            #pragma shader_feature_local_fragment _ _PARALLAX_PROJECTION_WARPED
            #pragma shader_feature_local_fragment PARALLAX_IRIDESCENCE
            #pragma shader_feature_local_fragment SECONDARY_UVS_PARALLAX
            #pragma shader_feature_local_fragment _ _PARALLAX_MASKING_TEXTURE _PARALLAX_MASKING_VERTEX_COLOR

            #pragma shader_feature_local_fragment DISTORTION_SIMPLE
            #pragma shader_feature_local_fragment MULTIPLY_REFLECTIONS
            #pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION

            #pragma shader_feature_local_fragment GROUND_FADE

            #pragma shader_feature_local_fragment _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE
            #pragma shader_feature_local_fragment _ _ACES_APPROACH_BEFORE_EMISSIVE
            #pragma shader_feature_local COLOR_ARRAY

            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment DISTANCE_DARKENING

            #pragma shader_feature_local_fragment MESH_PACKING

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomBloom.hlsl"
            #include "ShaderLibrary/CustomLighting.hlsl"
            #include "ShaderLibrary/CustomTime.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"
            #include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"

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

            #define USE_EMISSION_TEXTURE_COLOR ENABLE_EMISSION_TEXTURE
            // USE_EMISSION_GRADIENT_TEXTURE removed — gradient is now handled inside USE_EMISSION_TEXTURE_COLOR
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
            float _BaseColorBoost;
            float _BaseColorBoostThreshold;

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

            // _VERTEX_DISPLACEMENT
            float _DisplacementStrength;
            float4 _DisplacementAxisMultiplier;
            // --

            // VERTEXDISPLACEMENT_MASK
            #if defined(VERTEXDISPLACEMENT_MASK)
            #if defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE)
            sampler3D _VertexDisplacement3DTexture;
            float3    _VertexDisplacement3DTexOffset;
            float3    _VertexDisplacement3DTexPanning;
            float     _VertexDisplacement3DTexScale;
            #else
            sampler2D _VertexDisplacementMask;
            float4    _VertexDisplacementMask_ST;
            float2    _VertexDisplacementMaskSpeed;
            #endif
            float     _VertexDisplacementMaskMultiplier;
            float     _VertexDisplacementMaskOffset;
            #endif
            // --

            // _SPECTROGRAM_FULL
            float4 _SpectrogramData[32]; // 128 floats packed as 32 Vector4s
            // --

            // RIM_DIM
            float _RimScale;
            float _RimOffset;
            float _RimDistanceOffset;
            float _RimDistanceScale;
            float _RimSmoothness;
            float _RimDarkening;
            // --

            // DISTORTION_SIMPLE
            sampler2D _DistortionTex;
            float4 _DistortionTex_ST;
            float _DistortionStrength;
            float2 _DistortionPanning;
            float2 _DistortionAxes;
            // --

            // PARALLAX_IRIDESCENCE
            sampler2D _ParallaxMap;
            float4 _ParallaxMap_ST;
            float2 _ParallaxTexSpeed;
            float4 _ParallaxColor;
            float _ParallaxIntensity;
            float _ParallaxIntensity_Step;
            float _StartOffset;
            float _OffsetStep;
            float _Layers;
            float _IridescenceTiling;
            float3 _IridescenceAxesMultiplier;
            float _IridescenceColorInfluence;
            // _EMISSION_TEXTURE_SOURCE_SDF
            #ifdef _EMISSION_TEXTURE_SOURCE_SDF
            float4    _SDFPointArray[3];
            float3    _SDFNoisePanning;
            float3    _SDFNoiseOffset;
            float     _SDFNoiseIntensity;
            float     _SDFNoiseScale;
            float     _SDFPointIntensity;
            float     _SDFNegativeIntensity;
            sampler3D _SDFNoiseTex;
            #endif
            // --

            // _PARALLAX_MASKING_TEXTURE
            sampler2D _ParallaxMaskingMap;
            float4 _ParallaxMaskingMap_ST;
            float2 _ParallaxMaskSpeed;
            float _ParallaxMaskIntensity;
            // --
            // --

            // GROUND_FADE
            float _GroundFadeScale;
            float _GroundFadeOffset;
            // --

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

            #if defined(COLOR_ARRAY)
            float4 _ColorsArray[150];
            float _ColorsArrayOffset;
            float _Intensity;
            float _AlphaMultiplier;
            #endif


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
            UNITY_DEFINE_INSTANCED_PROP(float, _MeshPackingId)
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
                float4 _SongTime;
                float _MeshPackingId;
            CBUFFER_END
            #endif

            #define USE_WORLD_NORMAL defined(DIFFUSE) || defined(SPECULAR) || defined(RIM_DIM) || defined(PARALLAX_IRIDESCENCE) || defined(_VERTEX_DISPLACEMENT) || defined(_VERTEXMODE_DISPLACEMENT)

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
                #if defined(MESH_PACKING)
                float2 packingUv : TEXCOORD3;
                #endif
                #if defined(COLOR_ARRAY)
                float2 colorArrayId : TEXCOORD4;
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
                #if defined(COLOR_ARRAY)
                float2 colorArrayId : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID};

            v2f vert(appdata i, uint id : SV_VertexID)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                #if defined(_VERTEX_DISPLACEMENT) || defined(_VERTEXMODE_DISPLACEMENT)
                {
                    float3 dispDir;
                    #if defined(DISPLACEMENT_SPATIAL)
                    // RGB channels → XYZ displacement direction
                    dispDir = i.color.xyz;
                    #   if defined(DISPLACEMENT_BIDIRECTIONAL)
                    dispDir = dispDir * 2.0 - 1.0;
                    #   endif
                    dispDir *= _DisplacementAxisMultiplier.xyz;
                    #else
                    // Default: displace along vertex normal, magnitude from blue channel
                    dispDir = i.normal * i.color.b;
                    #   if defined(DISPLACEMENT_BIDIRECTIONAL)
                    dispDir = dispDir * 2.0 - 1.0;
                    #   endif
                    dispDir *= _DisplacementAxisMultiplier.xyz;
                    #endif

                    float spectrogramScale = 1.0;
                    #if defined(_SPECTROGRAM_FULL)
                    // uv3.x (0-1) indexes across 128 frequency bins uploaded by SpectrogramPropertyRowAnimator
                    {
                        uint bin   = (uint)(i.uv3.x * 128.0);
                        uint v4idx = bin / 4;
                        uint comp  = bin % 4;
                        float4 entry = _SpectrogramData[v4idx];
                        spectrogramScale = comp == 0 ? entry.x :
                                           comp == 1 ? entry.y :
                                           comp == 2 ? entry.z : entry.w;
                    }
                    #endif

                    float _dispScale = _DisplacementStrength * spectrogramScale;

                    #if defined(VERTEXDISPLACEMENT_MASK)
                    {
                        #if defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE)
                        // 3D texture mask — matches decompiled SimpleLit exactly:
                        // sample world-space position scaled/panned/offset into the 3D tex,
                        // then multiply+offset the result to get a scalar mask.
                        {
                            float4 _timeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
                            float3 _dmCoord = _VertexDisplacement3DTexPanning * _timeOffset.xxx;
                            _dmCoord = _dmCoord * float3(0.1, 0.1, 0.1) + _VertexDisplacement3DTexOffset;
                            // world-space position of unmodified vertex
                            float3 _dmWorldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                            float4 _dmSampCoord = float4(_VertexDisplacement3DTexScale.xxx * _dmWorldPos + _dmCoord, 0.0);
                            float4 _dmSamp = tex3Dlod(_VertexDisplacement3DTexture, _dmSampCoord);
                            float3 _dmVal = _VertexDisplacementMaskMultiplier.xxx * _dmSamp.xyz
                                          + _VertexDisplacementMaskOffset.xxx;
                            _dispScale *= _dmVal.x;
                        }
                        #else
                        // 2D texture mask — matches SimpleLit VERTEXDISPLACEMENT_MASK path
                        {
                            #if defined(_CUSTOM_TIME_FREEZE)
                            float _dmTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) * 0.05;
                            #else
                            float _dmTime = (_Time.y + UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) * 0.05;
                            #endif
                            float2 _dmUv = i.uv1.xy * _VertexDisplacementMask_ST.xy + _VertexDisplacementMask_ST.zw;
                            float2 _dmPan = _VertexDisplacementMask_ST.xy * _VertexDisplacementMaskSpeed;
                            _dmUv = _dmPan * _dmTime.xx + _dmUv;
                            float4 _dmSamp = tex2Dlod(_VertexDisplacementMask, float4(_dmUv, 0, 0));
                            float3 _dmVal = _VertexDisplacementMaskMultiplier.xxx * _dmSamp.xyz
                                          + _VertexDisplacementMaskOffset.xxx;
                            _dispScale *= _dmVal.x;
                        }
                        #endif
                    }
                    #endif

                    i.vertex.xyz += _dispScale * dispDir;
                }
                #endif

                o.vertex = UnityObjectToClipPos(i.vertex);
                #if USE_VERTEX_COLOR
                    o.color = i.color;
                    // TODO: wtf does this do
                    #if USE_VERTEX_EMISSION
                        o.emission = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
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
                #if defined(MESH_PACKING)
                float meshPackingID = UNITY_ACCESS_INSTANCED_PROP(Props, _MeshPackingId);
                float packingCull = abs(i.packingUv.y - meshPackingID) > 0.1;
                o.vertex.xyz = packingCull ? float3(0.0, 0.0, 0.0) : o.vertex.xyz;
                #endif
                #if defined(COLOR_ARRAY)
                o.colorArrayId.x = i.colorArrayId.x;
                o.colorArrayId.y = i.colorArrayId.y + _ColorsArrayOffset;
                #endif
                

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

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #if defined(COLOR_ARRAY)
                float colorIndex = round(i.colorArrayId.x * 10.0 + i.colorArrayId.y);
                float4 arrayColor = _ColorsArray[colorIndex];
                baseColor.rgb = arrayColor.rgb * _Intensity;
                baseColor.a   = arrayColor.a * _AlphaMultiplier;
                #endif
                //float4 baseColor = float4(0, 0, 0, 1);
                #if defined(_VERTEX_COLOR)
                baseColor *= i.color;
                
                #endif

                // Always start from black — baseColor contributes only via diffuse/ambient,
                // matching SimpleLit's behaviour so objects are pitch dark without emission or lights.
                float4 albedo = float4(0, 0, 0, 0);
                #if defined(DIFFUSE_TEXTURE)
                #if defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_R)
                baseColor.rgb *= tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).r;
                #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS)
                baseColor.rgb *= tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).a * _Smoothness;
                #else
                baseColor.rgb *= tex2D(_DiffuseTex, TRANSFORM_TEX(i.uv, _DiffuseTex));
                #endif
                baseColor.rgb *= _AlbedoMultiplier;
                #endif

                #if USE_VERTEX_EMISSION
                {
                    float thresholdRange = 1.0 / max(0.0001, 1.0 - _EmissionThreshold);
                    float t = saturate((i.color.g - _EmissionThreshold) * thresholdRange);
                    float smoothT = t * t * (3.0 - 2.0 * t);
                    smoothT *= _EmissionStrength;

                    float4 emissionColor = float4(i.emission.rgb * smoothT, i.emission.a * smoothT);

                    #if defined(_VERTEX_BLOOMTYPE_PP) || defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT)
                    // MainEffect path: wbVal drives bloom alpha, whiteboost drives colour boost
                    {
                        float _wbVal = emissionColor.a * emissionColor.a;
                        albedo.a = _wbVal * 3.5 * _EmissionBloomIntensity;
                        float _wbMult = _wbVal * _EmissionTexWhiteBoostMultiplier;
                        float _boost = _wbMult * _wbMult * _BaseColorBoost - _BaseColorBoostThreshold;
                        emissionColor.rgb = saturate(emissionColor.rgb * emissionColor.a + _boost);
                    }
                #elif defined(_VERTEX_BLOOMTYPE_FRAG)
                    // Whiteboost path: same formula but wbVal squared again before boost
                    {
                        float _wbVal = emissionColor.a * emissionColor.a;
                        albedo.a = _wbVal * 3.5 * _EmissionBloomIntensity;
                        float _wbMult = _wbVal * _wbVal * _EmissionTexWhiteBoostMultiplier;
                        float _boost = _wbMult * _wbMult * _BaseColorBoost - _BaseColorBoostThreshold;
                        emissionColor.rgb = saturate(emissionColor.rgb * emissionColor.a + _boost);
                    }
                #else
                    emissionColor.rgb *= emissionColor.a;
                #endif

                    albedo.rgb += emissionColor.rgb;

                    float bloomAlpha = i.color.a * i.color.a * i.emission.a * _EmissionBloomIntensity;
                    albedo.a = bloomAlpha;
                }
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
                // Resolve metallic/smoothness (same as before)
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

                #if defined(DIFFUSE)
                {
                    #if defined(LIGHT_FALLOFF)
                        float3 _toL0 = worldPos - _DirectionalLightPositions[0].xyz;
                        float _falloff0 = 1.0 / (dot(_toL0, _toL0) / (_DirectionalLightRadii[0] * _DirectionalLightRadii[0]) * 25.0 + 1.0);
                        float3 _toL1 = worldPos - _DirectionalLightPositions[1].xyz;
                        float _falloff1 = 1.0 / (dot(_toL1, _toL1) / (_DirectionalLightRadii[1] * _DirectionalLightRadii[1]) * 25.0 + 1.0);
                        float3 _toL2 = worldPos - _DirectionalLightPositions[2].xyz;
                        float _falloff2 = 1.0 / (dot(_toL2, _toL2) / (_DirectionalLightRadii[2] * _DirectionalLightRadii[2]) * 25.0 + 1.0);
                        float3 _toL3 = worldPos - _DirectionalLightPositions[3].xyz;
                        float _falloff3 = 1.0 / (dot(_toL3, _toL3) / (_DirectionalLightRadii[3] * _DirectionalLightRadii[3]) * 25.0 + 1.0);
                    #else
                        float _falloff0 = 1.0, _falloff1 = 1.0, _falloff2 = 1.0, _falloff3 = 1.0;
                    #endif

                    float3 lightAccum = float3(0, 0, 0);
                    float NdotL;

                    NdotL = max(0.0, dot(worldNormal, _DirectionalLightDirections[0].xyz));
                    lightAccum += NdotL * _DirectionalLightColors[0].rgb * _falloff0;
                    NdotL = max(0.0, dot(worldNormal, _DirectionalLightDirections[1].xyz));
                    lightAccum += NdotL * _DirectionalLightColors[1].rgb * _falloff1;
                    NdotL = max(0.0, dot(worldNormal, _DirectionalLightDirections[2].xyz));
                    lightAccum += NdotL * _DirectionalLightColors[2].rgb * _falloff2;
                    NdotL = max(0.0, dot(worldNormal, _DirectionalLightDirections[3].xyz));
                    lightAccum += NdotL * _DirectionalLightColors[3].rgb * _falloff3;

                    #if defined(BOTH_SIDES_DIFFUSE)
                    float NdotL_back;
                    NdotL_back = max(0.0, dot(-worldNormal, _DirectionalLightDirections[0].xyz));
                    lightAccum += NdotL_back * _DirectionalLightColors[0].rgb * _falloff0 * _BothSidesDiffuseMultiplier;
                    NdotL_back = max(0.0, dot(-worldNormal, _DirectionalLightDirections[1].xyz));
                    lightAccum += NdotL_back * _DirectionalLightColors[1].rgb * _falloff1 * _BothSidesDiffuseMultiplier;
                    NdotL_back = max(0.0, dot(-worldNormal, _DirectionalLightDirections[2].xyz));
                    lightAccum += NdotL_back * _DirectionalLightColors[2].rgb * _falloff2 * _BothSidesDiffuseMultiplier;
                    NdotL_back = max(0.0, dot(-worldNormal, _DirectionalLightDirections[3].xyz));
                    lightAccum += NdotL_back * _DirectionalLightColors[3].rgb * _falloff3 * _BothSidesDiffuseMultiplier;
                    #endif

                    float3 diffuseAlbedo = (1.0 - metallic) * baseColor.rgb;
                    albedo.rgb += lightAccum * diffuseAlbedo;
                }
                #endif

                // Ambient — always applied. Matches SimpleLit: albedo += surfaceColor * ambientTerm
                {
                    float3 ambientTerm = max(_AmbientMultiplier * _NominalDiffuseLevel.rgb, _AmbientMinimalValue);
                    albedo.rgb += baseColor.rgb * ambientTerm;
                }

                // MULTIPLY_REFLECTIONS
                #if defined(MULTIPLY_REFLECTIONS)
                {
                    float3 reflDir = reflect(normalize(worldPos - _WorldSpaceCameraPos), worldNormal);
                    #if defined(REFLECTION_PROBE_BOX_PROJECTION)
                    reflDir = BoxProjectedCubemapDirection(reflDir, worldPos,
                              unity_SpecCube0_ProbePosition,
                              unity_SpecCube0_BoxMin,
                              unity_SpecCube0_BoxMax);
                    #endif
                    float4 reflSample = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir,
                                        (1.0 - smoothness) * UNITY_SPECCUBE_LOD_STEPS);
                    float3 reflColor = DecodeHDR(reflSample, unity_SpecCube0_HDR);
                    albedo.rgb *= 1.0 + reflColor * metallic;
                }
                #endif

                // SPECULAR — GGX lobe matching SimpleLit
                #if defined(SPECULAR)
                {
                    float3 _viewDir = normalize(worldPos - _WorldSpaceCameraPos);
                    float  _vDotN   = dot(_viewDir, worldNormal);
                    float3 _reflDir = worldNormal * (-2.0 * _vDotN) + _viewDir;

                    float _rough2  = smoothness * smoothness;
                    _rough2 = _rough2 * _rough2; // smoothness^4
                    float _lobScale = _rough2 * 500.0;

                    #define SPEC_LOBE(lightDir, lightColor, atten) \
                    { \
                        float3 _ld   = (lightDir).xyz - _reflDir; \
                        float  _ldSq = dot(_ld, _ld); \
                        float  _lobe = saturate(-_lobScale * _ldSq * 0.5 + 1.0); \
                        _lobe = _lobe * _lobe; _lobe = _lobe * _lobe; _lobe = _lobe * _lobe; \
                        _specAcc += _lobe * (atten) * _rough2 * (lightColor).xyz * 500.0; \
                    }

                    float3 _specAcc = float3(0, 0, 0);
                    #if defined(LIGHT_FALLOFF)
                        float3 _stL0 = worldPos - _DirectionalLightPositions[0].xyz;
                        float _sf0 = 1.0 / (dot(_stL0,_stL0)/(_DirectionalLightRadii[0]*_DirectionalLightRadii[0])*25.0+1.0);
                        float3 _stL1 = worldPos - _DirectionalLightPositions[1].xyz;
                        float _sf1 = 1.0 / (dot(_stL1,_stL1)/(_DirectionalLightRadii[1]*_DirectionalLightRadii[1])*25.0+1.0);
                        float3 _stL2 = worldPos - _DirectionalLightPositions[2].xyz;
                        float _sf2 = 1.0 / (dot(_stL2,_stL2)/(_DirectionalLightRadii[2]*_DirectionalLightRadii[2])*25.0+1.0);
                        float3 _stL3 = worldPos - _DirectionalLightPositions[3].xyz;
                        float _sf3 = 1.0 / (dot(_stL3,_stL3)/(_DirectionalLightRadii[3]*_DirectionalLightRadii[3])*25.0+1.0);
                    #else
                        float _sf0 = 1.0, _sf1 = 1.0, _sf2 = 1.0, _sf3 = 1.0;
                    #endif

                    SPEC_LOBE(_DirectionalLightDirections[0], _DirectionalLightColors[0], _sf0)
                    SPEC_LOBE(_DirectionalLightDirections[1], _DirectionalLightColors[1], _sf1)
                    SPEC_LOBE(_DirectionalLightDirections[2], _DirectionalLightColors[2], _sf2)
                    SPEC_LOBE(_DirectionalLightDirections[3], _DirectionalLightColors[3], _sf3)
                    #undef SPEC_LOBE

                    float3 _f0 = lerp(float3(0.04, 0.04, 0.04), baseColor.rgb, metallic);
                    albedo.rgb += _specAcc * _f0 * _SpecularIntensity;
                }
                #endif

                // EMISSION
                #if defined(_ACES_APPROACH_BEFORE_EMISSIVE)
                ACES_TONE_MAPPING_APPLY(albedo);
                #endif


                #if defined(_PARALLAX_FLEXIBLE) || defined(_PARALLAX_RGB)
                {
                    float2 baseUv = i.uv.xy * _InputUvMultiplier;
                    float4 timeVal = GET_TIME(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset));

                    float3 dirToCam = normalize(i.worldPos.xyz - _WorldSpaceCameraPos);

                    #if defined(_PARALLAX_FLEXIBLE_REFLECTED)
                    float3 iridDir = dirToCam - 2.0 * dot(dirToCam, worldNormal) * worldNormal;
                    #else
                    float3 iridDir = dirToCam;
                    #endif

                    #if defined(PARALLAX_IRIDESCENCE)
                    float iridDot = dot(iridDir, _IridescenceAxesMultiplier);
                    iridDot = frac(iridDot * _IridescenceTiling);
                    float3 hueShift = iridDot.xxx * 6.0 + float3(0.0, 4.0, 2.0);
                    hueShift = hueShift * (1.0 / 6.0);
                    hueShift = frac(hueShift);
                    hueShift = hueShift * 6.0 - 3.0;
                    hueShift = saturate(abs(hueShift) - 1.0);
                    float3 hueShiftSq = hueShift * hueShift;
                    hueShift = (-hueShift * 2.0 + 3.0) * hueShiftSq;
                    #else
                    float3 hueShift = float3(1.0, 1.0, 1.0);
                    #endif

                    // Parallax UV
                    #if defined(SECONDARY_UVS_PARALLAX) && USE_SECONDARY_UV
                    float2 parallaxUv = i.uv.zw * _ParallaxMap_ST.xy + _ParallaxMap_ST.zw;
                    #else
                    float2 parallaxUv = baseUv * _ParallaxMap_ST.xy + _ParallaxMap_ST.zw;
                    #endif
                    parallaxUv += timeVal.x * _ParallaxTexSpeed * _ParallaxMap_ST.xy;

                    // Parallax layer accumulation
                    float3 layerColor = float3(0.0, 0.0, 0.0);
                    for (float layer = 0.0; layer < _Layers; layer += 1.0)
                    {
                        float lf = floor(layer);
                        float offset = _OffsetStep * lf + _StartOffset;
                        float2 sampleUv = offset.xx * dirToCam.xy + parallaxUv;
                        float4 parallaxSample = tex2D(_ParallaxMap, sampleUv);

                        // Cycle iridescence color per layer
                        float3 layerIrid;
                        if      (lf <= 0.1) layerIrid = hueShift.xyz;
                        else if (lf <= 1.1) layerIrid = hueShift.zxy;
                        else if (lf <= 2.1) layerIrid = hueShift.yzx;
                        else if (lf <= 3.1) layerIrid = hueShift.xzy;
                        else                layerIrid = hueShift.yzx;

                        float intensity = (_ParallaxIntensity_Step * lf + _ParallaxIntensity) * parallaxSample.x;
                        layerColor += intensity * layerIrid;
                    }
                    #if defined(_PARALLAX_MASKING_VERTEX_COLOR)
                    layerColor *= i.color.g;
                    #elif defined(_PARALLAX_MASKING_TEXTURE)
                    float4 maskSample = tex2D(_ParallaxMaskingMap,
                        TRANSFORM_TEX(baseUv, _ParallaxMaskingMap) + _ParallaxMaskSpeed * timeVal.y);
                    layerColor = lerp(layerColor, layerColor * maskSample.r, _ParallaxMaskIntensity);
                    #endif
                    float grayLayer = (layerColor.r + layerColor.g + layerColor.b) * 0.5;
                    float3 blended = _IridescenceColorInfluence.xxx * (grayLayer.xxx * _ParallaxColor.rgb - layerColor) + layerColor;
                    albedo.rgb += blended * _ParallaxColor.rgb;
                }
                #endif

                #if USE_EMISSION_TEXTURE_COLOR
                {
                    float4 emissionTexColor = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor);
                    float _eb = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionBrightness);
                    float4 finalEmission = 0;

                    #if USE_EMISSION_TEXTURE
                    // --- UV setup (matches SimpleLit es0.xy / es0.z time) ---
                    float2 _esUv = i.uv.xy * _InputUvMultiplier;
                    float _esTime;
                    #if defined(DISTORTION_SIMPLE)
                    {
                        float2 distortScrollUv = _esUv * _DistortionTex_ST.xy
                                               + _DistortionTex_ST.zw
                                               + _DistortionPanning * time.y * 0.1;
                        float2 distortSample = tex2D(_DistortionTex, distortScrollUv).xy;
                        _esUv += distortSample * (_DistortionStrength * 0.1) * _DistortionAxes;
                        _esTime = time.y;
                    }
                    #else
                    _esTime = time.y;
                    #endif

                    // --- Sample emission texture ---
                    float4 _esSample;
                    #if defined(_EMISSIONTEXTURE_FLIPBOOK)
                    {
                        float2 flipUv = _esUv;
                        flipUv.x /= _FlipbookColumns;
                        flipUv.y /= _FlipbookRows;
                        float flipbookTime = time.y * _FlipbookSpeed;
                        flipUv += float2(floor(flipbookTime % _FlipbookColumns) / _FlipbookColumns,
                                         floor(flipbookTime / _FlipbookColumns) % _FlipbookRows / _FlipbookRows);
                        _esSample = tex2D(_EmissionTex, TRANSFORM_TEX(flipUv, _EmissionTex));
                        #if !defined(FLIPBOOK_BLENDING_OFF)
                        float2 flipUv2 = _esUv;
                        flipUv2.x /= _FlipbookColumns;
                        flipUv2.y /= _FlipbookRows;
                        flipUv2 += float2(floor((flipbookTime + 1) % _FlipbookColumns) / _FlipbookColumns,
                                          floor((flipbookTime + 1) / _FlipbookColumns) % _FlipbookRows / _FlipbookRows);
                        _esSample = lerp(_esSample, tex2D(_EmissionTex, TRANSFORM_TEX(flipUv2, _EmissionTex)), flipbookTime % 1);
                        #endif
                    }
                    #elif defined(_EMISSIONTEXTURE_SIMPLE)
                    {
                        float2 _esTiled = _esUv * _EmissionTex_ST.xy + _EmissionTex_ST.zw;
                        _esTiled += _esTime.xx * _EmissionTexSpeed * _EmissionTex_ST.xy;
                        #if defined(_EMISSION_TEXTURE_SOURCE_SDF)
                        // -----------------------------------------------------------
                        // SDF emission: evaluate 3 signed-distance point contributions
                        // plus a 3D noise modulation. Matches SimpleLit exactly.
                        // -----------------------------------------------------------
                        {
                            // Time scalars: _TimeOffset * float2(0.05, 1.0)
                            // (freeze path — no _Time.y added, matching SimpleLit)
                            float4 _sdfTimeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
                            float2 _sdfT = _sdfTimeOffset.xx * float2(0.05, 1.0);

                            float _sdfAcc  = 0.0;
                            float _sdfMask = 1.0;

                            for (int _si = 0; _si < 3; _si++)
                            {
                                float3 _d  = worldPos - _SDFPointArray[_si].xyz;
                                float  _r  = dot(_d, _d);
                                _r = log(_r) * 0.25;
                                _r = pow(2.0, _r);

                                float _isPos  = _SDFPointArray[_si].w > 0.0;
                                float _isNeg  = _SDFPointArray[_si].w < 0.0;
                                // sign: -1 if negative, +1 if positive, 0 if zero
                                float _sign   = floor(_isNeg - _isPos);
                                float _intens = (_sign < 0.0) ? _SDFNegativeIntensity : _SDFPointIntensity;
                                float _contrib = max(abs(_SDFPointArray[_si].w) - _r, 0.0) * _intens;

                                _sdfAcc  += _contrib;
                                // accumulate occlusion mask only for negative points
                                float _occ = saturate(-_contrib * _intens + 1.0);
                                float _useOcc = (_sign >= 0.0) ? 1.0 : _occ;
                                _sdfMask *= _useOcc;
                            }

                            // 3D noise modulation
                            float3 _nCoord = _SDFNoisePanning.xyz * _sdfT.yyy + _SDFNoiseOffset.xyz;
                            _nCoord = _SDFNoiseScale.xxx * worldPos + _nCoord;
                            float4 _nSamp  = tex3D(_SDFNoiseTex, _nCoord);
                            float  _nVal   = _nSamp.y * _SDFNoiseIntensity;

                            // Final value feeds r and g equally
                            float _sdfVal  = _sdfMask * _sdfAcc + _nVal;
                            _esSample = float4(_sdfVal, _sdfVal, 0.0, 1.0);
                        }
                        #else
                        _esSample = tex2D(_EmissionTex, _esTiled);
                        #endif
                    }
                    #else
                    _esSample = tex2D(_EmissionTex, TRANSFORM_TEX(_esUv, _EmissionTex));
                    #endif

                    // --- _esBright: x = primary (r*brightness), y = whiteboost/mask (g) ---
                    // Matches SimpleLit line 1296-1297
                    float2 _esBright;
                    _esBright.x = _esSample.r * _eb;
                    _esBright.y = _esSample.g;

                    // --- Layer 2 mask (EMISSION_MASK) ---
                    // Matches SimpleLit: per-channel lerp-toward-1, applied to _esBright
                    #if defined(EMISSION_MASK)
                    {
                        #if defined(SECONDARY_UVS_EMISSION_MASK) && USE_SECONDARY_UV
                        float2 _mUv = uv2 * _EmissionMask_ST.xy + _EmissionMask_ST.zw;
                        #else
                        float2 _mUv = _esUv * _EmissionMask_ST.xy + _EmissionMask_ST.zw;
                        #endif
                        _mUv += _esTime.xx * _EmissionMaskSpeed * _EmissionMask_ST.xy;
                        float4 _mSamp = tex2D(_EmissionMask, _mUv);

                        float _mInt = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionMaskIntensity);
                        float _mInv = 1.0 - _mInt;

                        #if defined(_MASKBLEND_ADD)
                        _esBright.x += _mSamp.r;
                        _esBright.y += _mSamp.g;
                        #elif defined(_MASKBLEND_MASKED_ADD)
                        _esBright.x += _esBright.x * _mSamp.r;
                        _esBright.y += _esBright.y * _mSamp.g;
                        #else
                        // Multiply blend: lerp mask toward 1 by intensity, then multiply
                        _esBright.x *= _mSamp.r * _mInt + _mInv;
                        _esBright.y *= _mSamp.g * _mInt + _mInv;
                        #endif
                    }
                    #endif

                    // --- Layer 3 mask (SECONDARY_EMISSION_MASK) ---
                    // SimpleLit has no code for this (UI-only), so we mirror same logic as layer 2
                    #if defined(SECONDARY_EMISSION_MASK)
                    {
                        #if defined(SECONDARY_UVS_EMISSION_MASK2) && USE_SECONDARY_UV
                        float2 _m2Uv = uv2 * _SecondaryEmissionMask_ST.xy + _SecondaryEmissionMask_ST.zw;
                        #else
                        float2 _m2Uv = _esUv * _SecondaryEmissionMask_ST.xy + _SecondaryEmissionMask_ST.zw;
                        #endif
                        _m2Uv += _esTime.xx * _SecondaryEmissionMaskSpeed * _SecondaryEmissionMask_ST.xy;
                        float4 _m2Samp = tex2D(_SecondaryEmissionMask, _m2Uv);

                        float _m2Int = UNITY_ACCESS_INSTANCED_PROP(Props, _SecondaryEmissionMaskIntensity);
                        float _m2Inv = 1.0 - _m2Int;

                        #if defined(_SECONDARY_MASKBLEND_ADD)
                        _esBright.x += _m2Samp.r;
                        _esBright.y += _m2Samp.g;
                        #elif defined(_SECONDARY_MASKBLEND_MASKED_ADD)
                        _esBright.x += _esBright.x * _m2Samp.r;
                        _esBright.y += _esBright.y * _m2Samp.g;
                        #else
                        _esBright.x *= _m2Samp.r * _m2Int + _m2Inv;
                        _esBright.y *= _m2Samp.g * _m2Int + _m2Inv;
                        #endif
                    }
                    #endif

                    // --- Apply brightness second time (matches SimpleLit line 1325) ---
                    _esBright.xy *= _eb;

                    // --- Angle disappear ---
                    #if defined(EMISSION_ANGLE_DISAPPEAR) && defined(ENABLE_EMISSION_TEXTURE)
                    {
                        // angle disappear factor (reuse existing logic if available)
                    }
                    #endif

                    // --- Colour treatment ---
                    #if defined(_EMISSIONBLOOMTYPE_PP)
                    // MainEffect path — matches SimpleLit _EMISSIONCOLORTYPE_MAINEFFECT
                    {
                        float2 _esBrightME = _esBright.xy * _eb;
                        float3 _emitRGB = _esBrightME.x * emissionTexColor.rgb;
                        float _wbVal = _esBrightME.y * _esBrightME.y * emissionTexColor.a;
                        finalEmission.a = _wbVal * 3.5 * _EmissionTexBloomIntensity;
                        float _wbMult = _wbVal * _EmissionTexWhiteBoostMultiplier;
                        float _boost = _wbMult * _wbMult * _BaseColorBoost - _BaseColorBoostThreshold;
                        finalEmission.rgb = saturate(_emitRGB * emissionTexColor.a);
                    }
                    #elif defined(_EMISSIONBLOOMTYPE_FRAG)
                    // Whiteboost path — matches SimpleLit _EMISSIONCOLORTYPE_WHITEBOOST
                    {
                        float3 _emitRGB = _esBright.xxx * emissionTexColor.xyz;
                        float _wbVal = _esBright.y * _esBright.y * emissionTexColor.a;
                        finalEmission.a = _wbVal * 3.5 * _EmissionTexBloomIntensity;
                        float _wbMult = _wbVal * _wbVal * _EmissionTexWhiteBoostMultiplier;
                        float _boost = _wbMult * _wbMult * _BaseColorBoost - _BaseColorBoostThreshold;
                        finalEmission.rgb = saturate(_emitRGB * emissionTexColor.www + _boost);
                    }
                    #elif defined(_EMISSIONBLOOMTYPE_GRADIENT)
                    // Gradient path — matches SimpleLit _EMISSIONCOLORTYPE_GRADIENT
                    {
                        float _gradZ = _EmissionGradientPanningSpeed * _esTime + UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionGradientPosition);
                        float2 _gradUv = float2(_esSample.g, frac(_gradZ)) * _EmissionGradientTex_ST.xy;
                        float4 _grad = tex2D(_EmissionGradientTex, _gradUv);
                        finalEmission.rgb = saturate(_esBright.xxx * _grad.xyz) * _EmissionGradientIntensity * emissionTexColor.a;
                        finalEmission.a = _EmissionTexBloomIntensity * _EmissionGradientIntensity;
                    }
                    #else
                    // Flat path — matches SimpleLit default (no whiteboost, no gradient)
                    {
                        finalEmission.rgb = _esBright.xxx * emissionTexColor.xyz * emissionTexColor.www;
                        finalEmission.a = _esBright.x * _esBright.x * _EmissionTexBloomIntensity;
                    }
                    #endif

                    #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_EMISSION_TEXTURE_SOURCE_MPM_G)
                    // MPM G source — no _esBright path, direct sample
                    {
                        float4 _mpmSamp = float4(tex2D(_MetalSmoothnessTex, TRANSFORM_TEX(i.uv, _MetalSmoothnessTex)).ggg, 0);
                        float esBrightX = _mpmSamp.r * _eb * _eb;
                        finalEmission.rgb = esBrightX * emissionTexColor.rgb * emissionTexColor.a;
                        finalEmission.a = esBrightX * esBrightX * _EmissionTexBloomIntensity;
                    }
                    #endif // USE_EMISSION_TEXTURE

                    albedo += finalEmission;
                }
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
                //float dist = max(0, dot(darkeningOffset, normalize(_DarkeningDirection)));
                float dist = length(darkeningOffset);
                float darkeningFactor = saturate(dist * _DarkeningScale) * _DarkeningIntensity;
                albedo.rgb = lerp(albedo.rgb, 0, darkeningFactor);
                #endif
                //#if defined(UNITY_INSTANCING_ENABLED)
                //return float4(0, 1, 0, 1); // green = instancing active
                //#else
                //return float4(1, 0, 0, 1); // red = falling back to CBUFFER
                //#endif


                return albedo;
            }
            ENDHLSL
        }
    }
}