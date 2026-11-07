#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#pragma multi_compile _ STEREO_INSTANCING_ON

#pragma shader_feature_local _ _SECONDARY_UVS_IMPORT _SECONDARY_UVS_EXTERNAL_SCALE _SECONDARY_UVS_OBJECT_SPACE _SECONDARY_UVS_ADDITIVE_OFFSET

#pragma shader_feature_local_fragment METAL_SMOOTHNESS_TEXTURE
#pragma shader_feature_local_fragment _ _METALLIC_TEXTURE_SOURCE_MPM_R _METALLIC_TEXTURE_SOURCE_MPM_A
#pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_SOURCE_MPM_A _SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS
#pragma shader_feature_local PRECISE_NORMAL

#pragma shader_feature_local _ _VERTEXMODE_COLOR _VERTEXMODE_EMISSION \
    _VERTEXMODE_METALSMOOTHNESS _VERTEXMODE_SPECIAL _VERTEXMODE_DISPLACEMENT \
    _VERTEXMODE_EMISSIVE_MULT_ADD
#pragma shader_feature_local _ _VERTEX_WHITEBOOSTTYPE_MAINEFFECT \
    _VERTEX_WHITEBOOSTTYPE_ALWAYS

#pragma shader_feature_local_vertex DISPLACEMENT_SPATIAL
#pragma shader_feature_local_vertex DISPLACEMENT_BIDIRECTIONAL
#pragma shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL
#pragma shader_feature_local MESH_PACKING
#pragma shader_feature_local_vertex VERTEXDISPLACEMENT_MASK
#pragma shader_feature_local_vertex _ _VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE \
    _VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE

#pragma shader_feature_local _ _EMISSIONTEXTURE_SIMPLE _EMISSIONTEXTURE_PULSE \
    _EMISSIONTEXTURE_FLIPBOOK
#pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_MPM_G
#pragma shader_feature_local_fragment _ _EMISSION_TEXTURE_SOURCE_SDF
#pragma shader_feature_local SECONDARY_UVS_EMISSION
#pragma shader_feature_local SECONDARY_UVS_PULSE
#pragma shader_feature_local_fragment INVERT_PULSE
#pragma shader_feature_local_fragment PULSE_MULTIPLY_TEXTURE
#pragma shader_feature_local_fragment _ _EMISSION_ALPHA_SOURCE_COPY_EMISSION _EMISSION_ALPHA_SOURCE_MPM_R

#pragma shader_feature_local_fragment EMISSION_MASK
#pragma shader_feature_local_fragment _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD
#pragma shader_feature_local SECONDARY_UVS_EMISSION_MASK

#pragma shader_feature_local_fragment SECONDARY_EMISSION_MASK
#pragma shader_feature_local_fragment _ _SECONDARY_MASK_BLEND_ADD _SECONDARY_MASK_BLEND_MASKED_ADD
#pragma shader_feature_local SECONDARY_UVS_EMISSION_MASK2

#pragma shader_feature_local_fragment FLIPBOOK_BLENDING_OFF

#pragma shader_feature_local PRIVATE_POINT_LIGHT
#pragma shader_feature_local_fragment POINT_LIGHT_IS_LOCAL

#pragma shader_feature_local DIFFUSE
#pragma shader_feature_local_fragment BOTH_SIDES_DIFFUSE
#pragma shader_feature_local_fragment LIGHT_FALLOFF
#pragma shader_feature_local_fragment DIFFUSE_TEXTURE
#pragma shader_feature_local_fragment _DIFFUSE_TEXTURE_SOURCE_TEXTURE _DIFFUSE_TEXTURE_SOURCE_MPM_R _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS

#pragma shader_feature_local SPECULAR

#pragma shader_feature_local INVERT_RIM_DIM

#pragma shader_feature_local_fragment _ _PARALLAX_FLEXIBLE _PARALLAX_RGB
#pragma shader_feature_local _PARALLAX_FLEXIBLE_REFLECTED
#pragma shader_feature_local_fragment _ _PARALLAX_PROJECTION_WARPED
#pragma shader_feature_local PARALLAX_IRIDESCENCE
#pragma shader_feature_local SECONDARY_UVS_PARALLAX
#pragma shader_feature_local_fragment _ _PARALLAX_MASKING_TEXTURE _PARALLAX_MASKING_VERTEX_COLOR

#pragma shader_feature_local_fragment DISTORTION_SIMPLE
#pragma shader_feature_local NOISE_DITHERING
#pragma shader_feature_local_fragment MULTIPLY_REFLECTIONS
#pragma shader_feature_local REFLECTION_TEXTURE
#pragma shader_feature_local REFLECTION_PROBE
#pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION
#pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET

#pragma shader_feature_local_fragment GROUND_FADE

#pragma shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE
#pragma shader_feature_local_fragment _ _ACES_APPROACH_BEFORE_EMISSIVE
#pragma multi_compile_fragment _ ACES_TONE_MAPPING
#pragma shader_feature_local COLOR_ARRAY
#pragma shader_feature_local UV_COLOR_SEGMENTS
#pragma shader_feature_local HIGHLIGHT_SELECTION
#pragma shader_feature_local _ _HOLOGRAM_GRID _HOLOGRAM_SCANLINE _HOLOGRAM_LEGACY

#pragma shader_feature_local_fragment FOG
#pragma shader_feature_local_fragment HEIGHT_FOG
#pragma shader_feature_local_fragment HEIGHT_FOG_DEPTH_SOFTEN
#pragma shader_feature_local LIGHTMAP
#pragma shader_feature_local_fragment OCCLUSION
#pragma shader_feature_local_fragment DISTANCE_DARKENING
#pragma shader_feature_local_fragment DISSOLVE
#pragma shader_feature_local_fragment DISSOLVE_PROGRESS
#pragma shader_feature_local_fragment DISSOLVE_COLOR

// Selectors dropped or merged by the generated material UI are declared
// independently so feature-signature collisions remain distinct.
#pragma shader_feature_local COLOR_BY_FOG
#pragma shader_feature_local DIRECTIONAL_RIM
#pragma shader_feature_local DISSOLVE_TEXTURE
#pragma shader_feature_local EMISSION_ANGLE_DISAPPEAR
#pragma shader_feature_local RIM_DIM
#pragma shader_feature_local FOG_COLOR_HIGHLIGHT
#pragma shader_feature_local INSTANCED_PRIVATE_POINT_LIGHT
#pragma shader_feature_local NORMAL_MAP
#pragma shader_feature_local OCCLUSION_BEFORE_EMISSION
#pragma shader_feature_local OCCLUSION_DETAIL
#pragma shader_feature_local REFLECTION_STATIC
#pragma shader_feature_local SECONDARY_UVS_MPM
#pragma shader_feature_local SECONDARY_UVS_OCCLUSION
#pragma shader_feature_local SECONDARY_UVS_OCCLUSION_DETAIL
#pragma shader_feature_local SPECULAR_ANTIFLICKER
#pragma shader_feature_local TEXTURE3D_EMISSION
#pragma shader_feature_local TEXTURE3D_LOOKUP
#pragma shader_feature_local USE_SPHERICAL_NORMAL_OFFSET
#pragma shader_feature_local _DISSOLVE_SPACE_WORLD_CENTERED
#pragma shader_feature_local _DISTORTION_TARGET_EMISSIONTEX
#pragma shader_feature_local _ _EMISSIONCOLORTYPE_GRADIENT \
    _EMISSIONCOLORTYPE_MAINEFFECT _EMISSIONCOLORTYPE_WHITEBOOST
#pragma shader_feature_local _ _METALLIC_TEXTURE_MPM_R
#pragma shader_feature_local _OCCLUSION_SOURCE_MPM_B
#pragma shader_feature_local _PROBE_CALCULATION_PRECISE
#pragma shader_feature_local _ _RIMLIGHT_LERP _RIMLIGHT_ADDITIVE
#pragma shader_feature_local _RIM_WHITEBOOSTTYPE_MAINEFFECT
#pragma shader_feature_local _ _SMOOTHNESS_TEXTURE_MPM_A \
    _SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS

#pragma multi_compile_fragment _ BLOOM_FOG
#pragma multi_compile_fragment _ POST_BLOOM
#pragma multi_compile_fragment _ OVERDRAW_VIEW


// Payload and feature macros
// Payload requirements use the canonical feature selectors.
#define USE_UV_SCALE (defined(_SECONDARY_UVS_EXTERNAL_SCALE) || defined(_SECONDARY_UVS_OBJECT_SPACE))
#define USE_SECONDARY_UV_SOURCE (USE_UV_SCALE || defined(_SECONDARY_UVS_IMPORT))
#define USE_SECONDARY_UV_CONSUMER (defined(SECONDARY_UVS_EMISSION) || \
    defined(SECONDARY_UVS_PULSE) || \
    defined(SECONDARY_UVS_EMISSION_MASK) || defined(SECONDARY_UVS_EMISSION_MASK2) || \
    defined(SECONDARY_UVS_PARALLAX) || defined(SECONDARY_UVS_MPM) || \
    defined(SECONDARY_UVS_OCCLUSION) || \
    defined(SECONDARY_UVS_OCCLUSION_DETAIL))
#define USE_SECONDARY_UV (USE_SECONDARY_UV_SOURCE && USE_SECONDARY_UV_CONSUMER)
#define USE_NOISE_SCREEN_POSITION defined(NOISE_DITHERING)
#define USE_NORMAL_MAP_PAYLOAD defined(NORMAL_MAP)
#define USE_ANTIFLICKER_NORMAL_PAYLOAD defined(SPECULAR_ANTIFLICKER)
#if defined(MESH_PACKING)
#if USE_SECONDARY_UV || defined(COLOR_ARRAY) || defined(LIGHTMAP)
#define USE_MESH_PACKING_UV1 0
#else
#define USE_MESH_PACKING_UV1 1
#endif
#else
#define USE_MESH_PACKING_UV1 0
#endif

#define USE_SPHERE_SDF_3D_VERTEX \
     defined(_VERTEXMODE_DISPLACEMENT) && defined(VERTEXDISPLACEMENT_MASK) && \
     defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE) && \
     defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT) && defined(_CUSTOM_TIME_FREEZE) && \
     !defined(_VERTEXMODE_COLOR) && !defined(_VERTEXMODE_EMISSION) && \
     !defined(_VERTEXMODE_METALSMOOTHNESS) && !defined(_VERTEXMODE_SPECIAL) && \
     !defined(_VERTEXMODE_EMISSIVE_MULT_ADD) && !defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) && \
     !defined(DISPLACEMENT_SPATIAL) && !defined(DISPLACEMENT_BIDIRECTIONAL) && \
     !defined(_SPECTROGRAM_FLAT) && !defined(_SPECTROGRAM_FULL) && !defined(MESH_PACKING) && \
     !defined(_CUSTOM_TIME_SONG_TIME)
#define USE_RIBBON_SPATIAL_MASK_VERTEX \
     defined(_VERTEXMODE_DISPLACEMENT) && defined(DISPLACEMENT_SPATIAL) && \
     defined(DISPLACEMENT_BIDIRECTIONAL) && defined(VERTEXDISPLACEMENT_MASK) && \
     defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT) && defined(_CUSTOM_TIME_FREEZE) && \
    !defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE) && \
      !defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) && \
     !defined(_CUSTOM_TIME_SONG_TIME) && !defined(MESH_PACKING) && \
     !defined(_SPECTROGRAM_FLAT) && !defined(_SPECTROGRAM_FULL)
#define USE_VERTEX_EMISSION (defined(_VERTEXMODE_EMISSION) || \
    defined(_VERTEXMODE_SPECIAL) || defined(_VERTEXMODE_EMISSIVE_MULT_ADD))
#define USE_VERTEX_COLOR (USE_VERTEX_EMISSION || defined(_VERTEXMODE_COLOR) || \
    defined(_VERTEXMODE_METALSMOOTHNESS) || defined(_VERTEXMODE_DISPLACEMENT))
#if defined(PRIVATE_POINT_LIGHT) && !defined(INSTANCED_PRIVATE_POINT_LIGHT)
#define USE_UNIFORM_PRIVATE_POINT_COLOR 1
#else
#define USE_UNIFORM_PRIVATE_POINT_COLOR 0
#endif
#define ENABLE_EMISSION_TEXTURE defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK)
#define USE_EMISSION_TEXTURE !defined(_EMISSION_TEXTURE_SOURCE_MPM_G) && (defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_FLIPBOOK))
#define USE_EMISSION_TEXTURE_COLOR ENABLE_EMISSION_TEXTURE
// Gradient color sampling is part of USE_EMISSION_TEXTURE_COLOR.
#define USE_EMISSION_MASK defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_SIMPLE)
#define USE_FOG_SUPPRESSION (defined(_EMISSIONTEXTURE_SIMPLE) || defined(_EMISSIONTEXTURE_PULSE) || defined(_EMISSIONTEXTURE_FLIPBOOK) || defined(_VERTEXMODE_EMISSION) || defined(_VERTEXMODE_SPECIAL))
#define USE_WORLD_NORMAL defined(DIFFUSE) || defined(SPECULAR) || \
    defined(PARALLAX_IRIDESCENCE) || defined(_PARALLAX_FLEXIBLE_REFLECTED) || \
    defined(PRIVATE_POINT_LIGHT) || \
    defined(REFLECTION_TEXTURE) || defined(REFLECTION_PROBE) || defined(REFLECTION_STATIC) || \
    defined(_VERTEXMODE_DISPLACEMENT) || \
    defined(USE_SPHERICAL_NORMAL_OFFSET) || \
    defined(EMISSION_ANGLE_DISAPPEAR) || \
    defined(RIM_DIM) || defined(UV_COLOR_SEGMENTS) || \
    defined(_RIMLIGHT_LERP) || defined(_RIMLIGHT_ADDITIVE) || \
    defined(TEXTURE3D_LOOKUP) || \
    USE_NORMAL_MAP_PAYLOAD || USE_ANTIFLICKER_NORMAL_PAYLOAD

// Uniform declarations
// USE_SECONDARY_UV
// USE_UV_SCALE
float4 _UVScale;
// --
float2 _InputUvMultiplier;
// --

// METAL_SMOOTHNESS_TEXTURE
sampler2D _MetalSmoothnessTex;
float4 _MetalSmoothnessTex_ST;
#if USE_NORMAL_MAP_PAYLOAD
sampler2D _NormalTex;
float4 _NormalTex_ST;
float _NormalScale;
#endif
// --
float _Smoothness;
float _Metallic;

samplerCUBE _ReflectionProbeTexture1;
samplerCUBE _ReflectionProbeTexture2;
#if defined(REFLECTION_TEXTURE)
samplerCUBE _EnvironmentReflectionCube;
float _ReflectionTexIntensity;
#endif
float4 _LightProbeLightBakeIdA;
float4 _LightProbeLightBakeIdB;
float4 _LightProbeLightBakeIdC;
float4 _LightProbeLightBakeIdD;
float4 _LightProbeLightBakeIdE;
float4 _LightProbeLightBakeIdF;
float3 _ReflectionProbePosition;
float3 _ReflectionProbeBoundsMin;
float3 _ReflectionProbeBoundsMax;
float _ReflectionProbeIntensity;
float _ReflectionProbeGrayscale;
float _ColoredMetalMultiplier;
float _WhiteOffset;
float _AntiflickerStrength;
float _AntiflickerDistanceScale;
float _AntiflickerDistanceOffset;
float3 _ReflectionProbeBoxProjectionSizeOffset;
float3 _ReflectionProbeBoxProjectionPositionOffset;

#if defined(NOISE_DITHERING)
sampler2D _GlobalBlueNoiseTex;
float2 _GlobalBlueNoiseParams;
float _GlobalRandomValue;
#endif

// USE_VERTEX_EMISSION
float _EmissionThreshold;
float _EmissionStrength;
float _EmissionBloomIntensity;
float _QuestWhiteboostMultiplier;
// --

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

sampler2D _EmissionGradientTex;
float4 _EmissionGradientTex_ST;
// --
// _EMISSIONBLOOMTYPE_GRADIENT
float _EmissionGradientPanningSpeed;
// --

sampler2D _PulseMask;
float4 _PulseMask_ST;
float _PulseWidth;
float _PulseSpeed;
float _PulseSmooth;

// _EMISSIONTEXTURE_FLIPBOOK
float _FlipbookColumns;
float _FlipbookRows;
float _FlipbookNonloopableFrames;
float _FlipbookSpeed;
// --

float _EmissionTexBloomIntensity;
float _EmissionTexWhiteBoostMultiplier;

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
float _AmbientMultiplier;
#if defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY)
float _HologramGridSize;
float _HologramScanDistance;
float _HoloIntensity;
#endif

// DIFFUSE_TEXTURE
sampler2D _DiffuseTex;
float4 _DiffuseTex_ST;
float _AlbedoMultiplier;
// --

sampler2D _DirtDetailTex;
float4 _DirtDetailTex_ST;
sampler2D _DirtTex;
float4 _DirtTex_ST;
float _OcclusionIntensity;
sampler2D _LightMap1;
sampler2D _LightMap2;
float3 _LightmapLightBakeIdA;
float3 _LightmapLightBakeIdB;
float3 _LightmapLightBakeIdC;
float3 _LightmapLightBakeIdD;
float3 _LightmapLightBakeIdE;
float3 _LightmapLightBakeIdF;

// DIFFUSE
float _BothSidesDiffuseMultiplier;
// --

// SPECULAR
float _SpecularIntensity;
// --

float3 _SphericalNormalOffsetCenter;
float _SphericalNormalOffsetIntensity;

// VERTEXDISPLACEMENT_MASK
#if defined(VERTEXDISPLACEMENT_MASK)
#if defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE)
sampler3D _VertexDisplacement3DTexture;
float3 _VertexDisplacement3DTexOffset;
float3 _VertexDisplacement3DTexPanning;
float _VertexDisplacement3DTexScale;
#elif !defined(_VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE)
sampler2D _VertexDisplacementMask;
float4 _VertexDisplacementMask_ST;
float2 _VertexDisplacementMaskSpeed;
#endif
float _VertexDisplacementMaskMode;
float _VertexDisplacementMaskMultiplier;
float _VertexDisplacementMaskOffset;

#endif
// --

#if defined(_SPECTROGRAM_FULL) && !defined(_SPECTROGRAM_FLAT)
float _SpectrogramData[64];
#endif

// RIM_DIM
float _RimScale;
float _RimOffset;
float _RimDistanceOffset;
float _RimDistanceScale;
float _RimSmoothness;
float _RimDarkening;
float _RimLightEdgeStart;
float _RimLightIntensity;
float _RimLightBloomIntensity;
float3 _RimPerpendicularAxis;
float _RimLightWhiteboostMultiplier;
float _ColorFogMultiplier;
float _ColorFogHighlightMultiplier;
float _ColorFogInfluence;
float _ColorFogMax;
// --

// DISTORTION_SIMPLE
sampler2D _DistortionTex;
float4 _DistortionTex_ST;
float2 _DistortionPanning;
float2 _DistortionAxes;
// --

// PARALLAX_IRIDESCENCE
sampler2D _ParallaxMap;
float4 _ParallaxMap_ST;
float2 _ParallaxTexSpeed;
float _ParallaxIntensity;
float _ParallaxIntensity_Step;
float _StartOffset;
float _OffsetStep;
float _Layers;
float _IridescenceTiling;
float3 _IridescenceAxesMultiplier;
// _EMISSION_TEXTURE_SOURCE_SDF
float4 _SDFPointArray[3];
float3 _SDFNoisePanning;
float3 _SDFNoiseOffset;
float _SDFPointIntensity;
float _SDFNegativeIntensity;
sampler3D _SDFNoiseTex;
#if defined(TEXTURE3D_LOOKUP)
sampler3D _LookupTex;
float3 _LookupGridSize;
float4 _LookupXDisplacementMapping;
float4 _LookupYDisplacementMapping;
float4 _LookupZDisplacementMapping;
float4 _LookupRadialDisplacementMapping;
float4 _LookupScaleMapping;
float4 _LookupRotationMapping;
float4 _LookupEmissiveMapping;
float _LookupXYZDisplacementScale;
float _LookupRadialDisplacementScale;
float _LookupMaxScale;
float _LookupRotationMultiplier;
float _LookupEmissiveModulationStrength;
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

// BLOOM_FOG && FOG
float _FogStartOffset;
float _FogScale;
// HEIGHT_FOG
float _FogHeightOffset;
float _FogHeightScale;
float _FogSoften;
float _FogSoftenOffset;
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

// DISSOLVE
#if defined(DISSOLVE) || defined(DISSOLVE_TEXTURE)
float3 _DissolveAxisVector;
float _DissolveOffset;
float _DissolveStartValue;
float _DissolveEndValue;
float _DissolveReverse;
float _CutColorFalloff;
float _CutColorBacksideFalloff;
float _DissolveColorIntensity;
sampler2D _DissolveTexture;
float4 _DissolveTexture_ST;
float2 _DissolveTextureSpeed;
float _DissolveTextureInfluence;
#endif
// --

// COLOR_ARRAY
float4 _ColorsArray[200];
#if defined(COLOR_ARRAY)
float _Intensity;
float _AlphaMultiplier;
#endif
#if defined(UV_COLOR_SEGMENTS)
float4 _UVColors[10];
float4 _UVRimColors[10];
#endif
#if defined(HIGHLIGHT_SELECTION)
int _SegmentToHighlight;
#endif


// Shared includes
#include "UnityCG.cginc"
#include "ShaderLibrary/Core/Data.hlsl"
#include "ShaderLibrary/Core/Camera.hlsl"
#include "ShaderLibrary/Common/Time.hlsl"
#include "ShaderLibrary/Common/Lighting.hlsl"
#include "ShaderLibrary/Common/Reflection.hlsl"
#include "ShaderLibrary/Common/Bloom.hlsl"
#include "ShaderLibrary/Common/PostProcess.hlsl"
#include "ShaderLibrary/Families/BloomFogComposition.hlsl"
#include "ShaderLibrary/Core/Tonemapping.hlsl"

// Instancing declarations
// Props supports per-renderer MPB overrides. UNITY_ACCESS_INSTANCED_PROP
// provides the direct-name fallback when instancing is off.
UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(float4, _NominalDiffuseLevel)
    UNITY_DEFINE_INSTANCED_PROP(float, _EmissionBrightness)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTexColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _EmissionGradientPosition)
    UNITY_DEFINE_INSTANCED_PROP(float, _EmissionMaskIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float, _SecondaryEmissionMaskIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float4, _InstancedSecondaryTiling)
    UNITY_DEFINE_INSTANCED_PROP(float4, _InstancedSecondaryOffset)
    #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET)
    UNITY_DEFINE_INSTANCED_PROP(float4, _AdditiveUVOffset)
    #endif
    #if !USE_UNIFORM_PRIVATE_POINT_COLOR
    UNITY_DEFINE_INSTANCED_PROP(float4, _PrivatePointLightColor)
    #endif
    UNITY_DEFINE_INSTANCED_PROP(float, _OcclusionDetailIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float, _TimeOffset)
    UNITY_DEFINE_INSTANCED_PROP(float, _MeshPackingId)
    #if defined(_EMISSIONTEXTURE_FLIPBOOK)
    UNITY_DEFINE_INSTANCED_PROP(float, _StartTime)
    #endif
    UNITY_DEFINE_INSTANCED_PROP(float4, _DisplacementAxisMultiplier)
    UNITY_DEFINE_INSTANCED_PROP(float, _DisplacementStrength)
    #if defined(_SPECTROGRAM_FLAT)
    UNITY_DEFINE_INSTANCED_PROP(float, _SpectrogramData)
    #endif
    UNITY_DEFINE_INSTANCED_PROP(float, _EmissionGradientIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float, _SDFNoiseIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float, _SDFNoiseScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float4, _ParallaxColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _IridescenceColorInfluence)
    UNITY_DEFINE_INSTANCED_PROP(float, _DissolveProgress)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DissolveColor)
    #if defined(_RIMLIGHT_LERP) || defined(_RIMLIGHT_ADDITIVE)
    UNITY_DEFINE_INSTANCED_PROP(float4, _RimLightColor)
    #endif
    #if defined(COLOR_ARRAY)
    UNITY_DEFINE_INSTANCED_PROP(float, _ColorsArrayOffset)
    #endif
    #if defined(TEXTURE3D_LOOKUP)
    UNITY_DEFINE_INSTANCED_PROP(float4, _LookupGridElementIndex)
    UNITY_DEFINE_INSTANCED_PROP(float4, _LookupGridObjectSpacePivot)
    #endif
    #if defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY)
    UNITY_DEFINE_INSTANCED_PROP(float4, _HologramColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _HologramFill)
    UNITY_DEFINE_INSTANCED_PROP(float, _HologramPhaseOffset)
    UNITY_DEFINE_INSTANCED_PROP(float, _HologramStripeSpeed)
    UNITY_DEFINE_INSTANCED_PROP(float, _HaltScan)
    #endif
UNITY_INSTANCING_BUFFER_END(Props)
#if USE_UNIFORM_PRIVATE_POINT_COLOR
float4 _PrivatePointLightColor;
#endif

// Lit-local UV and surface helpers
inline float2 TransformSecondaryUv(SurfaceData surface, float4 texture_ST)
{
    return surface.uv1 * texture_ST.xy * surface.secondaryUvTiling +
        texture_ST.zw + surface.secondaryUvOffset;
}

inline float2 TransformScrollingSecondaryUv(
    SurfaceData surface, float4 texture_ST, float2 speed, float time)
{
    float2 scale = texture_ST.xy * surface.secondaryUvTiling;
    return surface.uv1 * scale + texture_ST.zw + surface.secondaryUvOffset +
        time * speed * scale;
}

inline void ResolveSurfaceMaterial(
    inout SurfaceData surface, float4 vertexColor,
    float metallic, float smoothness, float2 inputUvMultiplier,
    sampler2D metalSmoothnessTex, float4 metalSmoothnessTex_ST,
    sampler2D dirtTex, float4 dirtTex_ST, float occlusionIntensity,
    sampler2D dirtDetailTex, float4 dirtDetailTex_ST,
    float occlusionDetailIntensity)
{
    #if defined(_VERTEXMODE_METALSMOOTHNESS)
    surface.metallic = vertexColor.r * metallic;
    surface.smoothness = vertexColor.a * smoothness;
    #elif defined(_VERTEXMODE_SPECIAL)
    surface.metallic = vertexColor.r;
    surface.smoothness = vertexColor.a;
    #endif

    #if defined(METAL_SMOOTHNESS_TEXTURE)
    #if defined(SECONDARY_UVS_MPM) && USE_SECONDARY_UV
    float2 mpmUv = TransformSecondaryUv(surface, metalSmoothnessTex_ST);
    #else
    float2 mpmBaseUv = surface.uv0 * inputUvMultiplier;
    float2 mpmUv = mpmBaseUv * metalSmoothnessTex_ST.xy + metalSmoothnessTex_ST.zw;
    #endif
    surface.mpm = tex2D(metalSmoothnessTex, mpmUv);

    // MPM selectors map R/A to metallic, A/G to smoothness, B to
    // occlusion, and G to emission; the roughness selector inverts G.
    #if defined(_METALLIC_TEXTURE_MPM_R)
    surface.metallic = surface.mpm.r * metallic;
    #elif defined(_METALLIC_TEXTURE_SOURCE_MPM_R)
    surface.metallic = surface.mpm.r;
    #elif defined(_METALLIC_TEXTURE_SOURCE_MPM_A)
    surface.metallic = surface.mpm.a;
    #endif

    #if defined(_SMOOTHNESS_TEXTURE_MPM_A)
    surface.smoothness = surface.mpm.a * smoothness;
    #elif defined(_SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS)
    surface.smoothness = (1.0 - surface.mpm.g) * smoothness;
    #elif defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_A)
    surface.smoothness = surface.mpm.a;
    #elif defined(_SMOOTHNESS_TEXTURE_SOURCE_MPM_G_ROUGHNESS)
    surface.smoothness = surface.mpm.g;
    #endif
    #endif

    #if defined(OCCLUSION)
    #if defined(_OCCLUSION_SOURCE_MPM_B) && defined(METAL_SMOOTHNESS_TEXTURE)
    float primaryOcclusionSample = surface.mpm.b;
    #else
    #if defined(SECONDARY_UVS_OCCLUSION) && USE_SECONDARY_UV
    float2 occlusionUv = TransformSecondaryUv(surface, dirtTex_ST);
    #else
    float2 occlusionBaseUv = surface.uv0 * inputUvMultiplier;
    float2 occlusionUv = occlusionBaseUv * dirtTex_ST.xy + dirtTex_ST.zw;
    #endif
    float primaryOcclusionSample = tex2D(dirtTex, occlusionUv).r;
    #endif
    surface.occlusion = occlusionIntensity * primaryOcclusionSample +
        (1.0 - occlusionIntensity);
    #endif

    #if defined(OCCLUSION_DETAIL)
    #if defined(SECONDARY_UVS_OCCLUSION_DETAIL) && USE_SECONDARY_UV
    float2 detailUv = TransformSecondaryUv(surface, dirtDetailTex_ST);
    #else
    float2 detailBaseUv = surface.uv0 * inputUvMultiplier;
    #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET) && defined(SECONDARY_UVS_OCCLUSION_DETAIL)
    detailBaseUv += UNITY_ACCESS_INSTANCED_PROP(Props, _AdditiveUVOffset).xy;
    #endif
    float2 detailUv = detailBaseUv * dirtDetailTex_ST.xy + dirtDetailTex_ST.zw;
    #endif
    surface.occlusionDetail = occlusionDetailIntensity *
        tex2D(dirtDetailTex, detailUv).r + (1.0 - occlusionDetailIntensity);
    #endif
}

// Lit-local parallax helpers
// _PARALLAX_FLEXIBLE_REFLECTED selects the reflected-direction variant.
inline float4 ApplyParallax(
    float4 result, SurfaceData surface, float4 vertexColor,
    float2 inputUvMultiplier, float timeOffset, float2 parallaxTexSpeed,
    float parallaxIntensity, float parallaxIntensityStep,
    float layers, float startOffset, float offsetStep,
    float iridescenceColorInfluence,
    sampler2D parallaxTex, float4 parallaxTex_ST,
    sampler2D parallaxMaskingTex, float4 parallaxMaskingTex_ST,
    float parallaxMaskSpeed, float parallaxMaskIntensity,
    float3 iridescenceAxesMultiplier, float iridescenceTiling,
    float4 parallaxColor)
{
    float2 baseUv = surface.uv0 * inputUvMultiplier;
    float4 timeValue = GetTime(timeOffset);
    float3 cameraPosition = GetStereoAwareCameraPosition();
    float3 directionToCamera = normalize(surface.worldPosition - cameraPosition);
    #if defined(_PARALLAX_FLEXIBLE_REFLECTED)
    float3 parallaxDirection = directionToCamera -
        2.0 * dot(directionToCamera, surface.normalWS) * surface.normalWS;
    #else
    float3 parallaxDirection = directionToCamera;
    #endif

    float3 hueShift;
    {
        #if defined(_PARALLAX_FLEXIBLE_REFLECTED)
        float3 iridescenceDirection = directionToCamera -
            2.0 * dot(directionToCamera, surface.normalWS) * surface.normalWS;
        #else
        float3 iridescenceDirection = directionToCamera;
        #endif

        #if defined(PARALLAX_IRIDESCENCE)
        float iridescenceDot = dot(iridescenceDirection, iridescenceAxesMultiplier);
        iridescenceDot = frac(iridescenceDot * iridescenceTiling);
        hueShift = iridescenceDot.xxx * 6.0 + float3(0.0, 4.0, 2.0);
        hueShift = frac(hueShift * (1.0 / 6.0)) * 6.0 - 3.0;
        hueShift = saturate(abs(hueShift) - 1.0);
        float3 hueShiftSquared = hueShift * hueShift;
        hueShift = (-hueShift * 2.0 + 3.0) * hueShiftSquared;
        #else
        hueShift = float3(1.0, 1.0, 1.0);
        #endif
    }

    #if defined(SECONDARY_UVS_PARALLAX) && USE_SECONDARY_UV
    float2 parallaxUv = TransformSecondaryUv(surface, parallaxTex_ST);
    #else
    float2 parallaxUv = baseUv * parallaxTex_ST.xy + parallaxTex_ST.zw;
    #endif
    parallaxUv += timeValue.x * parallaxTexSpeed * parallaxTex_ST.xy;

    float3 layerColor = float3(0.0, 0.0, 0.0);
    for (float layer = 0.0; layer < layers; layer += 1.0)
    {
        float layerIndex = floor(layer);
        float offset = offsetStep * layerIndex + startOffset;
        float2 sampleUv = offset.xx * parallaxDirection.xy + parallaxUv;
        float4 parallaxSample = tex2D(parallaxTex, sampleUv);

        float3 layerIridescence;
        if (layerIndex <= 0.1) layerIridescence = hueShift.xyz;
        else if (layerIndex <= 1.1) layerIridescence = hueShift.yzx;
        else if (layerIndex <= 2.1) layerIridescence = hueShift.zyx;
        else if (layerIndex <= 3.1) layerIridescence = hueShift.xzy;
        else if (layerIndex <= 4.1) layerIridescence = hueShift.yxz;
        else layerIridescence = hueShift.zxy;

        float intensity = (parallaxIntensityStep * layerIndex + parallaxIntensity) *
            parallaxSample.x;
        layerColor += intensity * layerIridescence;
    }
    #if defined(_PARALLAX_MASKING_VERTEX_COLOR)
    layerColor *= vertexColor.g;
    #elif defined(_PARALLAX_MASKING_TEXTURE)
    float4 maskSample = tex2D(
        parallaxMaskingTex,
        TRANSFORM_TEX(baseUv, parallaxMaskingTex) + parallaxMaskSpeed * timeValue.y);
    layerColor = lerp(layerColor, layerColor * maskSample.r, parallaxMaskIntensity);
    #endif
    #if defined(PARALLAX_IRIDESCENCE)
    float grayscaleLayer = (layerColor.r + layerColor.g + layerColor.b) * 0.5;
    float3 blended = iridescenceColorInfluence.xxx *
        (grayscaleLayer.xxx * parallaxColor.rgb - layerColor) + layerColor;
    result.rgb += blended * parallaxColor.a;
    #else
    result.rgb += layerColor * parallaxColor.rgb;
    #endif
    return result;
}

// Lit-local reflection helpers
inline float CalculateLitReflectionTextureRimDim(
    float3 worldPosition, float rimFactor,
    float rimDistanceOffset, float rimDistanceScale, float rimScale)
{
    float3 cameraPosition = GetStereoAwareCameraPosition();
    float cameraDistance = length(worldPosition - cameraPosition);
    float rimDistance = max(cameraDistance - rimDistanceOffset, 0.0) *
        rimDistanceScale + rimScale;
    return rimDistance * rimFactor;
}

// Lit-local emission helpers
inline EmissionData InitializeEmissionData()
{
    EmissionData emission;
    emission.color = 0.0;
    emission.bloomAlpha = 0.0;
    return emission;
}

inline float4 ResolveTime(float timeOffset)
{
    return GetTime(timeOffset);
}

inline float ResolveVertexEmissionFactor(
    float vertexGreen, float emissionThreshold, float emissionStrength)
{
    float threshold = saturate((vertexGreen - emissionThreshold) /
        (1.0 - emissionThreshold));
    return threshold * threshold * (3.0 - 2.0 * threshold) * emissionStrength;
}

inline EmissionData ResolveVertexEmission(
    float4 vertexColor, float4 emissionColor,
    float emissionThreshold, float emissionStrength,
    float baseColorBoost, float baseColorBoostThreshold,
    float questWhiteboostMultiplier, float emissionBloomIntensity)
{
    float threshold = ResolveVertexEmissionFactor(
        vertexColor.g, emissionThreshold, emissionStrength);
    EmissionData emission = InitializeEmissionData();
    #if defined(_VERTEX_WHITEBOOSTTYPE_ALWAYS) || \
        (defined(_VERTEX_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM))
    float4 weightedEmissionColor = emissionColor.a * emissionColor;
    float whiteBoost = CalculateWhiteBoost(
        threshold * weightedEmissionColor.a * vertexColor.a, 1.0,
        baseColorBoost, baseColorBoostThreshold);
    emission.color = saturate(weightedEmissionColor.rgb * threshold + whiteBoost) *
        questWhiteboostMultiplier;
    #else
    emission.color = emissionColor.rgb * emissionColor.a * threshold;
    #endif
    #if defined(_VERTEXMODE_EMISSIVE_MULT_ADD)
    emission.bloomAlpha = vertexColor.g * vertexColor.g * emissionColor.a *
        emissionBloomIntensity;
    #else
    emission.bloomAlpha = vertexColor.a * vertexColor.a * emissionColor.a *
        emissionBloomIntensity;
    #endif
    return emission;
}

inline EmissionData ResolvePlainEmission(
    float2 emissionInput, float4 emissionColor, float emissionTexBloomIntensity)
{
    EmissionData emission = InitializeEmissionData();
    emission.color = emissionInput.r * emissionColor.rgb * emissionColor.a;
    emission.bloomAlpha = emissionInput.g * emissionInput.g * emissionColor.a *
        3.5 * emissionTexBloomIntensity;
    return emission;
}

// Lit-local hologram helper
inline float4 ApplyHologram(
    float4 result, float3 worldPosition, float3 objectPosition, float4 timeValue,
    float gridSize, float scanDistance,
    float holoIntensity, float haltScan, float stripeSpeed, float phaseOffset,
    float fill, float3 hologramColor)
{
    #if defined(_HOLOGRAM_GRID)
    {
        float time = timeValue.w;
        time = haltScan > 0.5 ? 0.0 : time;

        float3 gridPhase = time * float3(0.0, stripeSpeed, stripeSpeed * 0.5);
        float3 cameraPosition = GetStereoAwareCameraPosition();
        float cameraDistance = length(worldPosition - cameraPosition);
        float distanceFactor = saturate(cameraDistance * asfloat(0x3e088889));
        distanceFactor = 1.0 - (1.0 - distanceFactor) * (1.0 - distanceFactor);
        float resolvedGridSize = gridSize - distanceFactor * 10.0;
        float colorScale = 1.0 - distanceFactor * 0.6;

        float3 gridPosition = float3(abs(objectPosition.x), objectPosition.yz);
        float3 gridWave = cos(frac(-gridPosition * resolvedGridSize - gridPhase) +
            fill);
        float grid = gridWave.x * gridWave.y * gridWave.z;

        float scanPosition = (worldPosition.y - unity_ObjectToWorld._m13 +
            phaseOffset * scanDistance) / scanDistance;
        float scan = frac(-time * stripeSpeed + scanPosition);
        float leadingInput = max((asfloat(0x3cccccc0) - scan) * 40.00004, 0.0);
        float trailingInput = max((0.975 - scan) * -40.0, 0.0);
        float leading = 1.0 - leadingInput * leadingInput *
            (3.0 - 2.0 * leadingInput);
        float trailing = trailingInput * trailingInput *
            (3.0 - 2.0 * trailingInput);
        float envelope = (trailing - leading) * 0.25 + 1.25;
        float scanGrid = saturate((1.0 - scan) * leading + grid);
        float hologram = envelope - scanGrid;

        result.rgb += hologram * colorScale * holoIntensity * hologramColor;
    }
    #elif defined(_HOLOGRAM_SCANLINE)
    {
        float time = timeValue.w;
        float scanTime = haltScan > 0.5 ? -0.0 : -time * stripeSpeed;
        float scanPosition = (worldPosition.y - unity_ObjectToWorld._m13 +
            phaseOffset * scanDistance) / scanDistance;
        float scan = min(
            (1.0 - frac(scanTime + scanPosition)) * asfloat(0x3fd55555), 1.0);
        scan = 1.0 - scan * scan * (3.0 - 2.0 * scan);
        result.rgb += scan * holoIntensity * hologramColor;
    }
    result.a = 0.0;
    #elif defined(_HOLOGRAM_LEGACY)
    {
        float time = timeValue.w;
        float4 relativePosition = worldPosition.yxyz -
            unity_ObjectToWorld._m13_m03_m13_m23;
        float4 scaledPosition = relativePosition * gridSize;

        float3 wavePosition = scaledPosition.yzw -
            time * float3(0.0, 1.0, 0.0);
        float3 waves = sin(frac(wavePosition) * asfloat(0x40490fdb)) * 1.2;
        float pulsePosition = frac((scaledPosition.x * 0.33 + time) * 0.2);
        pulsePosition = min(pulsePosition * 2.0, 1.0);
        float pulseEdge = min(pulsePosition * asfloat(0x419ffffe), 1.0);
        pulseEdge = pulseEdge * pulseEdge * (3.0 - 2.0 * pulseEdge);
        float pulse = pulseEdge * (1.0 - pulsePosition);

        float modulation = cos(time * 2.0 + relativePosition.y +
            relativePosition.z - relativePosition.w * 7.0) * 0.4 + 0.8;
        float hologram =
            (waves.x * waves.y * waves.z * modulation + pulse) * pulse;
        result.rgb += hologram * hologramColor;
    }
    #endif
    return result;
}

// Lit-local rim helpers
inline float CalculateRimLightMask(
    float3 worldPosition, float3 normalWS, float rimLightEdgeStart,
    float3 rimPerpendicularAxis)
{
    float3 cameraPosition = GetStereoAwareCameraPosition();
    float3 viewDirection = normalize(worldPosition - cameraPosition);
    float rimLight = 1.0 - abs(dot(normalWS, viewDirection));
    rimLight = smoothstep(0.0, 1.0, saturate(
                              (rimLight - rimLightEdgeStart) / (1.0 - rimLightEdgeStart)));
    #if defined(DIRECTIONAL_RIM)
    float3 directionalAxis = normalize(
        rimPerpendicularAxis + (dot(rimPerpendicularAxis, rimPerpendicularAxis) < 0.00001
                                    ? float3(0.0, 1.0, 0.0)
                                    : float3(0.0, 0.0, 0.0)));
    rimLight *= 1.0 - abs(dot(normalWS, directionalAxis));
    #endif
    return rimLight;
}

inline float3 ResolveRimLightTarget(
    float3 rimColor, float rimScale, float rimLight,
    float rimLightWhiteboostMultiplier,
    float baseColorBoost, float baseColorBoostThreshold)
{
    float3 target = rimColor * rimScale;
    #if defined(_RIM_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)
    float whiteBoost = CalculateWhiteBoost(
        rimLight, rimScale, baseColorBoost, baseColorBoostThreshold);
    target = saturate(target + whiteBoost) * rimLightWhiteboostMultiplier;
    #endif
    return target;
}

inline float4 ApplyRimLight(
    float4 result, float3 worldPosition, float3 normalWS,
    float rimLightEdgeStart, float4 rimLightColor,
    float rimLightIntensity, float rimLightBloomIntensity,
    float3 rimPerpendicularAxis, float rimLightWhiteboostMultiplier,
    float baseColorBoost, float baseColorBoostThreshold)
{
    float rimLight = CalculateRimLightMask(
        worldPosition, normalWS, rimLightEdgeStart, rimPerpendicularAxis);
    float rimLightScale = rimLightColor.a * rimLightIntensity;
    float3 rimTarget = ResolveRimLightTarget(
        rimLightColor.rgb, rimLightScale, rimLight,
        rimLightWhiteboostMultiplier, baseColorBoost, baseColorBoostThreshold);
    result.rgb = lerp(result.rgb, rimTarget, rimLight);
    result.a = rimLightScale * rimLight * rimLightBloomIntensity;
    return result;
}

// Lit-local height-fog helpers
inline float4 ApplyHeightFogCurve(float4 result, float exactHeightInput)
{
    float exactHeightFog = CalculateHeightFogFactor(exactHeightInput);
    return exactHeightFog.xxxx *
        (float4(0.1, 0.1, 0.1, 0.0) - result) + result;
}

inline float4 ApplyLitColorFog(
    float4 result, float3 worldPosition,
    float colorFogMultiplier, float colorFogMax,
    float colorFogHighlightMultiplier, float colorFogInfluence,
    float fogHeightScale, float fogHeightOffset)
{
    #if defined(FOG_COLOR_HIGHLIGHT)
    float colorFogHighlight = min(
        asfloat(0x38d1b718u) * colorFogHighlightMultiplier,
        colorFogMax);
    #else
    float colorFogHighlight = 0.0;
    #endif
    float4 fogTarget = min(
        float4(0.1, 0.1, 0.1, 0.0) * colorFogMultiplier *
            (1.0 + colorFogHighlight),
        colorFogMax);
    float4 colorFogResult = float4(
        result.rgb * colorFogInfluence + fogTarget.rgb, result.a);
    float exactHeightInput =
        worldPosition.y * fogHeightScale + fogHeightOffset;
    float exactHeightFog = CalculateHeightFogFactor(exactHeightInput);
    return exactHeightFog.xxxx *
        (fogTarget - colorFogResult) + colorFogResult;
}

// Lit-local vertex-displacement helpers
#if defined(VERTEXDISPLACEMENT_MASK)
inline float ComposeVertexDisplacementMask(float displacementScale, float mask)
{
    // Scalar mode 0 multiplies the displacement scale by the sampled mask.
    return _VertexDisplacementMaskMode == 0.0
               ? displacementScale * mask
               : displacementScale + mask;
}
#endif

// Vertex payload and program
struct appdata
{
    float4 vertex : POSITION;
    #if USE_VERTEX_COLOR
    float4 color : COLOR;
    #endif
    float2 uv1 : TEXCOORD0;
    #if USE_SECONDARY_UV || defined(COLOR_ARRAY) || defined(LIGHTMAP)
    float2 uv2 : TEXCOORD1;
    #elif USE_RIBBON_SPATIAL_MASK_VERTEX
    float2 displacementUv : TEXCOORD1;
    #endif
    #if defined(_SPECTROGRAM_FULL)
    float2 uv3 : TEXCOORD2;
    #endif
    #if USE_WORLD_NORMAL
    float3 normal : NORMAL;
    #endif
    #if USE_NORMAL_MAP_PAYLOAD
    float4 tangent : TANGENT;
    #endif
    #if defined(MESH_PACKING)
    #if USE_MESH_PACKING_UV1
    float2 packingUv : TEXCOORD1;
    #else
    float2 packingUv : TEXCOORD3;
    #endif
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

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
    float3 worldPos : TEXCOORD1;
    float4 screenPos : TEXCOORD2;
    #if USE_WORLD_NORMAL
    float3 worldNormal : TEXCOORD3;
    #endif
    #if USE_NORMAL_MAP_PAYLOAD
    float3 tangentWS : TEXCOORD12;
    float3 bitangentWS : TEXCOORD13;
    #endif
    #if USE_ANTIFLICKER_NORMAL_PAYLOAD
    centroid float3 antiflickerNormal : TEXCOORD15;
    #endif
    #if USE_NOISE_SCREEN_POSITION
    float4 noiseScreenPos : TEXCOORD4;
    #endif
    #if defined(COLOR_ARRAY)
    float2 colorArrayId : TEXCOORD17;
    #endif
    #if defined(EMISSION_ANGLE_DISAPPEAR)
    float emissionAngle : TEXCOORD18;
    #endif
    #if defined(REFLECTION_TEXTURE)
    float3 reflectionTextureDirection : TEXCOORD10;
    float reflectionTextureRimFactor : TEXCOORD14;
    #elif defined(RIM_DIM)
    float rimDim : TEXCOORD14;
    #endif
    #if defined(LIGHTMAP)
    float2 lightmapUv : TEXCOORD11;
    #endif
    #if defined(_EMISSIONTEXTURE_FLIPBOOK)
    float2 flipbookUv : TEXCOORD22;
    float4 flipbookFrameSelector : TEXCOORD16;
    #endif
    #if defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION)
    float lookupEmission : TEXCOORD33;
    #endif
    #if defined(UV_COLOR_SEGMENTS)
    float4 uvSegmentColor : TEXCOORD19;
    float4 uvSegmentRimColor : TEXCOORD20;
    #endif
    #if defined(HIGHLIGHT_SELECTION)
    float highlightSelection : TEXCOORD21;
    #endif
    #if defined(_HOLOGRAM_GRID)
    float3 hologramObjectPosition : TEXCOORD30;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata i, uint id : SV_VertexID)
{
    v2f o;

    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_TRANSFER_INSTANCE_ID(i, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    #if defined(HIGHLIGHT_SELECTION)
    o.highlightSelection = _SegmentToHighlight == 0;
    #endif

    #if defined(MESH_PACKING)
    float exactMeshPackingId = UNITY_ACCESS_INSTANCED_PROP(Props, _MeshPackingId);
    if (abs(i.packingUv.y - exactMeshPackingId) > 0.1)
        i.vertex = float4(0.0, 0.0, 0.0, 0.0);
    #endif

    #if USE_SPHERE_SDF_3D_VERTEX
    {
        float timeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
        float4 undisplacedWorldPosition = mul(unity_ObjectToWorld, i.vertex);
        float3 displacementUv = _VertexDisplacement3DTexScale *
            undisplacedWorldPosition.xyz;
        displacementUv += _VertexDisplacement3DTexPanning * timeOffset * 0.1 +
            _VertexDisplacement3DTexOffset;
        float displacementLod = _VertexDisplacement3DTexScale *
            undisplacedWorldPosition.w;
        float3 displacementMask = tex3Dlod(
            _VertexDisplacement3DTexture,
            float4(displacementUv, displacementLod)).rgb;
        displacementMask = _VertexDisplacementMaskMultiplier * displacementMask +
            _VertexDisplacementMaskOffset;
        float displacementStrength = i.color.b *
            UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength);
        float3 displacementAxis =
            UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementAxisMultiplier).xyz;
        i.vertex.xyz += displacementMask * displacementStrength * displacementAxis * i.normal;
    }
    #elif USE_RIBBON_SPATIAL_MASK_VERTEX
    {
        float frozenTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) * 0.05;
        float2 displacementUv = i.displacementUv * _VertexDisplacementMask_ST.xy +
            _VertexDisplacementMask_ST.zw;
        displacementUv += _VertexDisplacementMask_ST.xy *
            _VertexDisplacementMaskSpeed * frozenTime;
        float3 displacementMask = tex2Dlod(
            _VertexDisplacementMask, float4(displacementUv, 0.0, 0.0)).rgb;
        displacementMask = _VertexDisplacementMaskMultiplier * displacementMask +
            _VertexDisplacementMaskOffset;
        float3 displacementDirection = (i.color.rgb * 2.0 - 1.0) *
            UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementAxisMultiplier).xyz;
        float displacementStrength =
            UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength);
        i.vertex.xyz += displacementMask * displacementStrength * displacementDirection;
    }
    #elif defined(_VERTEXMODE_DISPLACEMENT)
    {
        float3 dispDir;
    #if defined(DISPLACEMENT_SPATIAL)
    // RGB channels → XYZ displacement direction
    dispDir = i.color.xyz;
    #   if defined(DISPLACEMENT_BIDIRECTIONAL)
    dispDir = dispDir * 2.0 - 1.0;
    #   elif !defined(_SPECTROGRAM_FLAT) && !defined(_SPECTROGRAM_FULL) && !defined(VERTEXDISPLACEMENT_MASK)
    dispDir *= UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength);
    #   endif
    dispDir *= UNITY_ACCESS_INSTANCED_PROP(
        Props, _DisplacementAxisMultiplier).xyz;
    #else
    // Default: displace along vertex normal, magnitude from blue channel
    dispDir = i.normal * i.color.b;
    #   if defined(DISPLACEMENT_BIDIRECTIONAL)
    dispDir = dispDir * 2.0 - 1.0;
    #   endif
    dispDir *= UNITY_ACCESS_INSTANCED_PROP(
        Props, _DisplacementAxisMultiplier).xyz;
    #endif

    float spectrogramScale = 1.0;
    #if defined(_SPECTROGRAM_FLAT)
    // SpectrogramRow uploads this scalar through MPB.SetFloat.
    spectrogramScale = UNITY_ACCESS_INSTANCED_PROP(Props, _SpectrogramData);
    #elif defined(_SPECTROGRAM_FULL)
    // ChroMapper's spectrogram producer uploads 64 scalar bins.
    uint bin = min((uint)(saturate(i.uv3.x) * 64.0), 63u);
    spectrogramScale = _SpectrogramData[bin];
    #endif

    float _dispScale = UNITY_ACCESS_INSTANCED_PROP(Props, _DisplacementStrength) * (spectrogramScale);

    #if defined(VERTEXDISPLACEMENT_MASK)
    { 
    #if defined(_VERTEXDISPLACEMENT_MASK_SOURCE_3D_TEXTURE)
    // 3D texture mask:
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
        _dispScale = ComposeVertexDisplacementMask(_dispScale, _dmVal.x);
    }
    #elif defined(_VERTEXDISPLACEMENT_MASK_SOURCE_EMISSION_TEXTURE)
    { 
    #if defined(_CUSTOM_TIME_FREEZE)
    float _dmTime = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) * 0.05;
    #else
    float _dmTime = (_Time.y + UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) * 0.05;
    #endif
    float2 _dmUv = i.uv1.xy * _EmissionTex_ST.xy + _EmissionTex_ST.zw;
    _dmUv += _EmissionTex_ST.xy * _EmissionTexSpeed * _dmTime.xx;
    float _dmSample = tex2Dlod(_EmissionTex, float4(_dmUv, 0.0, 0.0)).r;
    _dispScale = ComposeVertexDisplacementMask(
        _dispScale,
        _VertexDisplacementMaskMultiplier * _dmSample + _VertexDisplacementMaskOffset);
            }
    #else
    // 2D texture mask for the VERTEXDISPLACEMENT_MASK path.
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
    _dispScale = ComposeVertexDisplacementMask(_dispScale, _dmVal.x);
            }
    #endif
        }
    #endif

    i.vertex.xyz += _dispScale * dispDir;
    }
    #endif

    #if defined(TEXTURE3D_LOOKUP)
    {
        float3 lookupIndex = UNITY_ACCESS_INSTANCED_PROP(
            Props, _LookupGridElementIndex).xyz;
        float3 lookupUv = (lookupIndex + 0.5) / _LookupGridSize;
        float4 lookupValue = tex3Dlod(_LookupTex, float4(lookupUv, 0.0));
        lookupValue = lookupValue * 2.0 - 1.0;

        float3 radialVector = 0.001.xxx - UNITY_ACCESS_INSTANCED_PROP(
            Props, _LookupGridObjectSpacePivot).xyz;
        float radialDistance = length(radialVector);
        float3 radialDirection = radialVector * rsqrt(dot(radialVector, radialVector));
        float radialFactor = exp2(log2(max(_LookupRadialDisplacementScale, 0.0)) *
            dot(lookupValue, _LookupRadialDisplacementMapping));
        float lookupScale = exp2(log2(max(_LookupMaxScale, 0.0)) *
            dot(lookupValue, _LookupScaleMapping));
        float rotation = dot(lookupValue, _LookupRotationMapping) *
            _LookupRotationMultiplier * 6.28319;
        float sine = sin(rotation);
        float cosine = cos(rotation);

        float3 scaledPosition = lookupScale * i.vertex.xyz;
        float2 rotatedPosition = float2(
            cosine * scaledPosition.x - sine * scaledPosition.y,
            sine * scaledPosition.x + cosine * scaledPosition.y);
        float2 rotatedNormal = float2(
            cosine * i.normal.x - sine * i.normal.y,
            sine * i.normal.x + cosine * i.normal.y);
        i.vertex.xyz = float3(rotatedPosition, scaledPosition.z);
        i.normal.xy = rotatedNormal;
        i.vertex.xyz += radialDistance * (radialFactor - 1.0) * radialDirection;
        i.vertex.xyz += _LookupXYZDisplacementScale * float3(
            dot(lookupValue, _LookupXDisplacementMapping),
            dot(lookupValue, _LookupYDisplacementMapping),
            dot(lookupValue, _LookupZDisplacementMapping));
    #if defined(TEXTURE3D_EMISSION)
    o.lookupEmission = (1.0 + dot(lookupValue, _LookupEmissiveMapping)) *
        _LookupEmissiveModulationStrength * 0.5;
    #endif
    }
    #endif

    o.vertex = UnityObjectToClipPos(i.vertex);
    #if USE_VERTEX_COLOR
    o.color = i.color;
    #if USE_VERTEX_EMISSION
    #if defined(COLOR_ARRAY)
    {
        // UV1.xy and the per-renderer offset form the emission palette index.
        float _caIdx = round(i.uv2.x * 10.0 + i.uv2.y +
            UNITY_ACCESS_INSTANCED_PROP(Props, _ColorsArrayOffset));
        o.emission = _ColorsArray[_caIdx];
    }
    #else
    o.emission = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
    #endif
    #endif
    #endif

    o.uv.xy = i.uv1.xy;
    #if defined(_HOLOGRAM_GRID)
    o.hologramObjectPosition = i.vertex.xyz;
    #endif
    #if defined(UV_COLOR_SEGMENTS)
    uint uvSegmentIndex = (uint)max(i.uv1.x * 10.0, 0.0);
    o.uvSegmentColor = _UVColors[uvSegmentIndex];
    o.uvSegmentRimColor = _UVRimColors[uvSegmentIndex];
    #endif
    #if defined(HIGHLIGHT_SELECTION)
    #if defined(UV_COLOR_SEGMENTS)
    o.highlightSelection = floor(i.uv1.x * 10.0) == floor(_SegmentToHighlight);
    #endif
    #endif
    #if defined(LIGHTMAP)
    o.lightmapUv = i.uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif
    #if USE_SECONDARY_UV
    o.uv.zw = i.uv2.xy;
    #if USE_UV_SCALE
    o.uv.zw *= _UVScale.xy;
    #endif
    #endif

    #if defined(_EMISSIONTEXTURE_FLIPBOOK)
    {
        float totalFrames = trunc(_FlipbookRows * _FlipbookColumns);
        float loopingFrameCount = trunc(totalFrames - _FlipbookNonloopableFrames);
        float elapsed = _SongTime.y +
            UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset) -
            UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime);
        float frameTime = elapsed * _FlipbookSpeed;
        float loopingTime = frameTime - _FlipbookNonloopableFrames;
        float loopingProduct = loopingFrameCount * loopingTime;
        float signedLoopingFrameCount = loopingProduct >= -loopingProduct
                                            ? loopingFrameCount
                                            : -loopingFrameCount;
        float wrappedFrame = frac(loopingTime / signedLoopingFrameCount) *
            signedLoopingFrameCount + _FlipbookNonloopableFrames;
        float frame = frameTime < totalFrames ? frameTime : wrappedFrame;
        float frameFloor = floor(frame);
        float columnQuotient = frameFloor / _FlipbookColumns;
        float columnSign = columnQuotient >= -columnQuotient ? 1.0 : -1.0;
        float column = frac(abs(columnQuotient)) * columnSign * _FlipbookColumns;
        float row = _FlipbookRows - 1.0 - floor(frame / _FlipbookColumns);
        o.flipbookUv = (float2(column, row) + i.uv1.xy) /
            float2(_FlipbookColumns, _FlipbookRows);

        float frameFraction = frac(frame);
        float4 frameSelector = frameFraction < 0.75
                                   ? float4(0.0, 0.0, 1.0, 0.0)
                                   : float4(0.0, 0.0, 0.0, 1.0);
        frameSelector = frameFraction < 0.5 ? float4(0.0, 1.0, 0.0, 0.0) : frameSelector;
        frameSelector = frameFraction < 0.25 ? float4(1.0, 0.0, 0.0, 0.0) : frameSelector;
        o.flipbookFrameSelector = i.uv1.x + i.uv1.y <= 0.01 ? 0.0.xxxx : frameSelector;
    }
    #endif

    #if USE_WORLD_NORMAL
    float3 sourceLocalNormal = i.normal;
    #if defined(USE_SPHERICAL_NORMAL_OFFSET)
    sourceLocalNormal = lerp(
        sourceLocalNormal,
        i.vertex.xyz + _SphericalNormalOffsetCenter,
        _SphericalNormalOffsetIntensity);
    #endif
    #if defined(PRECISE_NORMAL)
    // The precise route normalizes before interpolation and again in
    // the fragment stage.
    o.worldNormal = normalize(UnityObjectToWorldNormal(sourceLocalNormal));
    #else
    o.worldNormal = normalize(UnityObjectToWorldNormal(sourceLocalNormal));
    #endif
    #if USE_NORMAL_MAP_PAYLOAD
    o.tangentWS = normalize(UnityObjectToWorldDir(i.tangent.xyz));
    float tangentSign = i.tangent.w * unity_WorldTransformParams.w;
    o.bitangentWS = cross(o.worldNormal, o.tangentWS) * tangentSign;
    #endif
    #endif
    #if USE_ANTIFLICKER_NORMAL_PAYLOAD
    o.antiflickerNormal = o.worldNormal;
    #endif
    o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
    #if defined(EMISSION_ANGLE_DISAPPEAR) || defined(REFLECTION_TEXTURE) || defined(RIM_DIM)
    float3 cameraPosition = GetStereoAwareCameraPosition();
    #endif
    #if defined(EMISSION_ANGLE_DISAPPEAR)
    float3 emissionViewDirection = normalize(
        cameraPosition - o.worldPos.xyz);
    float emissionAngleDot = abs(dot(o.worldNormal, emissionViewDirection));
    o.emissionAngle = smoothstep(0.0, 1.0, saturate(
                                     (emissionAngleDot - 0.05) / (
                                         _EmissionThresholdAngle
                                         - 0.05)));
    #endif
    #if defined(REFLECTION_TEXTURE)
    float3 reflectionTextureViewDirection = normalize(
        cameraPosition - o.worldPos.xyz);
    o.reflectionTextureRimFactor = saturate(
        _RimOffset + 1.0 - dot(o.worldNormal, reflectionTextureViewDirection));
    o.reflectionTextureDirection = reflect(
        -reflectionTextureViewDirection, o.worldNormal);
    #elif defined(RIM_DIM)
    float3 rimViewDirection = normalize(cameraPosition - o.worldPos.xyz);
    float rimNormalDot = dot(o.worldNormal, rimViewDirection);
    #if defined(INVERT_RIM_DIM)
    float rimFacing = saturate(rimNormalDot + _RimOffset);
    #else
    float rimFacing = saturate(1.0 + _RimOffset - rimNormalDot);
    #endif
    o.rimDim = rimFacing;
    #endif
    o.screenPos = ComputeScreenPosCustom(o.vertex);
    #if USE_NOISE_SCREEN_POSITION
    o.noiseScreenPos = BuildNoiseScreenPosition(
        o.screenPos, o.vertex, _GlobalBlueNoiseParams,
        _GlobalRandomValue, unity_ObjectToWorld._m03_m13);
    #endif
    #if defined(COLOR_ARRAY)
    o.colorArrayId.x = i.uv2.x;
    o.colorArrayId.y = i.uv2.y +
        UNITY_ACCESS_INSTANCED_PROP(Props, _ColorsArrayOffset);
    #endif

    return o;
}

// Fragment program
#if defined(OVERDRAW_VIEW)
#include "ShaderLibrary/Common/Overdraw.hlsl"
#endif

#if defined(CM_DEPTH_ONLY)
float _ZWrite;
#endif

float4 frag(v2f i, float facing : VFACE) : SV_Target
{
    #if defined(CM_DEPTH_ONLY)
    clip(_ZWrite - 0.5);
    #endif
    #if defined(OVERDRAW_VIEW) && !defined(CM_DEPTH_ONLY)
    return CalculateOpaqueOverdrawColor();
    #else
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #if USE_SECONDARY_UV
    float2 uv2 = i.uv.zw;
    #else
    float2 uv2 = i.uv.xy;
    #endif

    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
    #if defined(UV_COLOR_SEGMENTS)
    baseColor = i.uvSegmentColor;
    #endif
    #if defined(COLOR_ARRAY) && !defined(_EMISSIONCOLORTYPE_MAINEFFECT)
    float colorIndex = round(i.colorArrayId.x * 10 + i.colorArrayId.y);
    float4 arrayColor = _ColorsArray[colorIndex];
    baseColor.rgb = arrayColor.rgb * _Intensity;
    baseColor.a = arrayColor.a * _AlphaMultiplier;
    #endif
    #if defined(_VERTEXMODE_COLOR)
    baseColor *= i.color;
    #endif

    // Always start from black: baseColor contributes only via diffuse/ambient,
    // so objects are pitch dark without emission or lights.
    float4 albedo = 0;
    {
        #if defined(DIFFUSE_TEXTURE)
        float2 baseUv = i.uv.xy * _InputUvMultiplier;
        #if defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_R)
        float2 mpmUv = baseUv * _MetalSmoothnessTex_ST.xy +
            _MetalSmoothnessTex_ST.zw;
        baseColor.rgb *= tex2D(_MetalSmoothnessTex, mpmUv).r;
        #elif defined(METAL_SMOOTHNESS_TEXTURE) && defined(_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS)
        float2 mpmUv = baseUv * _MetalSmoothnessTex_ST.xy +
            _MetalSmoothnessTex_ST.zw;
        baseColor.rgb *= tex2D(_MetalSmoothnessTex, mpmUv).a * _Smoothness;
        #else
        float2 diffuseUv = baseUv * _DiffuseTex_ST.xy + _DiffuseTex_ST.zw;
        baseColor.rgb *= tex2D(_DiffuseTex, diffuseUv).rgb;
        #endif
        #endif
    }

    float3 worldPos = i.worldPos;

    // DISSOLVE_TEXTURE and DISSOLVE_COLOR are child selectors and remain
    // inert unless the semantic parent DISSOLVE is enabled.
    #if defined(DISSOLVE)
    float dissolveTime = ResolveTime(
        UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)).y;
    float dissolveFactor;
    {
        float3 axis = normalize(_DissolveAxisVector);
    #if defined(DISSOLVE_PROGRESS)
    float dissolveProgress = UNITY_ACCESS_INSTANCED_PROP(
        Props, _DissolveProgress);
    float direction = dissolveProgress < -0.001 ? -1.0 : 1.0;
    axis *= direction;
    float threshold = abs(dissolveProgress) *
        (_DissolveEndValue - _DissolveStartValue) + _DissolveStartValue;
    #else
    float threshold = _DissolveOffset;
    #endif

    #if defined(_DISSOLVE_SPACE_WORLD_CENTERED)
    float3 localOffset = worldPos - unity_ObjectToWorld._m03_m13_m23;
    #else
    float3 localOffset = worldPos;
    #endif
    float projected = dot(localOffset, axis);
    float dissolveValue = projected - threshold;
    #if defined(DISSOLVE_TEXTURE)
    float2 dissolveUv = i.uv.xy * _DissolveTexture_ST.xy +
        _DissolveTexture_ST.zw;
    dissolveUv += dissolveTime.xx * _DissolveTextureSpeed *
        _DissolveTexture_ST.xy;
    float textureOffset = tex2D(_DissolveTexture, dissolveUv).r * 2.0 - 1.0;
    dissolveValue += textureOffset * _DissolveTextureInfluence;
    #endif
    dissolveValue *= (_DissolveReverse > 0.5) ? -1.0 : 1.0;

    if (dissolveValue < 0.0)
        discard;

    #if defined(DISSOLVE_COLOR)
    float facingMultiplier = facing > 0.0 ? 1.0 : _CutColorBacksideFalloff;
    float edgeFalloff = saturate(
        -dissolveValue * _CutColorFalloff * facingMultiplier + 1.0);
    edgeFalloff = edgeFalloff * edgeFalloff * edgeFalloff;
    dissolveFactor = edgeFalloff *
        UNITY_ACCESS_INSTANCED_PROP(Props, _DissolveColor).a;
    #else
    dissolveFactor = 0.0;
    #endif
    }
    #endif
    #if defined(CM_DEPTH_ONLY)
    return 0;
    #endif
    #if USE_WORLD_NORMAL
    #if defined(PRECISE_NORMAL)
    float3 worldNormal = normalize(i.worldNormal);
    #else
    float3 worldNormal = i.worldNormal;
    #endif
    #else
    float3 worldNormal = 1;
    #endif
    #if USE_NORMAL_MAP_PAYLOAD
    {
        float2 normalUv = i.uv.xy * _InputUvMultiplier;
        normalUv = normalUv * _NormalTex_ST.xy + _NormalTex_ST.zw;
        float4 normalSample = tex2D(_NormalTex, normalUv);
        // Normal maps pack tangent X in A*R and tangent Y in G.
        float2 normalXY = float2(
            normalSample.a * normalSample.r, normalSample.g) * 2.0 - 1.0;
        float normalZ = sqrt(1.0 - min(dot(normalXY, normalXY), 1.0));
        normalXY *= _NormalScale;
        worldNormal = normalize(
            normalXY.x * i.tangentWS + normalXY.y * i.bitangentWS +
            normalZ * worldNormal);
    }
    #endif

    // Composable lighting boundary. The structures feed the single
    // feature-composed output path in composition order.
    SurfaceData composableSurface = InitializeSurfaceData(
        worldPos, worldNormal, i.uv.xy, uv2, baseColor,
        _Metallic, _Smoothness);
    #if defined(_SECONDARY_UVS_IMPORT)
    composableSurface.secondaryUvTiling = UNITY_ACCESS_INSTANCED_PROP(
        Props, _InstancedSecondaryTiling).xy;
    composableSurface.secondaryUvOffset = UNITY_ACCESS_INSTANCED_PROP(
        Props, _InstancedSecondaryOffset).xy;
    #elif defined(_SECONDARY_UVS_EXTERNAL_SCALE)
    composableSurface.secondaryUvOffset = UNITY_ACCESS_INSTANCED_PROP(
        Props, _InstancedSecondaryOffset).xy;
    #endif
    #if defined(LIGHTMAP)
    composableSurface.lightmapUv = i.lightmapUv;
    #endif
    #if USE_VERTEX_COLOR
    ResolveSurfaceMaterial(
        composableSurface, i.color, _Metallic, _Smoothness,
        _InputUvMultiplier, _MetalSmoothnessTex,
        _MetalSmoothnessTex_ST, _DirtTex, _DirtTex_ST,
        _OcclusionIntensity, _DirtDetailTex, _DirtDetailTex_ST,
        UNITY_ACCESS_INSTANCED_PROP(Props, _OcclusionDetailIntensity));
    #else
    ResolveSurfaceMaterial(
        composableSurface, 1.0, _Metallic, _Smoothness,
        _InputUvMultiplier, _MetalSmoothnessTex,
        _MetalSmoothnessTex_ST, _DirtTex, _DirtTex_ST,
        _OcclusionIntensity, _DirtDetailTex, _DirtDetailTex_ST,
        UNITY_ACCESS_INSTANCED_PROP(Props, _OcclusionDetailIntensity));
    #endif
    float3 nominalDiffuseLevel =
        UNITY_ACCESS_INSTANCED_PROP(Props, _NominalDiffuseLevel).rgb;
    #if !USE_UNIFORM_PRIVATE_POINT_COLOR
    float3 privatePointLightColor =
        UNITY_ACCESS_INSTANCED_PROP(Props, _PrivatePointLightColor).rgb;
    #else
    float3 privatePointLightColor = _PrivatePointLightColor.rgb;
    #endif
    float3 ambientLight = max(
        _AmbientMultiplier * nominalDiffuseLevel, _AmbientMinimalValue);
    LightingData composableLighting;
    {
        composableLighting.directDiffuse = 0.0;
        composableLighting.directSpecular = 0.0;
        composableLighting.reflection = 0.0;
        composableLighting.ambient = 0.0;
        float3 directBaseColor = composableSurface.baseColor.rgb;
        float groundFade = 1.0;
        #if defined(GROUND_FADE)
        groundFade = 1.0 - saturate(
            -composableSurface.worldPosition.y * _GroundFadeScale +
            _GroundFadeOffset);
        directBaseColor *= groundFade;
        #endif
        #if defined(_PROBE_CALCULATION_PRECISE)
        float minimumColor = min(directBaseColor.r,
                                 min(directBaseColor.g, directBaseColor.b));
        float maximumColor = max(directBaseColor.r,
                                 max(directBaseColor.g,
                                     directBaseColor.b));
        float saturation = (maximumColor - minimumColor) / maximumColor;
        composableLighting.ambient = directBaseColor * ambientLight *
            ((saturation + 1.0) * (1.0 - composableSurface.metallic));
        #else
        composableLighting.ambient = directBaseColor * ambientLight;
        #endif

        float3 directDiffuseNormal = composableSurface.normalWS;
        #if USE_ANTIFLICKER_NORMAL_PAYLOAD && !USE_NORMAL_MAP_PAYLOAD
        directDiffuseNormal = dot(worldNormal, worldNormal) >= 1.01
                                  ? i.antiflickerNormal
                                  : worldNormal;
        #endif
        float3 diffuseLights = 0.0;
        #if defined(DIFFUSE)
        #if defined(LIGHT_FALLOFF)
        diffuseLights = CalculateLightFalloffDiffuse(
            composableSurface.worldPosition, directDiffuseNormal);
        #else
        diffuseLights = CalculateLightDiffuse(
            directDiffuseNormal, _BothSidesDiffuseMultiplier);
        #endif
        #endif
        #if defined(PRIVATE_POINT_LIGHT)
        { 
        #if defined(POINT_LIGHT_IS_LOCAL)
        float3 lightPosition = mul(
            unity_ObjectToWorld,
            float4(_PrivatePointLightPosition.xyz, 1.0)).xyz;
        #else
        float3 lightPosition = _PrivatePointLightPosition.xyz;
        #endif
        float3 lightVector = lightPosition - composableSurface.worldPosition;
        float distanceSquared = max(dot(lightVector, lightVector), 0.00001);
        float3 lightDirection = lightVector / sqrt(distanceSquared);
        #if defined(BOTH_SIDES_DIFFUSE)
        float diffuse = abs(dot(directDiffuseNormal, lightDirection));
        #else
        float diffuse = max(dot(directDiffuseNormal, lightDirection), 0.0);
        #endif
        diffuseLights += diffuse * privatePointLightColor *
            _PrivatePointLightIntensity / distanceSquared;
        }
        #endif

        float3 directColor = diffuseLights * directBaseColor;
        #if defined(DIFFUSE) && defined(SPECULAR)
        composableLighting.directDiffuse = directColor *
            (0.96 * (1.0 - composableSurface.metallic));
        #else
        composableLighting.directDiffuse = directColor *
            (1.0 - composableSurface.metallic);
        #endif
        #if defined(SPECULAR)
        #if defined(DIFFUSE)
        float3 specularColor = 0.04 + composableSurface.metallic *
            (directColor - 0.04);
        #else
        float3 specularColor = 0.04 + composableSurface.metallic *
            (directBaseColor - 0.04);
        #endif
        #if defined(LIGHT_FALLOFF)
        float3 specularLights = CalculateLightFalloffSpecular(
            composableSurface.worldPosition, composableSurface.normalWS,
            composableSurface.smoothness);
        #else
        float3 specularLights = CalculateLightSpecular(
            composableSurface.worldPosition, composableSurface.normalWS,
            composableSurface.smoothness);
        #endif
        composableLighting.directSpecular = specularLights * specularColor *
            (_SpecularIntensity * groundFade);
        #if defined(OCCLUSION)
        composableLighting.directSpecular *= composableSurface.occlusion;
        #endif
        #endif
        #if defined(LIGHTMAP)
        float3 lightmap1 = tex2D(_LightMap1, composableSurface.lightmapUv).rgb;
        float3 lightmap2 = tex2D(_LightMap2, composableSurface.lightmapUv).rgb;
        float3 decodedLightmap =
            lightmap1.r * _LightmapLightBakeIdA +
            lightmap1.g * _LightmapLightBakeIdB +
            lightmap1.b * _LightmapLightBakeIdC +
            lightmap2.r * _LightmapLightBakeIdD +
            lightmap2.g * _LightmapLightBakeIdE +
            lightmap2.b * _LightmapLightBakeIdF;
        float3 lightmapBaseColor =
            (1.0 - composableSurface.metallic) * directBaseColor;
        composableLighting.directDiffuse +=
            (decodedLightmap * 4.5947933) * lightmapBaseColor;
        #endif
    }
    float3 composableReflectionNormal = worldNormal;
    #if USE_ANTIFLICKER_NORMAL_PAYLOAD
    composableReflectionNormal = dot(worldNormal, worldNormal) >= 1.01
                                     ? i.antiflickerNormal
                                     : worldNormal;
    #endif
    #if defined(REFLECTION_TEXTURE)
    float composableReflectionTextureRimDim =
        CalculateLitReflectionTextureRimDim(
            worldPos, i.reflectionTextureRimFactor,
            _RimDistanceOffset, _RimDistanceScale, _RimScale);
    {
        float smoothness = composableSurface.smoothness;
    #if defined(RIM_DIM)
    smoothness = saturate(
        smoothness - composableReflectionTextureRimDim * _RimSmoothness);
    #endif

    float roughness = 1.0 - smoothness;
    float reflectionLod = roughness * (1.7 - 0.7 * roughness) * 6.0;
    float3 reflection = texCUBElod(
        _EnvironmentReflectionCube,
        float4(i.reflectionTextureDirection, reflectionLod)).rgb;
    reflection *= _ReflectionTexIntensity;
    #if defined(MULTIPLY_REFLECTIONS)
    reflection *= 1.0 + composableSurface.metallic *
        (composableSurface.baseColor.rgb - 1.0);
    #endif
    reflection *= 2.0 * (composableSurface.metallic * 0.8 + 0.2);
    reflection *= smoothness;
    #if defined(RIM_DIM)
    reflection *= 1.0 - composableReflectionTextureRimDim * _RimDarkening;
    #endif
    composableLighting.reflection = reflection;
    }
    #else
    #if defined(RIM_DIM)
    float composableRimDim = CalculateLitReflectionTextureRimDim(
        worldPos, i.rimDim,
        _RimDistanceOffset, _RimDistanceScale, _RimScale);
    #else
    float composableRimDim = 0.0;
    #endif
    #if !defined(REFLECTION_PROBE)
    composableLighting.reflection = 0.0;
    #else
    {
        float smoothness = composableSurface.smoothness;
    #if defined(RIM_DIM)
    smoothness = saturate(smoothness - composableRimDim * _RimSmoothness);
    #endif
    #if defined(SPECULAR_ANTIFLICKER)
    float3 cameraPosition = GetStereoAwareCameraPosition();
    float cameraDistance = length(
        composableSurface.worldPosition - cameraPosition);
    float weight = saturate(
            (_AntiflickerDistanceOffset - cameraDistance) *
            _AntiflickerDistanceScale) *
        _AntiflickerStrength;
    float3 normalDx = ddx(composableReflectionNormal);
    float3 normalDy = ddy(composableReflectionNormal);
    float gradient = min(
        max(dot(normalDx, normalDx), dot(normalDy, normalDy)), 1.0);
    float filteredSmoothness = min(
        1.0 - pow(gradient, 0.333), smoothness);
    smoothness += weight * (filteredSmoothness - smoothness);
    #endif
    #if defined(REFLECTION_STATIC)
    float3 reflectionDirection =
        composableSurface.worldPosition + composableReflectionNormal;
    #else
    float3 reflectionDirection = CalculateViewReflectionDirection(
        composableSurface.worldPosition, composableReflectionNormal);
    #endif

    #if defined(REFLECTION_PROBE_BOX_PROJECTION)
    float3 boundsMin = _ReflectionProbeBoundsMin;
    float3 boundsMax = _ReflectionProbeBoundsMax;
    float3 probePosition = _ReflectionProbePosition;
    #if defined(REFLECTION_PROBE_BOX_PROJECTION_OFFSET)
    boundsMin -= _ReflectionProbeBoxProjectionSizeOffset;
    boundsMax += _ReflectionProbeBoxProjectionSizeOffset;
    probePosition += _ReflectionProbeBoxProjectionPositionOffset;
    #endif
    reflectionDirection = BoxProjectReflectionDirection(
        reflectionDirection, composableSurface.worldPosition,
        boundsMin, boundsMax, probePosition);
    #endif

    float3 reflection = SampleReflectionProbePair(
        reflectionDirection, smoothness,
        _ReflectionProbeTexture1, _ReflectionProbeTexture2,
        _LightProbeLightBakeIdA, _LightProbeLightBakeIdB,
        _LightProbeLightBakeIdC, _LightProbeLightBakeIdD,
        _LightProbeLightBakeIdE, _LightProbeLightBakeIdF,
        _ReflectionProbeIntensity);
    #if defined(_PROBE_CALCULATION_PRECISE)
    float grayscale = dot(float3(0.33, 0.33, 0.33), reflection);
    float metallicScale = composableSurface.metallic *
        composableSurface.metallic * 2.5 + 1.0;
    float3 grayscaleDelta = grayscale * metallicScale - reflection;
    float scaledGrayscale = grayscale * metallicScale;
    float minimumColor = min(
        composableSurface.baseColor.r,
        min(composableSurface.baseColor.g, composableSurface.baseColor.b));
    float maximumColor = max(
        composableSurface.baseColor.r,
        max(composableSurface.baseColor.g, composableSurface.baseColor.b));
    float saturation = (maximumColor - minimumColor) / maximumColor;
    float metallicSaturation = saturation * composableSurface.metallic;
    float grayscaleFactor = max(
        metallicSaturation, _ReflectionProbeGrayscale);
    float coloredMetalScale =
        metallicSaturation * _ColoredMetalMultiplier + 1.0;
    reflection += grayscaleFactor * grayscaleDelta;
    float metallicFactor = saturate(
        composableSurface.metallic -
        scaledGrayscale * scaledGrayscale * 0.1);
    metallicFactor *= max(saturation, 0.95);
    reflection *= 1.0 + metallicFactor *
        (composableSurface.baseColor.rgb * coloredMetalScale - 1.0);
    float whiteFactor = (1.0 - saturation) *
        _WhiteOffset * max(_ColoredMetalMultiplier, 1.0);
    reflection *= max(
        whiteFactor * composableSurface.baseColor.rgb *
        composableSurface.metallic, 1.0);
    reflection *= smoothness;
    #if defined(RIM_DIM)
    reflection *= 1.0 - composableRimDim * _RimDarkening;
    #endif
    composableLighting.reflection = reflection;
    #else
    float groundFade = 1.0;
    float3 reflectionBaseColor = composableSurface.baseColor.rgb;
    #if defined(GROUND_FADE)
    groundFade = 1.0 - saturate(
        -composableSurface.worldPosition.y * _GroundFadeScale +
        _GroundFadeOffset);
    reflectionBaseColor *= groundFade;
    #endif
    #if defined(MULTIPLY_REFLECTIONS)
    reflection *= 1.0 + composableSurface.metallic *
        (reflectionBaseColor - 1.0);
    #endif
    reflection *= 2.0 * (composableSurface.metallic * 0.8 + 0.2);
    reflection *= smoothness * groundFade;
    #if defined(RIM_DIM)
    reflection *= 1.0 - composableRimDim * _RimDarkening;
    #endif
    composableLighting.reflection = reflection;
    #endif
    }
    #endif
    #endif
    EmissionData composableEmission = InitializeEmissionData();
    #if defined(BLOOM_FOG) && defined(FOG) && USE_FOG_SUPPRESSION
    float fogSuppression = 0.0;
    #endif
    float4 composableTime = ResolveTime(
        UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset));

    albedo += float4(
        composableLighting.reflection + composableLighting.ambient +
        (composableLighting.directDiffuse + composableLighting.directSpecular),
        0.0);

    #if defined(_HOLOGRAM_GRID) || defined(_HOLOGRAM_SCANLINE) || defined(_HOLOGRAM_LEGACY)
    #if defined(_HOLOGRAM_GRID)
    albedo = ApplyHologram(
        albedo, worldPos, i.hologramObjectPosition, composableTime,
        _HologramGridSize, _HologramScanDistance,
        _HoloIntensity,
        UNITY_ACCESS_INSTANCED_PROP(Props, _HaltScan),
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramStripeSpeed),
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramPhaseOffset),
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramFill),
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramColor));
    #elif defined(_HOLOGRAM_SCANLINE)
    albedo = ApplyHologram(
        albedo, worldPos, 0.0.xxx, composableTime,
        0.0, _HologramScanDistance,
        _HoloIntensity,
        UNITY_ACCESS_INSTANCED_PROP(Props, _HaltScan),
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramStripeSpeed),
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramPhaseOffset),
        0.0,
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramColor));
    #else
    albedo = ApplyHologram(
        albedo, worldPos, 0.0.xxx, composableTime,
        _HologramGridSize, 0.0, 0.0,
        0.0, 0.0, 0.0, 0.0,
        UNITY_ACCESS_INSTANCED_PROP(Props, _HologramColor));
    #endif
    #endif

    #if defined(DISTANCE_DARKENING)
    {
        float3 offset = _DarkeningCenter - worldPos;
        float weightedDistance = dot(
            offset * offset,
            _DarkeningDirection * float3(0.0001, 0.0001, 0.0001));
        albedo.rgb *= 1.0 - saturate(weightedDistance * _DarkeningScale) *
            _DarkeningIntensity;
    }
    #endif

    #if defined(OCCLUSION) && defined(OCCLUSION_BEFORE_EMISSION)
    albedo.rgb *= composableSurface.occlusion;
    #endif
    #if defined(OCCLUSION_DETAIL) && defined(OCCLUSION_BEFORE_EMISSION)
    albedo.rgb *= composableSurface.occlusionDetail;
    #endif

    // EMISSION
    #if defined(ACES_TONE_MAPPING) && defined(_ACES_APPROACH_BEFORE_EMISSIVE)
    albedo = ApplyAcesTonemapping(albedo);
    #endif

    #if defined(_PARALLAX_FLEXIBLE) || defined(_PARALLAX_RGB)
    #if USE_VERTEX_COLOR
    albedo = ApplyParallax(
        albedo, composableSurface, i.color,
        _InputUvMultiplier, UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset),
        _ParallaxTexSpeed, _ParallaxIntensity, _ParallaxIntensity_Step,
        _Layers, _StartOffset, _OffsetStep,
        UNITY_ACCESS_INSTANCED_PROP(Props, _IridescenceColorInfluence),
        _ParallaxMap, _ParallaxMap_ST,
        _ParallaxMaskingMap, _ParallaxMaskingMap_ST,
        _ParallaxMaskSpeed, _ParallaxMaskIntensity,
        _IridescenceAxesMultiplier, _IridescenceTiling,
        UNITY_ACCESS_INSTANCED_PROP(Props, _ParallaxColor));
    #else
    albedo = ApplyParallax(
        albedo, composableSurface, 1.0.xxxx,
        _InputUvMultiplier, UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset),
        _ParallaxTexSpeed, _ParallaxIntensity, _ParallaxIntensity_Step,
        _Layers, _StartOffset, _OffsetStep,
        UNITY_ACCESS_INSTANCED_PROP(Props, _IridescenceColorInfluence),
        _ParallaxMap, _ParallaxMap_ST,
        _ParallaxMaskingMap, _ParallaxMaskingMap_ST,
        _ParallaxMaskSpeed, _ParallaxMaskIntensity,
        _IridescenceAxesMultiplier, _IridescenceTiling,
        UNITY_ACCESS_INSTANCED_PROP(Props, _ParallaxColor));
    #endif
    #endif

    #if USE_EMISSION_TEXTURE_COLOR
    #if defined(_EMISSIONTEXTURE_FLIPBOOK)
    {
        float2 emissionUv = i.flipbookUv * _EmissionTex_ST.xy +
            _EmissionTex_ST.zw;
        float emissionInput = dot(
                tex2D(_EmissionTex, emissionUv), i.flipbookFrameSelector) *
            UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionBrightness);
        #if defined(BLOOM_FOG) && defined(FOG) && USE_FOG_SUPPRESSION
        fogSuppression += emissionInput *
            UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor).a;
        #endif
        composableEmission = ResolvePlainEmission(
            emissionInput.xx,
            UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionTexColor),
            _EmissionTexBloomIntensity);
    }
    albedo.rgb += composableEmission.color;
    albedo.a = composableEmission.bloomAlpha;
    #if USE_VERTEX_EMISSION
    #if defined(BLOOM_FOG) && defined(FOG) && USE_FOG_SUPPRESSION
    float flipbookVertexEmissionFactor = ResolveVertexEmissionFactor(
        i.color.g, _EmissionThreshold, _EmissionStrength);
    fogSuppression += flipbookVertexEmissionFactor * i.emission.a * i.emission.a;
    #endif
    EmissionData flipbookVertexEmission = ResolveVertexEmission(
        i.color, i.emission,
        _EmissionThreshold, _EmissionStrength,
        _BaseColorBoost, _BaseColorBoostThreshold,
        _QuestWhiteboostMultiplier, _EmissionBloomIntensity);
    albedo.rgb += flipbookVertexEmission.color;
    albedo.a += flipbookVertexEmission.bloomAlpha;
    #endif
    #else
    #if defined(COLOR_ARRAY)
    float2 composableColorArrayId = i.colorArrayId;
    #else
    float2 composableColorArrayId = 0.0;
    #endif
    #if defined(EMISSION_ANGLE_DISAPPEAR)
    float composableEmissionAngle = i.emissionAngle;
    #else
    float composableEmissionAngle = 1.0;
    #endif
    #if defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION)
    float composableLookupEmission = i.lookupEmission;
    #else
    float composableLookupEmission = 1.0;
    #endif
    {
        float2 emissionInput = 0.0;
    #if defined(_EMISSION_TEXTURE_SOURCE_MPM_G) && defined(METAL_SMOOTHNESS_TEXTURE)
    emissionInput = composableSurface.mpm.gg;
    #elif defined(_EMISSION_TEXTURE_SOURCE_SDF)
    {
        float sdfMask = 1.0;
        float sdfAccumulator = 0.0;
        for (int pointIndex = 0; pointIndex < 3; pointIndex++)
        {
            float3 pointOffset = composableSurface.worldPosition -
                _SDFPointArray[pointIndex].xyz;
            float distanceSquared = dot(pointOffset, pointOffset);
            float pointDistance = exp2(log2(distanceSquared) * 0.25);
            float isPositive = _SDFPointArray[pointIndex].w > 0.0
                                   ? 1.0
                                   : 0.0;
            float isNegative = _SDFPointArray[pointIndex].w < 0.0
                                   ? 1.0
                                   : 0.0;
            float pointSign = floor(isPositive - isNegative);
            float pointIntensity = pointSign < 0.0
                                       ? _SDFNegativeIntensity
                                       : _SDFPointIntensity;
            float pointDistanceField = max(
                abs(_SDFPointArray[pointIndex].w) - pointDistance, 0.0);
            float accumulated =
                pointDistanceField * pointIntensity + sdfAccumulator;
            float occlusion = saturate(
                -pointDistanceField * pointIntensity + 1.0);
            float masked = sdfMask * occlusion;
            if (pointSign >= 0.0)
                sdfAccumulator = accumulated;
            else
                sdfMask = masked;
        }
        float sdfNoiseScale = UNITY_ACCESS_INSTANCED_PROP(
            Props, _SDFNoiseScale);
        float3 noiseUv = sdfNoiseScale.xxx *
            composableSurface.worldPosition;
        noiseUv += _SDFNoisePanning * composableTime.y + _SDFNoiseOffset;
        float sdfEmission = sdfAccumulator * sdfMask +
            tex3D(_SDFNoiseTex, noiseUv).r *
            UNITY_ACCESS_INSTANCED_PROP(Props, _SDFNoiseIntensity);
        emissionInput = sdfEmission.xx;
    }
    #elif defined(_EMISSIONTEXTURE_PULSE)
    { 
    #if defined(SECONDARY_UVS_PULSE) && USE_SECONDARY_UV
    float2 pulseUv = TransformSecondaryUv(
        composableSurface, _PulseMask_ST);
    #else
    float2 baseUv = composableSurface.uv0 * _InputUvMultiplier;
    float2 pulseUv = baseUv * _PulseMask_ST.xy + _PulseMask_ST.zw;
    #endif
    float pulseTexture = tex2D(_PulseMask, pulseUv).r;
    #if defined(INVERT_PULSE)
    pulseTexture = 1.0 - pulseTexture;
    #endif
    float pulsePhase = frac(
        pulseTexture - composableTime.x * _PulseSpeed);
    float pulseDistance = min(pulsePhase, 1.0 - pulsePhase);
    float pulse = 1.0 - smoothstep(
        max(_PulseWidth, 0.0),
        max(_PulseWidth + _PulseSmooth, 0.00001),
        pulseDistance);
    #if defined(PULSE_MULTIPLY_TEXTURE)
    pulse *= pulseTexture;
    #endif
    emissionInput = pulse.xx;
        }
    #elif defined(_EMISSIONTEXTURE_SIMPLE)
    #if defined(DISTORTION_SIMPLE) && \
            defined(_DISTORTION_TARGET_EMISSIONTEX)
    { 
    #if defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV
    float2 distortionUv = TransformScrollingSecondaryUv(
        composableSurface, _DistortionTex_ST,
        _DistortionPanning * 0.1, composableTime.y);
    #else
    float2 baseUv = composableSurface.uv0 * _InputUvMultiplier;
    float2 distortionUv = baseUv * _DistortionTex_ST.xy +
        _DistortionTex_ST.zw;
    distortionUv += composableTime.yy * _DistortionPanning *
        _DistortionTex_ST.xy * 0.1;
    #endif
    float2 distortion = tex2D(_DistortionTex, distortionUv).rg;
    distortion = distortion *
        UNITY_ACCESS_INSTANCED_PROP(Props, _DistortionStrength) *
        0.1 * _DistortionAxes * 2.0 - 1.0;
    #if defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV
    float2 emissionUv = TransformScrollingSecondaryUv(
        composableSurface, _EmissionTex_ST,
        _EmissionTexSpeed, composableTime.x);
    emissionUv += distortion * _EmissionTex_ST.xy *
        composableSurface.secondaryUvTiling;
    #else
    float2 emissionUv = (baseUv + distortion) * _EmissionTex_ST.xy +
        _EmissionTex_ST.zw;
    emissionUv += composableTime.xx * _EmissionTexSpeed *
        _EmissionTex_ST.xy;
    #endif
    emissionInput = tex2D(_EmissionTex, emissionUv).rg;
        }
    #else
    { 
    #if defined(SECONDARY_UVS_EMISSION) && USE_SECONDARY_UV
    float2 emissionUv = TransformScrollingSecondaryUv(
        composableSurface, _EmissionTex_ST,
        _EmissionTexSpeed, composableTime.x);
    #else
    float2 baseUv = composableSurface.uv0 * _InputUvMultiplier;
    #if defined(_SECONDARY_UVS_ADDITIVE_OFFSET) && defined(SECONDARY_UVS_EMISSION)
    baseUv += UNITY_ACCESS_INSTANCED_PROP(Props, _AdditiveUVOffset).xy;
    #endif
    float2 emissionUv = baseUv * _EmissionTex_ST.xy +
        _EmissionTex_ST.zw;
    emissionUv += composableTime.xx * _EmissionTexSpeed *
        _EmissionTex_ST.xy;
    #endif
    emissionInput = tex2D(_EmissionTex, emissionUv).rg;
        }
    #endif
    #endif

    #if defined(EMISSION_ANGLE_DISAPPEAR)
    emissionInput *= composableEmissionAngle;
    #endif

    #if defined(_EMISSION_ALPHA_SOURCE_COPY_EMISSION)
    emissionInput.y = emissionInput.x;
    #elif defined(_EMISSION_ALPHA_SOURCE_MPM_R) && defined(METAL_SMOOTHNESS_TEXTURE)
    emissionInput.y = composableSurface.mpm.r;
    #endif
    #if defined(EMISSION_MASK)
    { 
    #if defined(SECONDARY_UVS_EMISSION_MASK) && USE_SECONDARY_UV
    float2 maskUv = TransformScrollingSecondaryUv(
        composableSurface, _EmissionMask_ST,
        _EmissionMaskSpeed, composableTime.x);
    #else
    float2 maskBaseUv = composableSurface.uv0 * _InputUvMultiplier;
    float2 maskUv = maskBaseUv * _EmissionMask_ST.xy +
        _EmissionMask_ST.zw;
    maskUv += composableTime.xx * _EmissionMaskSpeed *
        _EmissionMask_ST.xy;
    #endif
    float2 mask = tex2D(_EmissionMask, maskUv).rg;
    float emissionMaskIntensity = UNITY_ACCESS_INSTANCED_PROP(
        Props, _EmissionMaskIntensity);
    #if defined(_MASKBLEND_ADD)
    emissionInput += mask * emissionMaskIntensity;
    #elif defined(_MASKBLEND_MASKED_ADD)
    emissionInput += emissionInput * mask * emissionMaskIntensity;
    #else
    emissionInput *= mask * emissionMaskIntensity +
        (1.0 - emissionMaskIntensity);
    #endif
        }
    #endif
    #if defined(SECONDARY_EMISSION_MASK)
    { 
    #if defined(SECONDARY_UVS_EMISSION_MASK2) && USE_SECONDARY_UV
    float2 maskUv = TransformScrollingSecondaryUv(
        composableSurface, _SecondaryEmissionMask_ST,
        _SecondaryEmissionMaskSpeed, composableTime.x);
    #else
    float2 maskBaseUv = composableSurface.uv0 * _InputUvMultiplier;
    float2 maskUv = maskBaseUv * _SecondaryEmissionMask_ST.xy +
        _SecondaryEmissionMask_ST.zw;
    maskUv += composableTime.xx * _SecondaryEmissionMaskSpeed *
        _SecondaryEmissionMask_ST.xy;
    #endif
    float2 mask = tex2D(_SecondaryEmissionMask, maskUv).rg;
    float secondaryEmissionMaskIntensity =
        UNITY_ACCESS_INSTANCED_PROP(
            Props, _SecondaryEmissionMaskIntensity);
    #if defined(_SECONDARY_MASK_BLEND_ADD)
    emissionInput += mask * secondaryEmissionMaskIntensity;
    #elif defined(_SECONDARY_MASK_BLEND_MASKED_ADD)
    emissionInput += emissionInput * mask *
        secondaryEmissionMaskIntensity;
    #else
    emissionInput *= mask * secondaryEmissionMaskIntensity +
        (1.0 - secondaryEmissionMaskIntensity);
    #endif
        }
    #endif
    emissionInput *= UNITY_ACCESS_INSTANCED_PROP(
        Props, _EmissionBrightness);
    #if defined(_VERTEXMODE_EMISSIVE_MULT_ADD)
    emissionInput *= i.color.a;
    #endif

    #if defined(COLOR_ARRAY)
    float emissionColorIndex = round(
        composableColorArrayId.x * 10.0 + composableColorArrayId.y);
    float4 emissionColor = _ColorsArray[emissionColorIndex];
    #else
    float4 emissionColor = UNITY_ACCESS_INSTANCED_PROP(
        Props, _EmissionTexColor);
    #endif
    #if defined(BLOOM_FOG) && defined(FOG) && USE_FOG_SUPPRESSION
    fogSuppression += emissionInput.r * emissionColor.a;
    #endif
    #if defined(TEXTURE3D_LOOKUP) && defined(TEXTURE3D_EMISSION)
    emissionColor.a *= composableLookupEmission;
    #endif

    #if defined(_EMISSIONCOLORTYPE_GRADIENT)
    composableEmission = InitializeEmissionData();
    float gradientPhase = frac(
        _EmissionGradientPanningSpeed * composableTime.x +
        UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionGradientPosition));
    float2 gradientUv = float2(emissionInput.y, gradientPhase) *
        _EmissionGradientTex_ST.xy;
    float3 gradient = tex2D(_EmissionGradientTex, gradientUv).rgb;
    float gradientIntensity = UNITY_ACCESS_INSTANCED_PROP(
        Props, _EmissionGradientIntensity);
    composableEmission.color = gradientIntensity *
        saturate(emissionInput.x * gradient);
    composableEmission.bloomAlpha =
        _EmissionTexBloomIntensity * gradientIntensity;
    #elif defined(_EMISSIONCOLORTYPE_WHITEBOOST) || \
            (defined(_EMISSIONCOLORTYPE_MAINEFFECT) && !defined(POST_BLOOM))
    float3 emissionRgb = emissionInput.r * emissionColor.rgb;
    float bloomValue = emissionInput.g * emissionInput.g * emissionColor.a;
    float bloomAlpha = bloomValue * 3.5 * _EmissionTexBloomIntensity;
    emissionRgb = CalculateBloomComposition(
        emissionRgb, emissionColor.a, bloomValue,
        _EmissionTexWhiteBoostMultiplier,
        _BaseColorBoost, _BaseColorBoostThreshold);
    float4 configured = float4(emissionRgb, bloomAlpha);
    composableEmission = InitializeEmissionData();
    composableEmission.color = configured.rgb;
    composableEmission.bloomAlpha = configured.a;
    #else
    composableEmission = ResolvePlainEmission(
        emissionInput, emissionColor, _EmissionTexBloomIntensity);
    #endif
    }
    albedo.rgb += composableEmission.color;
    albedo.a += composableEmission.bloomAlpha;
    #endif
    #endif

    #if USE_VERTEX_EMISSION && !defined(_EMISSIONTEXTURE_FLIPBOOK)
    #if defined(BLOOM_FOG) && defined(FOG) && USE_FOG_SUPPRESSION
    float vertexEmissionFactor = ResolveVertexEmissionFactor(
        i.color.g, _EmissionThreshold, _EmissionStrength);
    fogSuppression += vertexEmissionFactor * i.emission.a * i.emission.a;
    #endif
    EmissionData composableVertexEmission = ResolveVertexEmission(
        i.color, i.emission,
        _EmissionThreshold, _EmissionStrength,
        _BaseColorBoost, _BaseColorBoostThreshold,
        _QuestWhiteboostMultiplier, _EmissionBloomIntensity);
    albedo.rgb += composableVertexEmission.color;
    albedo.a += composableVertexEmission.bloomAlpha;
    #endif

    #if defined(_RIMLIGHT_ADDITIVE)
    {
        float rimLight = CalculateRimLightMask(
            worldPos, worldNormal, _RimLightEdgeStart,
            _RimPerpendicularAxis);
        float4 rimLightColor = UNITY_ACCESS_INSTANCED_PROP(
            Props, _RimLightColor);
        float rimLightScale = rimLightColor.a * _RimLightIntensity;
        float3 rimTarget = ResolveRimLightTarget(
            rimLightColor.rgb, rimLightScale, rimLight,
            _RimLightWhiteboostMultiplier,
            _BaseColorBoost, _BaseColorBoostThreshold);
        albedo.rgb += rimTarget * rimLight * rimLightScale;
        albedo.a = rimLight * rimLightScale * _RimLightBloomIntensity;
    }
    #endif

    #if defined(OCCLUSION) && !defined(OCCLUSION_BEFORE_EMISSION)
    albedo *= composableSurface.occlusion;
    #endif
    #if defined(OCCLUSION_DETAIL) && !defined(OCCLUSION_BEFORE_EMISSION)
    albedo *= composableSurface.occlusionDetail;
    #endif

    #if defined(_RIMLIGHT_LERP)
    #if defined(UV_COLOR_SEGMENTS)
    albedo = ApplyRimLight(
        albedo, worldPos, worldNormal, _RimLightEdgeStart,
        i.uvSegmentRimColor, _RimLightIntensity,
        _RimLightBloomIntensity,
        _RimPerpendicularAxis, _RimLightWhiteboostMultiplier,
        _BaseColorBoost, _BaseColorBoostThreshold);
    #else
    albedo = ApplyRimLight(
        albedo, worldPos, worldNormal, _RimLightEdgeStart,
        UNITY_ACCESS_INSTANCED_PROP(Props, _RimLightColor),
        _RimLightIntensity, _RimLightBloomIntensity,
        _RimPerpendicularAxis, _RimLightWhiteboostMultiplier,
        _BaseColorBoost, _BaseColorBoostThreshold);
    #endif
    #endif

    #if defined(ACES_TONE_MAPPING) && !defined(_ACES_APPROACH_BEFORE_EMISSIVE)
    albedo = ApplyAcesTonemapping(albedo);
    #endif

    #if defined(HIGHLIGHT_SELECTION)
    {
        float pulse = frac(
            composableTime.w * 0.15 + worldPos.x * 0.2 + worldPos.y);
        pulse = max(1.0 - pulse * 5.0, 0.0);
        pulse = pulse * pulse * (3.0 - 2.0 * pulse);
        pulse *= saturate(100.0 - 100.0 * pulse);
        pulse = saturate(0.4 * pulse * pulse * i.highlightSelection);
        albedo.rgb += pulse;
    }
    #endif

    #if defined(COLOR_BY_FOG) && !(defined(BLOOM_FOG) && defined(FOG)) && \
        !defined(_HOLOGRAM_GRID) && !defined(_HOLOGRAM_LEGACY)
    albedo = ApplyLitColorFog(
        albedo, worldPos, _ColorFogMultiplier, _ColorFogMax,
        _ColorFogHighlightMultiplier, _ColorFogInfluence,
        _FogHeightScale, _FogHeightOffset);
    #endif

    // Terminal fog composition keeps bloom fog, height fog, and blue-noise
    // dithering in separate passes.
    #if defined(BLOOM_FOG) && defined(FOG)
    float customFogScale = _FogScale;
    #if USE_FOG_SUPPRESSION
    #if defined(POST_BLOOM)
    fogSuppression *= _MainEffectFogSuppression;
    #else
    fogSuppression *= _EmissionFogSuppression;
    #endif
    customFogScale *= 1.0 - fogSuppression;
    #endif
    #if defined(HEIGHT_FOG)
    float customFogFactor = CalculateCustomFogFactor(
        distanceSquared(worldPos), _FogStartOffset, customFogScale);
    #if defined(HEIGHT_FOG_DEPTH_SOFTEN)
    float cameraDistance = length(worldPos - GetStereoAwareCameraPosition());
    float heightInput = worldPos.y *
        (_FogHeightScale / (cameraDistance * _FogSoften * 0.01));
    heightInput += _FogHeightOffset -
        cameraDistance * _FogSoftenOffset * 0.001;
    #else
    float heightInput = worldPos.y * _FogHeightScale + _FogHeightOffset;
    #endif
    heightInput -=
        CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME + CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME;
    heightInput = clamp(
        heightInput / CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME, 0.0, 1.0);
    float customHeightFogFactor =
        heightInput * heightInput * (3.0 - 2.0 * heightInput);
    #if defined(COLOR_BY_FOG)
    {
        float3 bloomColor = SampleBloomPrePass(i.screenPos).rgb;
        float3 colorFog = bloomColor * _ColorFogMultiplier;
    #if defined(FOG_COLOR_HIGHLIGHT)
    float bloomMaximum = max(
        bloomColor.r, max(bloomColor.g, bloomColor.b));
    float highlight = bloomMaximum * bloomMaximum;
    highlight *= bloomMaximum;
    highlight *= _ColorFogHighlightMultiplier;
    highlight = min(highlight * bloomMaximum, _ColorFogMax);
    colorFog = min(colorFog * (1.0 + highlight), _ColorFogMax);
    #else
    colorFog = min(colorFog, _ColorFogMax);
    #endif

    float4 fogTarget = min(float4(colorFog, 0.0), _ColorFogMax);
    float4 fogSource = float4(
        albedo.rgb * _ColorFogInfluence + colorFog, albedo.a);
    float blendFactor =
        1.0 - customHeightFogFactor * (1.0 - customFogFactor);
    albedo = fogSource + blendFactor * (fogTarget - fogSource);
    }
    #else
    albedo = ApplyBloomHeightFogCalculatedFactor(
        albedo, i.screenPos, customFogFactor, customHeightFogFactor);
    #endif
    #else
    albedo = ApplyBloomFog(
        albedo, i.screenPos, worldPos, _FogStartOffset, customFogScale);
    #endif
    #elif defined(FOG) && defined(HEIGHT_FOG) && \
        !defined(COLOR_BY_FOG) && \
        !defined(_HOLOGRAM_GRID) && !defined(_HOLOGRAM_LEGACY)
    #if defined(HEIGHT_FOG_DEPTH_SOFTEN)
    {
        float cameraDistance = length(
            worldPos - GetStereoAwareCameraPosition());
        float exactHeightInput = worldPos.y *
            (_FogHeightScale / (cameraDistance * _FogSoften * 0.01));
        exactHeightInput += _FogHeightOffset -
            cameraDistance * _FogSoftenOffset * 0.001;
        albedo = ApplyHeightFogCurve(albedo, exactHeightInput);
    }
    #else
    {
        float exactHeightInput =
            worldPos.y * _FogHeightScale + _FogHeightOffset;
        albedo = ApplyHeightFogCurve(albedo, exactHeightInput);
    }
    #endif
    #endif

    #if defined(NOISE_DITHERING)
    #if USE_NOISE_SCREEN_POSITION
    albedo = ApplyNoiseDither(albedo, i.noiseScreenPos, _GlobalBlueNoiseTex);
    #else
    albedo = ApplyNoiseDither(albedo, 0.0, _GlobalBlueNoiseTex);
    #endif
    #endif

    // Dissolve-color routes apply the final edge-color
    // blend after fog and blue-noise dithering.
    #if defined(DISSOLVE) && defined(DISSOLVE_COLOR)
    albedo.rgb = lerp(
        albedo.rgb,
        _DissolveColorIntensity *
        UNITY_ACCESS_INSTANCED_PROP(Props, _DissolveColor).rgb,
        dissolveFactor);
    #endif
    return albedo;
    #endif
}
