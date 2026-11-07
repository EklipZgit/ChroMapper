Shader "ChroMapper/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [Toggle(AVATAR_COMPUTE_SKINNING)] _AvatarComputeSkinning ("Avatar Compute Skinning", Float) = 0
        [KeywordEnum(None, Import, External Scale, Object Space, Additive Offset)] _Secondary_UVs ("Secondary UVs", Float) = 0
        [ShowIfAny(_SECONDARY_UVS_IMPORT)] _InstancedSecondaryTiling ("Secondary UV Tiling", Vector) = (1,1,0,0)
        [ShowIfAny(_SECONDARY_UVS_EXTERNAL_SCALE, _SECONDARY_UVS_IMPORT)] _InstancedSecondaryOffset ("Secondary UV Offset", Vector) = (0,0,0,0)
        [ShowIfAny(_SECONDARY_UVS_EXTERNAL_SCALE, _SECONDARY_UVS_OBJECT_SPACE)] _UVScale ("UV Scale", Vector) = (1,1,1,1)
        [ShowIfAny(_SECONDARY_UVS_ADDITIVE_OFFSET)] _AdditiveUVOffset ("UV Offset", Vector) = (0,0,0,0)
        [VectorShowIfAny(2)] _InputUvMultiplier ("UV Multiplier", Vector) = (1,1,0,0)
        [Header(BASE PROPERTIES)] [Space(12)] [Toggle(METAL_SMOOTHNESS_TEXTURE)] _EnableMetalSmoothnessTex ("Multi Purpose Map", Float) = 0
        [ShowIfAny(METAL_SMOOTHNESS_TEXTURE)] _MetalSmoothnessTex ("MPM Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_MPM, 2, 0_SECONDARY_UVS_NONE, METAL_SMOOTHNESS_TEXTURE)] _SecondaryUVsMPM ("MPM Secondary UVs", Float) = 0
        [ToggleShowIfAny(MPM_CUSTOM_MIP, METAL_SMOOTHNESS_TEXTURE)] _EnableCustomMPMMip ("Mip map bias", Float) = 0
        [ShowIfAny(2, METAL_SMOOTHNESS_TEXTURE, MPM_CUSTOM_MIP)] _MpmMipBias ("MPM mipmap bias", Range(-3, 0)) = 0
        [Space(12)] [KeywordEnum(None, MPM R, MPM_A, MPM Avatar B)] _Metallic_Texture ("Metallic Source", Float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 1
        [Space(12)] [KeywordEnum(None, MPM A, MPM G Roughness)] _Smoothness_Texture ("Smoothness Source", Float) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Space(12)] [Toggle(SPECULAR_ANTIFLICKER)] _SpecularAntiflicker ("Smoothness Anti-Flicker", Float) = 0
        [ShowIfAny(SPECULAR_ANTIFLICKER)] _AntiflickerStrength ("Antiflicker Strength", Range(0, 1)) = 0.7
        [ShowIfAny(SPECULAR_ANTIFLICKER)] _AntiflickerDistanceScale ("Antiflicker Distance Scale", Float) = 0.1
        [ShowIfAny(SPECULAR_ANTIFLICKER)] _AntiflickerDistanceOffset ("Antiflicker Distance Offset", Float) = 21
        [Space(12)] [Toggle(PRECISE_NORMAL)] _PreciseNormal ("Precise Normal", Float) = 0
        [Space(18)] [KeywordEnum(None, Color, Emission, MetalSmoothness, Special, Displacement, Emissive Mult Add)] _VertexMode ("Vertex Color Mode", Float) = 0
        [Space(14)] [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionThreshold ("Emission Threshold", Range(0, 1)) = 0
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionColor ("Emission Color", Color) = (1,1,1,0)
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionStrength ("Emission Strength", Float) = 1
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _EmissionBloomIntensity ("Bloom Intensity", Float) = 1
        [EnumShowIfAny(3, None, MainEffect, Always, _VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _Vertex_WhiteBoostType ("Vertex Color Treatment", Float) = 0
        [ShowIfAny(_VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL, _VERTEXMODE_EMISSIVE_MULT_ADD)] _QuestWhiteboostMultiplier ("Whiteboost Multiplier", Float) = 1
        [ToggleShowIfAny(DISPLACEMENT_SPATIAL, _VERTEXMODE_DISPLACEMENT)] _DisplacementSpatial ("RGB Direction", Float) = 0
        [ToggleShowIfAny(DISPLACEMENT_BIDIRECTIONAL, 2, DISPLACEMENT_SPATIAL, _VERTEXMODE_DISPLACEMENT)] _DisplacementBidirectional ("RGB Bidirectional", Float) = 0
        [EnumShowIfAny(3, None, Flat, Full, _VERTEXMODE_DISPLACEMENT)] _Spectrogram ("Spectrogram", Float) = 0
        [ShowIfAny(_VERTEXMODE_DISPLACEMENT)] _DisplacementStrength ("Displacement Strength", Float) = 0.1
        [ShowIfAny(_VERTEXMODE_DISPLACEMENT)] _DisplacementAxisMultiplier ("Axis Multiplier", Vector) = (1,1,1,1)
        _EnableVertexDisplacementMask ("Vertex Displacement Mask", Float) = 0
        _VertexDisplacement_Mask_Source ("Mask Source", Float) = 0
        _VertexDisplacementMask ("Mask Texture", 2D) = "white" {}
        _VertexDisplacementMaskSpeed ("Mask Texture Speed", Vector) = (0,1,0,0)
        _VertexDisplacementMaskMode ("Mask Mode", Float) = 0
        _VertexDisplacementMaskMultiplier ("Mask Multiplier", Float) = 1
        _VertexDisplacementMaskOffset ("Mask Offset", Float) = 0
        _VertexDisplacement3DTexture ("Noise Tex", 3D) = "white" {}
        _VertexDisplacement3DTexOffset ("Texture Offset", Vector) = (0,0,0,0)
        _VertexDisplacement3DTexPanning ("Texture Panning", Vector) = (0,0,0,0)
        _VertexDisplacement3DTexScale ("Texture Scale", Float) = 5
        [Header(EMISSIONS)] [Space(18)] [KeywordEnum(None, Simple, Pulse, Flipbook)] _EmissionTexture ("Texture Emission", Float) = 0
        [EnumShowIfAny(4, Texture, Fill, MPM G, SDF, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_FLIPBOOK, _EMISSION_TEXTURE_SOURCE_SDF)] _Emission_Texture_Source ("Emission Source", Float) = 0
        [ShowIfAny(1, _EMISSION_TEXTURE_SOURCE_TEXTURE, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionTex ("Emission Texture", 2D) = "white" {}
        [VectorShowIfAny(2, 2, _EMISSIONTEXTURE_SIMPLE, _EMISSION_TEXTURE_SOURCE_TEXTURE)] _EmissionTexSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [ToggleShowIfAny(SECONDARY_UVS_EMISSION, 2, 0_SECONDARY_UVS_NONE, _EMISSION_TEXTURE_SOURCE_TEXTURE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryUVsEmissionTex ("Use Secondary UVs", Float) = 0
        [EnumShowIfAny(3, Emission G, Copy Emission, MPM R, _EMISSIONTEXTURE_SIMPLE)] _Emission_Alpha_Source ("Alpha Source", Float) = 0
        _EmissionBrightness ("Brightness", Float) = 1
        [ToggleShowIfAny(TEXTURE3D_EMISSION, 1, TEXTURE3D_LOOKUP)] _LookupTextureEmission ("Use Texture3D Emission", Float) = 0
        [ToggleShowIfAny(EMISSION_ANGLE_DISAPPEAR, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EnableEmissionAngleDisappear ("Angle Disappear", Float) = 0
        [ShowIfAny(1, EMISSION_ANGLE_DISAPPEAR, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionThresholdAngle ("Threshold Angle", Float) = 0
        [Space(6)] [EnumShowIfAny(4, Flat, Whiteboost, Gradient, MainEffect, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionColorType ("Emission Color Treatment", Float) = 0
        [ShowIfAny(1, 0_EMISSIONCOLORTYPE_GRADIENT, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionTexColor ("Emission Color", Color) = (1,1,1,1)
        [ShowIfAny(1, _EMISSIONCOLORTYPE_GRADIENT, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK)] _EmissionGradientTex ("Gradient LUT", 2D) = "white" {}
        [ShowIfAny(_EMISSIONCOLORTYPE_GRADIENT)] _EmissionGradientPosition ("LUT Position", Float) = 0.5
        [ShowIfAny(_EMISSIONCOLORTYPE_GRADIENT)] _EmissionGradientPanningSpeed ("LUT Panning", Float) = 0
        [ShowIfAny(_EMISSIONCOLORTYPE_GRADIENT)] _EmissionGradientIntensity ("LUT Intensity", Float) = 1
        _EmissionTexBloomIntensity ("Bloom Intensity", Float) = 1
        _EmissionTexWhiteBoostMultiplier ("Whiteboost multiplier", Float) = 1
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseMask ("Pulse Mask", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_PULSE, 2, 0_SECONDARY_UVS_NONE, _EMISSIONTEXTURE_PULSE)] _SecondaryUVsPulseTex ("Pulse Texture Secondary UVs", Float) = 0
        [ToggleShowIfAny(INVERT_PULSE, _EMISSIONTEXTURE_PULSE)] _InvertPulseTexture ("Invert Texture", Float) = 0
        [ToggleShowIfAny(PULSE_MULTIPLY_TEXTURE, _EMISSIONTEXTURE_PULSE)] _PulseMultiplyByTexture ("Brightness from Texture", Float) = 0
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseWidth ("Pulse Width", Float) = 0.1
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseSpeed ("Pulse Speed", Float) = 0.2
        [ShowIfAny(_EMISSIONTEXTURE_PULSE)] _PulseSmooth ("Pulse Smooth", Range(0, 0.2)) = 0.02
        [Space(12)] [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookColumns ("Flipbook Columns", Float) = 8
        [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookRows ("Flipbook Rows", Float) = 8
        [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookNonloopableFrames ("Full Non-loopable frames", Float) = 0
        [ShowIfAny(_EMISSIONTEXTURE_FLIPBOOK)] _FlipbookSpeed ("Flipbook Speed", Float) = 1
        [ToggleShowIfAny(FLIPBOOK_BLENDING_OFF, _EMISSIONTEXTURE_FLIPBOOK)] _FlipbookBlendingOff ("No Frame Blending", Float) = 0
        [ToggleShowIfAny(EMISSION_MASK, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE)] _EnableEmissionMask ("Layer 2", Float) = 0
        [EnumShowIfAny(3, Multiply, Add, Masked Add, EMISSION_MASK)] _MaskBlend ("Layer Blend", Float) = 0
        [ShowIfAny(1, EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMask ("Layer Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_EMISSION_MASK, 2, 0_SECONDARY_UVS_NONE, EMISSION_MASK)] _SecondaryUVsMask ("Use Secondary UVs", Float) = 0
        [VectorShowIfAny(2, 1, EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskSpeed ("Layer Texture Speed", Vector) = (0,1,0,0)
        [ShowIfAny(1, EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskIntensity ("Layer Intensity", Float) = 1
        [ToggleShowIfAny(SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE)] _EnableSecondaryEmissionMask ("Layer 3", Float) = 0
        [EnumShowIfAny(3, Multiply, Add, Masked Add, SECONDARY_EMISSION_MASK)] _Secondary_Mask_Blend ("Layer Blend", Float) = 0
        [ShowIfAny(1, SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryEmissionMask ("Layer Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_EMISSION_MASK2, 2, 0_SECONDARY_UVS_NONE, SECONDARY_EMISSION_MASK)] _SecondaryUVsMask2 ("Use Secondary UVs", Float) = 0
        [VectorShowIfAny(2, 1, SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryEmissionMaskSpeed ("Texture Speed", Vector) = (0,1,0,0)
        [ShowIfAny(1, SECONDARY_EMISSION_MASK, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _SecondaryEmissionMaskIntensity ("Layer Intensity", Float) = 1
        [Space(12)] [EnumShowIfAny(4 , None, Mask, Secondary Mask, Emission Texture, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _Emission_Step ("Step Emission", Float) = 0
        [ShowIfAny(_EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskStepValue ("Step Value", Range(0, 1)) = 0.5
        [ShowIfAny(_EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_SIMPLE)] _EmissionMaskStepWidth ("Step Width", Range(0, 0.5)) = 0.1
        [Space(12)] [KeywordEnum(None, Flexible, RGB)] _Parallax ("Parallax Emission", Float) = 0
        [ToggleShowIfAny(_PARALLAX_FLEXIBLE_REFLECTED, 0_PARALLAX_NONE)] _EnableReflectedDir ("Reflected Direction", Float) = 0
        [EnumShowIfAny(2, Planar, Warped, 0_PARALLAX_NONE)] _Parallax_Projection ("Parallax Projection", Float) = 0
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxColor ("Parallax Color", Color) = (1,1,1,1)
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxMap ("Parallax Map", 2D) = "black" {}
        [ToggleShowIfAny(SECONDARY_UVS_PARALLAX, 2, 0_SECONDARY_UVS_NONE, 0_PARALLAX_NONE)] _SecondaryUVsParallax ("Parallax Texture Secondary UVs", Float) = 0
        [VectorShowIfAny(2, 0_PARALLAX_NONE)] _ParallaxTexSpeed ("Parallax Speed", Vector) = (0,0,0,0)
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxIntensity ("Parallax Intensity", Float) = 1
        [ShowIfAny(0_PARALLAX_NONE)] _ParallaxIntensity_Step ("Parallax Intensity Step", Float) = -0.25
        [ShowIfAny(_PARALLAX_FLEXIBLE)] _Layers ("Layers", Range(2, 6)) = 3
        [ShowIfAny(0_PARALLAX_NONE)] _StartOffset ("Start Offset", Float) = 1
        [ShowIfAny(0_PARALLAX_NONE)] _OffsetStep ("Offset Step", Float) = 1
        [ToggleShowIfAny(PARALLAX_IRIDESCENCE, 0_PARALLAX_NONE)] _Parallax_Iridescence ("Iridescence", Float) = 0
        [ShowIfAny(2, 0_PARALLAX_NONE, PARALLAX_IRIDESCENCE)] _IridescenceAxesMultiplier ("Axes Multiplier", Vector) = (1,2,3,0)
        [ShowIfAny(2, 0_PARALLAX_NONE, PARALLAX_IRIDESCENCE)] _IridescenceTiling ("Iridescence Tiling", Float) = 0.25
        [ShowIfAny(2, 0_PARALLAX_NONE, PARALLAX_IRIDESCENCE)] _IridescenceColorInfluence ("Color Influence", Range(0, 1)) = 0
        [EnumShowIfAny(3, None, Texture, Vertex Color, 0_PARALLAX_NONE)] _Parallax_Masking ("Mask by", Float) = 0
        [ShowIfAny(2, 0_PARALLAX_NONE, _PARALLAX_MASKING_TEXTURE)] _ParallaxMaskingMap ("Parallax Mask", 2D) = "white" {}
        [VectorShowIfAny(2, 2, 0_PARALLAX_NONE, _PARALLAX_MASKING_TEXTURE)] _ParallaxMaskSpeed ("Mask Speed", Vector) = (0,0,0,0)
        [ShowIfAny(2, 0_PARALLAX_NONE, _PARALLAX_MASKING_TEXTURE)] _ParallaxMaskIntensity ("Mask Intensity", Range(0, 1)) = 1
        [Space(12)] [KeywordEnum(None, Lerp, Additive)] _RimLight ("Rim Light Type", Float) = 0
        [ToggleShowIfAny(RIMLIGHT_INVERT, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _InvertRimlight ("Invert Rimlight", Float) = 0
        [ToggleShowIfAny(DIRECTIONAL_RIM, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _EnableDirectionalRim ("Make Rim Directional", Float) = 0
        [VectorShowIfAny(3, 1, DIRECTIONAL_RIM, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimPerpendicularAxis ("Rim Perpendicular Axis", Vector) = (0,1,0,0)
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightEdgeStart ("Rim Light Edge Start", Float) = 0.5
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightColor ("Rim Light Color", Color) = (1,1,1,0)
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightIntensity ("Rim Light Intensity", Float) = 1
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightBloomIntensity ("Rim Light Bloom Intensity", Float) = 1
        [EnumShowIfAny(3, None, MainEffect, Always, _RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _Rim_WhiteBoostType ("Rimlight Color Treatment", Float) = 0
        [ShowIfAny(_RIMLIGHT_LERP, _RIMLIGHT_ADDITIVE)] _RimLightWhiteboostMultiplier ("Rim Light Whiteboost Multiplier", Float) = 1
        [Header(LIGHTNING)] [Header(Ambient)] [Space(8)] _AmbientMinimalValue ("Ambient Minimum", Range(0, 1)) = 0
        _NominalDiffuseLevel ("Ambient Color", Color) = (0,0,0,0)
        _AmbientMultiplier ("Ambient Color Multiplier", Float) = 1
        [Space(18)] [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", Float) = 1
        [ToggleShowIfAny(LIGHT_FALLOFF, DIFFUSE, SPECULAR)] _EnableLightFalloff ("Light Falloff", Float) = 0
        [ToggleShowIfAny(INVERT_DIFFUSE_NORMAL, DIFFUSE)] _InvertDiffuseNormal ("Invert Diffuse Normal", Float) = 0
        [ToggleShowIfAny(BOTH_SIDES_DIFFUSE, DIFFUSE)] _EnableBothSidesDiffuse ("Both Sides Diffuse", Float) = 0
        [ShowIfAny(2, BOTH_SIDES_DIFFUSE, DIFFUSE)] _BothSidesDiffuseMultiplier ("Far Side Multiplier", Float) = 1
        [Space(12)] [Toggle(PRIVATE_POINT_LIGHT)] _PrivatePointLight ("Private Point Light", Float) = 0
        [ToggleShowIfAny(INSTANCED_PRIVATE_POINT_LIGHT, PRIVATE_POINT_LIGHT)] _InstancedPrivatePointLightColor ("Instance Color", Float) = 0
        [ShowIfAny(PRIVATE_POINT_LIGHT)] [HDR] _PrivatePointLightColor ("Color", Color) = (1,0,0,0)
        [ToggleShowIfAny(POINT_LIGHT_IS_LOCAL, PRIVATE_POINT_LIGHT)] _PointLightPositionLocal ("Make Position Local", Float) = 0
        [ShowIfAny(PRIVATE_POINT_LIGHT)] _PrivatePointLightIntensity ("Intensity Multiplier", Float) = 1
        [ShowIfAny(PRIVATE_POINT_LIGHT)] _PrivatePointLightPosition ("Light World Position", Vector) = (0,0,0,1)
        [Space(12)] [Toggle(DIFFUSE_TEXTURE)] _EnableDiffuseTexture ("Albedo Texture", Float) = 0
        [EnumShowIfAny(3, Texture, MPM R, MPM A Smoothness, DIFFUSE_TEXTURE)] _Diffuse_Texture_Source ("Diffuse Texture Source", Float) = 0
        [ShowIfAny(2, DIFFUSE_TEXTURE, _DIFFUSE_TEXTURE_SOURCE_TEXTURE)] _DiffuseTex ("Diffuse Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_DIFFUSE, 2, 0_SECONDARY_UVS_NONE, DIFFUSE_TEXTURE)] _SecondaryUVsDiffuse ("Diffuse Texture Secondary UVs", Float) = 0
        [ShowIfAny(2, DIFFUSE_TEXTURE, _DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS)] _AlbedoMultiplier ("Albedo multiplier", Float) = 1
        [Space(12)] [Toggle(SPECULAR)] _EnableSpecular ("Specular", Float) = 1
        [ShowIfAny(SPECULAR)] _SpecularIntensity ("Specular Intensity", Float) = 1
        [Space(12)] [Toggle(LIGHTMAP)] _EnableLightmap ("Lightmap", Float) = 0
        [Space(12)] [Toggle(NORMAL_MAP)] _EnableNormalMap ("Normal Map", Float) = 0
        [ShowIfAny(NORMAL_MAP)] _NormalTex ("Normal Texture", 2D) = "bump" {}
        [ToggleShowIfAny(SECONDARY_UVS_NORMAL, 2, 0_SECONDARY_UVS_NONE, NORMAL_MAP)] _SecondaryUVsNormal ("Normal Map Secondary UVs", Float) = 0
        [ShowIfAny(NORMAL_MAP)] _NormalScale ("Normal Scale", Float) = 1
        [Space(12)] [Toggle(USE_SPHERICAL_NORMAL_OFFSET)] _UseSphericalNormalOffset ("Spherical Normal Offset", Float) = 0
        [ShowIfAny(USE_SPHERICAL_NORMAL_OFFSET)] _SphericalNormalOffsetIntensity ("Spherical Normal Offset Intensity", Float) = 0.5
        [ShowIfAny(USE_SPHERICAL_NORMAL_OFFSET)] _SphericalNormalOffsetCenter ("Spherical Normal Offset Center", Vector) = (0,0,0,1)
        [Header(REFLECTIONS)] [Space(12)] [Toggle(REFLECTION_TEXTURE)] _EnableReflectionTexture ("Reflection Texture", Float) = 0
        [ShowIfAny(REFLECTION_TEXTURE)] _ReflectionTexIntensity ("Texture Intensity", Float) = 1
        [ShowIfAny(REFLECTION_TEXTURE)] _EnvironmentReflectionCube ("Environment Reflection", Cube) = "" {}
        [Space(12)] [Toggle(REFLECTION_PROBE)] _EnableReflectionProbe ("Reflection Probe", Float) = 0
        [EnumShowIfAny(2, Fast, Precise, REFLECTION_PROBE)] _Probe_Calculation ("Probe Calculations", Float) = 0
        [ToggleShowIfAny(REFLECTION_PROBE_DISABLED_WHITEBOOST, REFLECTION_PROBE)] _ReflectionProbeDisabledWhiteboost ("Disable Probe Whiteboost", Float) = 0
        [ShowIfAny(2, REFLECTION_PROBE, _PROBE_CALCULATION_PRECISE)] _ReflectionProbeGrayscale ("Probe Grayscale Factor", Range(0, 1)) = 0.2
        [ShowIfAny(2, REFLECTION_PROBE, _PROBE_CALCULATION_PRECISE)] _ColoredMetalMultiplier ("Colored Metal Multiplier", Range(0, 15)) = 3.5
        [ShowIfAny(2, REFLECTION_PROBE, _PROBE_CALCULATION_PRECISE)] _WhiteOffset ("White Offset", Float) = 2
        [ShowIfAny(REFLECTION_PROBE)] _ReflectionProbeIntensity ("Probe Intensity", Float) = 1
        [ToggleShowIfAny(REFLECTION_PROBE_BOX_PROJECTION, REFLECTION_PROBE)] _ReflectionProbeBoxProjection ("Box Projection", Float) = 1
        [ToggleShowIfAny(REFLECTION_PROBE_BOX_PROJECTION_OFFSET, 2, REFLECTION_PROBE, REFLECTION_PROBE_BOX_PROJECTION)] _EnableBoxProjectionOffset ("Box Projection Offset", Float) = 0
        [ShowIfAny(3, REFLECTION_PROBE, REFLECTION_PROBE_BOX_PROJECTION, REFLECTION_PROBE_BOX_PROJECTION_OFFSET)] _ReflectionProbeBoxProjectionSizeOffset ("Box Projection Size Offset", Vector) = (0,0,0,0)
        [ShowIfAny(3, REFLECTION_PROBE, REFLECTION_PROBE_BOX_PROJECTION, REFLECTION_PROBE_BOX_PROJECTION_OFFSET)] _ReflectionProbeBoxProjectionPositionOffset ("Box Projection Position Offset", Vector) = (0,0,0,0)
        [ToggleShowIfAny(REFLECTION_STATIC, REFLECTION_PROBE)] _ReflectionStatic ("Static Reflection", Float) = 0
        [ToggleShowIfAny(REFLECTION_PROBE_SINGLE_CUBEMAP, REFLECTION_PROBE)] _ReflectionSingleCubemap ("Single Cubemap Reflections", Float) = 0
        [ToggleShowIfAny(MULTIPLY_REFLECTIONS, 2, REFLECTION_PROBE, REFLECTION_TEXTURE)] _MultiplyReflections ("Multiply Reflection Texture", Float) = 1
        [Toggle(RIM_DIM)] _EnableRimDim ("Reflection Rim Dim", Float) = 0
        [ShowIfAny(RIM_DIM)] _RimScale ("Rim Scale", Float) = 1
        [ShowIfAny(RIM_DIM)] _RimOffset ("Rim Offset", Float) = 1
        [ShowIfAny(RIM_DIM)] _RimDistanceOffset ("Rim Camera Distance Offset", Float) = 2
        [ShowIfAny(RIM_DIM)] _RimDistanceScale ("Rim Camera Distance Scale", Float) = 0.3
        [ShowIfAny(RIM_DIM)] _RimSmoothness ("Rim Smoothness", Float) = 1
        [ShowIfAny(RIM_DIM)] _RimDarkening ("Rim Darkening", Float) = 0
        [ToggleShowIfAny(INVERT_RIM_DIM, RIM_DIM)] _InvertRimDim ("Invert Rim Dim", Float) = 0
        [Header(OCCLUSION AND GROUND FADE)] [Space(12)] [Toggle(GROUND_FADE)] _EnableGroundFade ("Height Occlusion", Float) = 0
        [ShowIfAny(GROUND_FADE)] _GroundFadeScale ("Height Occlusion Scale", Float) = 0.5
        [ShowIfAny(GROUND_FADE)] _GroundFadeOffset ("Height Occlusion Offset", Float) = 1
        [Space(12)] [Toggle(OCCLUSION)] _EnableOcclusion ("Texture Occlusion", Float) = 0
        [EnumShowIfAny(3, Texture, MPM B, Avatar MPM R, OCCLUSION)] _Occlusion_Source ("Occlusion Source", Float) = 0
        [ShowIfAny(2, OCCLUSION, _OCCLUSION_SOURCE_TEXTURE)] _DirtTex ("Occlusion Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_OCCLUSION, 2, 0_SECONDARY_UVS_NONE, _OCCLUSION_SOURCE_TEXTURE, OCCLUSION)] _SecondaryUVsOcclusion ("Occlusion Map Secondary UVs", Float) = 0
        [ShowIfAny(OCCLUSION)] _OcclusionIntensity ("Occlusion Intensity", Range(0, 1)) = 1
        [Space(12)] [Toggle(OCCLUSION_DETAIL)] _EnableOcclusionDetail ("Texture Occlusion Detail", Float) = 0
        [ShowIfAny(OCCLUSION_DETAIL)] _DirtDetailTex ("Occlusion Detail Texture", 2D) = "white" {}
        [ToggleShowIfAny(SECONDARY_UVS_OCCLUSION_DETAIL, 2, 0_SECONDARY_UVS_NONE, OCCLUSION_DETAIL)] _SecondaryUVsOcclusionDetail ("Occlusion Detail Secondary UVs", Float) = 0
        [ShowIfAny(OCCLUSION_DETAIL)] _OcclusionDetailIntensity ("Occlusion Detail Intensity", Range(0, 1)) = 1
        [ToggleShowIfAny(OCCLUSION_BEFORE_EMISSION, OCCLUSION, OCCLUSION_DETAIL)] _OcclusionBeforeEmission ("Texture Occlusion Before Emission", Float) = 0
        [Header(OTHER)] [Space(16)] _EnableRotateUV ("Rotate UVs 90", Float) = 0
        _RotateUV ("Rotation Angle", Float) = 0
        [Space(12)] [Toggle(UV_COLOR_SEGMENTS)] _UVColorSegments ("UV Color Segments", Float) = 0
        [ToggleShowIfAny(UV_SEGMENTS_IGNORE_RIM, UV_COLOR_SEGMENTS)] _UvSegmentsIgnoreRim ("Don't override Rim color", Float) = 0
        [Space(12)] [Toggle(HIGHLIGHT_SELECTION)] _HighlightSelection ("Highlight Selection", Float) = 0
        [ShowIfAny(HIGHLIGHT_SELECTION)] _SegmentToHighlight ("Segment To Highlight", Float) = -1
        [Space(12)] [Toggle(FOG)] _EnableFog ("Fog", Float) = 1
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", Float) = 0
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", Float) = 1
        [ToggleShowIfAny(HEIGHT_FOG, FOG)] _EnableHeightFog ("Height Fog", Float) = 0
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", Float) = 1
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", Float) = 0
        [ToggleShowIfAny(HEIGHT_FOG_DEPTH_SOFTEN, 2, FOG, HEIGHT_FOG)] _EnableHeightFogSoften ("Soften with Distance", Float) = 0
        [ShowIfAny(3, FOG, HEIGHT_FOG, HEIGHT_FOG_DEPTH_SOFTEN)] _FogSoften ("Soften Scale", Float) = 1
        [ShowIfAny(3, FOG, HEIGHT_FOG, HEIGHT_FOG_DEPTH_SOFTEN)] _FogSoftenOffset ("Soften Offset", Float) = 1
        [ShowIfAny(1, FOG, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK, _VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL)] _EmissionFogSuppression ("Quest Fog Supression", Range(0, 1)) = 0
        [ShowIfAny(1, FOG, _EMISSIONTEXTURE_SIMPLE, _EMISSIONTEXTURE_PULSE, _EMISSIONTEXTURE_FLIPBOOK, _VERTEXMODE_EMISSION, _VERTEXMODE_SPECIAL)] _MainEffectFogSuppression ("MainEffect Fog Supression", Range(0, 1)) = 0
        [Space(18)] [Toggle(COLOR_BY_FOG)] _ColorFog ("Color by Fog", Float) = 0
        [ShowIfAny(COLOR_BY_FOG)] _ColorFogMultiplier ("Fog Multiplier", Float) = 1
        [ShowIfAny(COLOR_BY_FOG)] _ColorFogMax ("Fog Max Brightness", Float) = 1
        [ShowIfAny(COLOR_BY_FOG)] _ColorFogInfluence ("Color Influence", Range(0, 1)) = 0.5
        [ToggleShowIfAny(FOG_COLOR_HIGHLIGHT, COLOR_BY_FOG)] _FogColorHighlight ("Fog Highlight", Float) = 0
        [ShowIfAny(2, COLOR_BY_FOG, FOG_COLOR_HIGHLIGHT)] _ColorFogHighlightMultiplier ("Fog Highlight Multiplier", Float) = 30000
        [Space(12)] [Toggle(DISTANCE_DARKENING)] _EnableDistanceDarkening ("Worldspace Occlusion", Float) = 0
        [ShowIfAny(DISTANCE_DARKENING)] _DarkeningScale ("Scale", Float) = 0.35
        [ShowIfAny(DISTANCE_DARKENING)] _DarkeningIntensity ("Intensity", Float) = 1
        [VectorShowIfAny(3, DISTANCE_DARKENING)] _DarkeningCenter ("Center", Vector) = (0,0,0,0)
        [VectorShowIfAny(3, DISTANCE_DARKENING)] _DarkeningDirection ("Axes", Vector) = (1,1,1,1)
        [Space(12)] [KeywordEnum(None, Grid, Scanline, Legacy)] _Hologram ("Hologram Effect", Float) = 0
        [ToggleShowIfAny(HOLOGRAM_MATERIALIZATION, _HOLOGRAM_GRID)] _UseHologramMaterialization ("Materialization", Float) = 0
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE, _HOLOGRAM_LEGACY)] _HologramColor ("Hologram Color", Color) = (1,1,1,1)
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_LEGACY)] _HologramGridSize ("Hologram Grid Size", Float) = 3
        [ShowIfAny(_HOLOGRAM_GRID)] _HologramFill ("Hologram Fill", Float) = -0.6
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HologramStripeSpeed ("Hologram Stripe Speed", Float) = 1.43
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HologramScanDistance ("Hologram Scan Distance", Float) = 2
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HologramPhaseOffset ("Hologram Phase Offset", Range(-1, 1)) = 0
        [ShowIfAny(2, _HOLOGRAM_GRID, HOLOGRAM_MATERIALIZATION)] _HoloMaterialize ("Hologram Materialize", Range(0, 1)) = 1
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HoloIntensity ("Hologram Intensity", Float) = 1
        [ShowIfAny(_HOLOGRAM_GRID, _HOLOGRAM_SCANLINE)] _HaltScan ("Halt scanning", Float) = 0
        [Space(12)] [Toggle(FAKE_MIRROR_TRANSPARENCY)] _EnableFakeMirrorTransparency ("Fake Mirror Transparency", Float) = 0
        [ShowIfAny(FAKE_MIRROR_TRANSPARENCY)] _FakeMirrorTransparency ("Mirror Transparency Multiplier", Float) = 1
        [ToggleShowIfAny(MIRROR_VERTEX_DISTORTION, FAKE_MIRROR_TRANSPARENCY)] _NoteVertexDistortion ("Mirror Vertex Distortion", Float) = 0
        [HideInInspector] Note_Plane_Cut ("Note Plane Cut", Float) = 0
        [ShowIfAny(NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _CutPlaneEdgeGlowWidth ("Plane Edge Glow Width", Float) = 0.01
        [ShowIfAny(NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _NoteSize ("Note Size", Float) = 0.25
        [ShowIfAny(NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _CutPlane ("Cut Plane", Vector) = (1,0,0,0)
        [HideInInspector] Cutout_Type ("Cutout", Float) = 0
        [ShowIfAny(CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _Cutout ("Cutout Threshold", Range(0, 1)) = 0
        [ShowIfAny(CUTOUT_TYPE_HD_DISSOLVE)] _CutoutTexScale ("Cutout Texture Scale", Float) = 1
        [ToggleShowIfAny(CLOSE_TO_CAMERA_CUTOUT, CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _EnableCloseToCameraCutout ("Close to Camera Cutout", Float) = 0
        [ShowIfAny(1, CLOSE_TO_CAMERA_CUTOUT, CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _CloseToCameraCutoutOffset ("Close to Camera Cutout Offset", Float) = 0.5
        [ShowIfAny(1, CLOSE_TO_CAMERA_CUTOUT, CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE)] _CloseToCameraCutoutScale ("Close to Camera Cutout Scale", Float) = 0.5
        [ShowIfAny(CUTOUT_TYPE_HD_DISSOLVE, CUTOUT_TYPE_LW_SCALE, NOTE_PLANE_CUT_HD_DISSOLVE NOTE_PLANE_CUT_LW_SNAP)] _GlowCutoutColor ("Cut/Cutout Glow Color", Color) = (1,1,1,1)
        [Space(12)] [Toggle(DISSOLVE)] _EnableDissolve ("Dissolve", Float) = 0
        [EnumShowIfAny(3, Clip, Fade, Both, DISSOLVE)] _DissolveAlpha ("Alpha Aproach", Float) = 0
        [ShowIfAny(1, DISSOLVE, _DISSOLVEALPHA_FADE, _DISSOLVEALPHA_BOTH)] _AlphaMultiplier ("Alpha Multiplier", Float) = 1
        [ShowIfAny(1, DISSOLVE, _DISSOLVEALPHA_FADE, _DISSOLVEALPHA_BOTH)] _DissolveScale ("Fade Falloff Scale", Float) = 5
        [Space(16)] [FloatToggleShowIfAny(DISSOLVE)] _DissolveReverse ("Invert Dissolve", Float) = 0
        [EnumShowIfAny(5, Local, World, World Centered, Uv, Avatar, DISSOLVE)] _Dissolve_Space ("Dissolve Space", Float) = 0
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeStartY ("Body Fade Fully Opaque Y", Float) = 1.25
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeEndY ("Body Fade Fully Transparent Y", Float) = 1
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeZoneInterceptX ("Fade Zone Intercept X", Float) = 0.2
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _FadeZoneSlope ("Fade Zone Slope", Float) = 0.76
        [ShowIfAny(2, DISSOLVE, _DISSOLVEAXIS_AVATAR)] _BodyFadeGamma ("Fadeout Factor", Float) = 2
        [ShowIfAny(2, DISSOLVE, 0_DISSOLVEAXIS_AVATAR)] _DissolveAxisVector ("Dissolve Axis", Vector) = (0,1,0,0)
        [ToggleShowIfAny(DISSOLVE_PROGRESS, DISSOLVE)] _UseDissolveProgress ("Dissolve Progress", Float) = 0
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVEAXIS_AVATAR, 0DISSOLVE_PROGRESS)] _DissolveOffset ("Dissolve Offset", Float) = 0
        [ShowIfAny(3, DISSOLVE, DISSOLVE_PROGRESS, 0_DISSOLVEAXIS_AVATAR)] _DissolveStartValue ("Dissolve Start Value", Float) = 0
        [ShowIfAny(3, DISSOLVE, DISSOLVE_PROGRESS, 0_DISSOLVEAXIS_AVATAR)] _DissolveEndValue ("Dissolve End Value", Float) = 10
        [ShowIfAny(3, DISSOLVE, DISSOLVE_PROGRESS, 0_DISSOLVEAXIS_AVATAR)] _DissolveProgress ("Dissolve Progress", Range(-1, 1)) = 0
        [ToggleShowIfAny(DISSOLVE_COLOR, 1, DISSOLVE)] _UseDissolveColor ("Dissolve Color", Float) = 0
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _DissolveColor ("Dissolve Color", Color) = (0,1,1,0)
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _DissolveColorIntensity ("Color Intensity", Float) = 1
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _CutColorFalloff ("Cut Falloff Scale", Float) = 4
        [ShowIfAny(2, DISSOLVE, DISSOLVE_COLOR)] _CutColorBacksideFalloff ("Backface Falloff Multiplier", Float) = 0.07
        [EnumShowIfAny(4, None, Local, World, Uv, DISSOLVE, DISSOLVE_COLOR)] _Dissolve_Grid ("Dissolve Grid", Float) = 0
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridThickness ("Grid Thickness", Float) = 1.5
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridSize ("Grid Size", Float) = 10
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridFalloff ("Grid Falloff Scale", Float) = 4
        [ShowIfAny(3, DISSOLVE, 0_DISSOLVE_GRID_NONE, DISSOLVE_COLOR)] _GridSpeed ("Grid Speed", Float) = 0.1
        [ToggleShowIfAny(DISSOLVE_TEXTURE, 1, DISSOLVE)] _UseDissolveTexture ("Dissolve Texture", Float) = 0
        [ShowIfAny(2, DISSOLVE, DISSOLVE_TEXTURE)] _DissolveTexture ("Dissolve Texture", 2D) = "black" {}
        [VectorShowIfAny(2, 2, DISSOLVE, DISSOLVE_TEXTURE)] _DissolveTextureSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [ShowIfAny(2, DISSOLVE, DISSOLVE_TEXTURE)] _DissolveTextureInfluence ("Texture Influence", Float) = 0.2
        [KeywordEnum(None, Simple)] Distortion ("Distortion", Float) = 0
        [EnumShowIfAny(10, MPM, EmissionTex, Emission Mask, Secondary Emission Mask, Pulse, Parralax, Diffuse, Normal, Occlusion, Occlusion Detail, DISTORTION_SIMPLE)] _Distortion_Target ("Distortion Target", Float) = 0
        [Space(12)] [ShowIfAny(DISTORTION_SIMPLE)] _DistortionTex ("Distortion Texture", 2D) = "black" {}
        [ToggleShowIfAny(SECONDARY_UVS_DISTORTION, 2, 0_SECONDARY_UVS_NONE, DISTORTION_SIMPLE)] _SecondaryUVsDistortion ("Distortion Secondary UVs", Float) = 0
        [ShowIfAny(DISTORTION_SIMPLE)] _DistortionStrength ("Distortion Strength", Float) = 0.2
        [ShowIfAny(DISTORTION_SIMPLE)] _DistortionAxes ("Distortion Axes", Vector) = (1,1,0,0)
        [ShowIfAny(DISTORTION_SIMPLE)] _DistortionPanning ("Distortion Panning", Vector) = (0,0,0,0)
        [Space(12)] [Header(Other)] [Space] [Toggle(NOISE_DITHERING)] _EnableNoiseDithering ("Noise Dithering", Float) = 1
        [Toggle(LINEAR_TO_GAMMA)] _LinearToGamma ("LinearToGamma", Float) = 0
        [KeywordEnum(Standard, Song Time, Freeze)] _Custom_Time ("Time Behavior", Float) = 0
        [KeywordEnum(None, Around_X, Around_Y, Around_Z)] _Curve_Vertices ("Curve Vertices", Float) = 0
        [KeywordEnum(After Emissive, Before Emissive)] _Aces_Approach ("ACES approach", Float) = 0
        [Space(12)] [Toggle(TEXTURE3D_LOOKUP)] _Texture3D_Lookup ("Texture 3D Grid Lookup", Float) = 0
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupTex ("Lookup Texture", 3D) = "gray" {}
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupGridSize ("Lookup Grid Size", Vector) = (1,1,1,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupXYZDisplacementScale ("Lookup XYZ Displacement Scale", Float) = 10
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupXDisplacementMapping ("Lookup X Displacement Mapping", Vector) = (0,0,0,0)
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupYDisplacementMapping ("Lookup Y Displacement Mapping", Vector) = (0,0,0,0)
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupZDisplacementMapping ("Lookup Z Displacement Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupRadialDisplacementScale ("Lookup Radial Displacement Scale", Float) = 10
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupRadialDisplacementMapping ("Lookup Radial Displacement Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupMaxScale ("Lookup Max Scale", Float) = 2
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupScaleMapping ("Lookup Scale Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupRotationMultiplier ("Lookup Rotation Multiplier", Float) = 1
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupRotationMapping ("Lookup Rotation Mapping", Vector) = (0,0,0,0)
        [VectorShowIfAny(4, TEXTURE3D_LOOKUP)] _LookupEmissiveMapping ("Lookup Emissive Mapping", Vector) = (0,0,0,0)
        [ShowIfAny(TEXTURE3D_LOOKUP)] _LookupEmissiveModulationStrength ("Emissive Modulation Strength", Float) = 1
        [Header(SETTINGS)] [Space(12)] [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", Float) = 2
        [Toggle] _ZWrite ("Z Write", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", Float) = 0
        [Space(12)] [Header(Color Blending)] [Space] [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Foreground Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Background Factor", Float) = 0
        [Header(Bloom Blending)] [Space] [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Foreground Factor", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Background Factor", Float) = 0
        [Space(12)] [Toggle(MESH_PACKING)] _MeshPacking ("Mesh Packed Instancing", Float) = 0
        [ShowIfAny(MESH_PACKING)] _MeshPackingId ("Mesh Packing Id", Float) = 1
        [Toggle(COLOR_ARRAY)] _UseColorArray ("Color Array", Float) = 0
        _SDFNoiseOffset ("Noise offset", Vector) = (0,0,0,0)
        _SDFNoisePanning ("Noise panning", Vector) = (0,0,0,0)
        _SDFNoiseIntensity ("Noise Intensity", Float) = 1
        _SDFNoiseScale ("Noise Scale", Float) = 5
        _SDFPointIntensity ("Color Intensity", Float) = 1
        _SDFNegativeIntensity ("Negative Intensity", Float) = 0.5
        _SDFNoiseTex ("Noise Tex", 3D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
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
            #include_with_pragmas "LitPass.hlsl"
            ENDHLSL
        }

        // The camera depth texture uses ShadowCaster passes. Share vertex displacement and
        // dissolve with the visible pass so smoke fades against the rendered surface.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Blend Off
            ColorMask 0

            HLSLPROGRAM
            #define CM_DEPTH_ONLY
            #include_with_pragmas "LitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "META"
            Tags { "LightMode"="META" }
            Cull Off
            ZWrite On
            ZTest LEqual
            Blend One Zero, One Zero
            BlendOp Add, Add
            ColorMask RGBA

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitMetaVertex
            #pragma fragment LitMetaFragment
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"
            #include "UnityMetaPass.cginc"

            float4 _Color;
            float4 _MainTex_ST;
            float4 _DetailAlbedoMap_ST;
            float _UVSec;

            struct LitMetaAttributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct LitMetaVaryings
            {
                float4 position : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            LitMetaVaryings LitMetaVertex(LitMetaAttributes v)
            {
                LitMetaVaryings o;
                // META uses eye zero so baking is independent of the instance eye.
                #if defined(UNITY_STEREO_INSTANCING_ENABLED)
                unity_StereoEyeIndex = 0;
                #endif
                o.position = UnityMetaVertexPosition(v.vertex, v.uv1, v.uv2,
                    unity_LightmapST, unity_DynamicLightmapST);
                o.uv.xy = v.uv0 * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 detailUV = _UVSec == 0.0 ? v.uv0 : v.uv1;
                o.uv.zw = detailUV * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
                return o;
            }

            float4 LitMetaFragment() : SV_Target
            {
                float outputBoost = saturate(unity_OneOverOutputBoost);
                float3 albedo = exp2(log2(_Color.rgb) * outputBoost);
                albedo = min(albedo, unity_MaxOutputValue);
                float4 result = unity_MetaFragmentControl.x
                    ? float4(albedo, 1.0) : float4(0.0, 0.0, 0.0, 0.0);
                return unity_MetaFragmentControl.y
                    ? float4(0.0, 0.0, 0.0, 1.0) : result;
            }
            ENDHLSL
        }
    }
}
