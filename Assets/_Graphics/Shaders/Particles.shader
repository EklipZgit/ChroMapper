Shader "ChroMapper/Particles"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        [Space]
        [Toggle(SECONDARY_COLOR)] _EnableSecondaryColor ("Use Secondary Color", float) = 0
        [ShowIfAny(SECONDARY_COLOR)] _SecondaryColor ("Secondary Color", Color) = (1,1,1,1)
        [ShowIfAny(SECONDARY_COLOR)] _SecondaryColorTex ("Secondary Color Texture", 2D) = "white" {}
        [ShowIfAny(SECONDARY_COLOR)] _SecondaryColorPanning ("Secondary Color Panning", Vector) = (0,0,0,0)

        [Space]
        [Toggle(COLOR_GRADIENT)] _UseColorGradient ("Use Color Gradient", float) = 0
        [ShowIfAny(COLOR_GRADIENT)] _ColorGradient ("Gradient LUT", 2D) = "white" {}
        [ShowIfAny(COLOR_GRADIENT)] _GradientPosition ("Gradient Position", Range(0, 1)) = 0.5
        [ShowIfAny(COLOR_GRADIENT)] _GradientPanningSpeed ("Gradient Panning Speed", float) = 0

        [Space]
        [Toggle(SPECTROGRAM_COLOR)] _UseSpectrogram ("Color by Spectrogram", float) = 0
        [ShowIfAny(SPECTROGRAM_COLOR)] _SpectrogramBaseValue ("Spectrogram Base Value", Range(0, 1)) = 0.2
        [ShowIfAny(SPECTROGRAM_COLOR)] _SpectrogramRange ("Spectrogram Range", Range(0, 1)) = 0.2

        [Space]
        [Toggle(COLOR_ARRAY)] _UseColorArray ("Use Color Array", float) = 0

        [Space]
        [KeywordEnum(None, Import)] _Secondary_UVs ("Secondary UVs", float) = 0
        [Toggle] _EnableRotateUV ("Rotate UVs 90", float) = 0
        _RotateUV ("Rotation Angle", float) = 0
        [Toggle] _RotateMainUVOnly ("Rotate Main UV Only", float) = 0



        [Header(Vertex)] [Space]
        [Toggle(VERTEX_COLOR)] _EnableVertexColor ("Vertex Color", float) = 0
        [ToggleShowIfAny(VERTEX_SQUARE_ALPHA, VERTEX_COLOR)] _SquareVertexAlpha ("Square Vertex Alpha", float) = 0
        [ToggleShowIfAny(VERTEX_RED_IS_ALPHA, VERTEX_COLOR)] _RedIsVertexAlpha ("Red is Vertex Alpha", float) = 0
        [EnumShowIfAny(3, RGBA, A, RGB, VERTEX_COLOR)] _VertexChannels ("Vertex Channels", float) = 0

        [Space]
        [Toggle(VERTEX_DISPLACEMENT)] _VertexDisplacement ("Use Vertex Displacement", float) = 0
        [ShowIfAny(VERTEX_DISPLACEMENT)] _DisplacementTex ("Displacement Texture", 2D) = "white" {}
        [ToggleShowIfAny(SPATIAL_DISPLACEMENT, VERTEX_DISPLACEMENT)] _3DDisplacement ("3D Displacement", float) = 0
        [ShowIfAny(VERTEX_DISPLACEMENT)] _DisplacementStrength ("Strength", float) = 0.1
        [ShowIfAny(2, VERTEX_DISPLACEMENT, SPATIAL_DISPLACEMENT)] _DisplacementAxes ("Per Axis Strength", Vector) = (1,1,1,0)
        [ShowIfAny(VERTEX_DISPLACEMENT)] _DisplacementPanningSpeed ("Panning Speed", float) = 1
        [ShowIfAny(VERTEX_DISPLACEMENT)] _DisplacementPanning ("Panning", Vector) = (0,0,0,0)
        [EnumShowIfAny(1, None, Flat, Full, SPATIAL_DISPLACEMENT)] _Spectrogram ("Spectrogram Influence", float) = 0
        [ShowIfAny(_SPECTROGRAM_FULL)] _UV3Offset ("UV3 Offset", float) = 0
        [ShowIfAny(_SPECTROGRAM_FULL)] _UV3Scale ("UV3 Scale", float) = 1

        [Space]
        [KeywordEnum(None, Around_X, Around_Y, Around_Z)] _Curve_Vertices ("Curve Vertices (Object Space)", float) = 0



        [Header(Texture)] [Space]
        [Toggle(MAIN_TEXTURE)] _UseMainTex ("Base Texture", float) = 1
        _BaseLayer ("Base Color", float) = 1
        _MainTex ("Texture", 2D) = "white" {}

        [Space]
        [Toggle(PIXELATE)] _Pixelate ("Pixelate", float) = 0
        [VectorShowIfAny(2, PIXELATE)] _PixelateResolution ("Pixelate Resolution", Vector) = (64,64,0,0)

        [Space]
        [Toggle(TEXTURE_COLOR)] _EnableTextureColor ("Use Texture Color", float) = 0
        [EnumShowIfAny(2, Alpha, Red, 0TEXTURE_COLOR)] _AlphaChannel ("Alpha Channel", float) = 0

        [Space]
        _Intensity("Color Intensity", float) = 1
        _UvPanning ("UV Panning", Vector) = (0,0,0,0)

        [Space]
        [Toggle(CUSTOM_WRAPPING)] _EnableCustomPadding ("Custom Repeat Wrapping", float) = 0
        [VectorShowIfAny(2, CUSTOM_WRAPPING)] _CustomPadding ("Custom Padding", Vector) = (0,0,0,0)

        [Space]
        [Toggle(TEXTURE_FLIPBOOK)] _UseTextureFlipbook ("Use Texture Flipbook", float) = 0
        [ShowIfAny(TEXTURE_FLIPBOOK)] _FlipbookColumns ("Flipbook Columns", float) = 8
        [ShowIfAny(TEXTURE_FLIPBOOK)] _FlipbookRows ("Flipbook Rows", float) = 8
        [ShowIfAny(TEXTURE_FLIPBOOK)] _FlipbookNonloopableFrames ("Full Non-loopable frames", float) = 0
        [ShowIfAny(TEXTURE_FLIPBOOK)] _FlipbookSpeed ("Flipbook Speed", float) = 1
        [ToggleShowIfAny(FLIPBOOK_BLENDING_OFF, TEXTURE_FLIPBOOK)] _FlipbookBlendingOff ("No Frame Blending", float) = 0

        [Space]
        [Toggle(MASK)] _EnableMask ("Mask", float) = 0
        [ToggleShowIfAny(SECONDARY_UVS_MASK, MASK)] _MaskSecondaryUVs ("Use Secondary UVs", float) = 0
        [ToggleShowIfAny(MASK_RED_IS_ALPHA, MASK)] _MaskRedIsAlpha ("Red is Alpha", float) = 0
        [EnumShowIfAny(3, Multiply, Add, Masked Add, MASK)] _MaskBlend ("Mask Blend", float) = 0
        [ShowIfAny(MASK)] _MaskTex ("Mask Texture", 2D) = "white" {}
        [ShowIfAny(MASK)] _MaskStrength ("Mask Strength", float) = 1
        [ShowIfAny(MASK)] _MaskPanning ("Mask Panning", Vector) = (0,0,0,0)

        [Space]
        [Toggle(MASK2)] _EnableMask2 ("Secondary Mask", float) = 0
        [ToggleShowIfAny(SECONDARY_UVS_MASK2, MASK2)] _Mask2SecondaryUVs ("Use Secondary UVs", float) = 0
        [ToggleShowIfAny(MASK2_RED_IS_ALPHA, MASK2)] _Mask2RedIsAlpha ("Red is Alpha", float) = 0
        [EnumShowIfAny(3, Multiply, Add, Masked Add, MASK2)] _Mask2Blend ("Secondary Mask Blend", float) = 0
        [ShowIfAny(MASK2)] _Mask2Tex ("Secondary Mask Texture", 2D) = "white" {}
        [ShowIfAny(MASK2)] _Mask2Strength ("Secondary Mask Strength", float) = 1
        [ShowIfAny(MASK2)] _Mask2Panning ("Secondary Mask Panning", Vector) = (0,0,0,0)

        [Space]
        [KeywordEnum(None, Simple)] _Distortion ("Distortion", float) = 0
        [ShowIfAny(_DISTORTION_SIMPLE)] _DistortionTex ("Distortion Texture", 2D) = "white" {}
        [ShowIfAny(_DISTORTION_SIMPLE)] _DistortionStrength ("Distortion Strength", float) = 0.2
        [ShowIfAny(_DISTORTION_SIMPLE)] _DistortionAxes ("Distortion Axes", Vector) = (1, 1, 0, 0)
        [ShowIfAny(_DISTORTION_SIMPLE)] _DistortionPanning ("Distortion Panning", Vector) = (0, 0, 0, 0)

        [Header(Dissolve)] [Space]
        [KeywordEnum(None, Alpha Clip)] _CutoutType ("Cutout", float) = 0
        [ShowIfAny(_CUTOUTTYPE_WORLDSPACE_NOISE, _CUTOUTTYPE_ALPHA_CLIP, _CUTOUTTYPE_SCALE)] _Cutout ("Threshold", Range(0, 1)) = 0.5



        [Header(Alpha Handling)] [Space]
        _AlphaMultiplier ("Alpha Multiplier", float) = 1
        [Toggle(SQUARE_ALPHA)] _SquareAlpha("Square Alpha", float) = 1

        [Space]
        [Toggle(VIEW_ALIGN_DISAPPEAR)] _EnableViewAlignDisappear ("View Align Disappear", float) = 0
        [ToggleShowIfAny(VIEW_ALIGN_DISAPPEAR_SQUARE_ANGLE, VIEW_ALIGN_DISAPPEAR)] _SquareAngleForViewAlignDisappear ("Square Angle", float) = 0
        [ShowIfAny(VIEW_ALIGN_DISAPPEAR)] _ViewAlignFactor ("View Align Factor", float) = 1.5
        [ShowIfAny(VIEW_ALIGN_DISAPPEAR)] _ViewAlignOffset ("View Align Offset", float) = 0



        [Header(Others)] [Space]
        [KeywordEnum(None, PP, Frag)] _BloomType ("Bloom Type", float) = 0
        [ShowIfAny(_BLOOMTYPE_PP, _BLOOMTYPE_FRAG)] _QuestWhiteboostMultiplier ("White Multiplier", float) = 1
        [ShowIfAny(_BLOOMTYPE_PP, _BLOOMTYPE_FRAG)] _BloomMultiplier ("Bloom Multiplier", float) = 1
        [Toggle(REMAP_WHITEBOOST_START)] _EnableRemapWhiteBoostStart ("Remap White Boost Start", float) = 0
        [ShowIfAny(_WHITEBOOSTTYPE_MAINEFFECT, _WHITEBOOSTTYPE_ALWAYS)] _WhiteBoostRemapStart ("Alpha for no White Boost", Range(0, 1)) = 0

        [Space]
        [KeywordEnum(None, Full, Y Axis, Camera Facing)] _Billboard ("Billboard", float) = 0
        [ShowIfAny(_BILLBOARD_FULL, _BILLBOARD_CAMERA_FACING)] _BillboardScale ("Billboard Scale", float) = 1

        [Space]
        [KeywordEnum(Standard, Song Time, Freeze)] _Custom_Time ("Time Behavior", float) = 0
        [Toggle(MESH_PACKING)] _MeshPacking ("Use Mesh Packed Instancing", Float) = 0
        [ShowIfAny(MESH_PACKING)] _MeshPackingId ("Mesh Packing ID", float) = 0



        [Header(Fog Settings)] [Space]
        [KeywordEnum(None, Lerp, Color, Alpha)] _FogType ("Fog Type", float) = 0
        [ShowIfAny(0_FOGTYPE_NONE)] _FogStartOffset ("Fog Start Offset", float) = 1
        [ShowIfAny(0_FOGTYPE_NONE)] _FogScale ("Fog Scale", float) = 1
        [Space]
        [ToggleShowIfAny(HEIGHT_FOG, 0_FOGTYPE_NONE)] _EnableHeightFog ("Enable Height Fog", float) = 0
        [ShowIfAny(2, 0_FOGTYPE_NONE, HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", float) = 0
        [ShowIfAny(2, 0_FOGTYPE_NONE, HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", float) = 1

        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src A", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst A", float) = 1
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", float) = 0
        [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 0
        _OffsetFactor ("Offset Factor", float) = 0
        _OffsetUnits ("Offset Units", float) = 0
    }
    SubShader
    {
        Tags
        {
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Lighting Off
        Offset [_OffsetFactor], [_OffsetUnits]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local SECONDARY_COLOR

            #pragma shader_feature_local COLOR_GRADIENT

            #pragma shader_feature_local_vertex SPECTROGRAM_COLOR
            #pragma shader_feature_local_fragment SPECTROGRAM_COLOR

            #pragma shader_feature_local COLOR_ARRAY

            #pragma shader_feature_local _ _SECONDARY_UVS_IMPORT

            #pragma shader_feature_local_vertex VERTEX_COLOR
            #pragma shader_feature_local_vertex VERTEX_SQUARE_ALPHA
            #pragma shader_feature_local_vertex VERTEX_RED_IS_ALPHA
            #pragma shader_feature_local_vertex _ _VERTEXCHANNELS_A _VERTEXCHANNELS_RGB

            #pragma shader_feature_local_vertex VERTEX_DISPLACEMENT
            #pragma shader_feature_local_vertex SPATIAL_DISPLACEMENT
            #pragma shader_feature_local_vertex _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL

            #pragma shader_feature_local_vertex _ _CURVE_VERTICES_AROUND_X _CURVE_VERTICES_AROUND_Y _CURVE_VERTICES_AROUND_Z
            #pragma shader_feature_local_vertex MESH_PACKING

            #pragma shader_feature_local MAIN_TEXTURE

            #pragma shader_feature_local_fragment PIXELATE

            #pragma shader_feature_local_fragment TEXTURE_COLOR
            #pragma shader_feature_local_fragment _ _ALPHACHANNEL_RED

            #pragma shader_feature_local_fragment CUSTOM_WRAPPING

            #pragma shader_feature_local_fragment TEXTURE_FLIPBOOK
            #pragma shader_feature_local_fragment FLIPBOOK_BLENDING_OFF

            #pragma shader_feature_local MASK
            #pragma shader_feature_local SECONDARY_UVS_MASK
            #pragma shader_feature_local MASK_RED_IS_ALPHA
            #pragma shader_feature_local _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD

            #pragma shader_feature_local MASK2
            #pragma shader_feature_local SECONDARY_UVS_MASK2
            #pragma shader_feature_local MASK2_RED_IS_ALPHA
            #pragma shader_feature_local _ _MASK2BLEND_ADD _MASK2BLEND_MASKED_ADD

            #pragma shader_feature_local _ _DISTORTION_SIMPLE

            #pragma shader_feature_local_fragment _ _CUTOUTTYPE_ALPHA_CLIP

            #pragma shader_feature_local_fragment SQUARE_ALPHA
            #pragma shader_feature_local_fragment VIEW_ALIGN_DISAPPEAR

            #pragma shader_feature_local_fragment _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG
            #pragma shader_feature_local_fragment REMAP_WHITEBOOST_START

            #pragma shader_feature_local_vertex _ _BILLBOARD_FULL _BILLBOARD_Y_AXIS _BILLBOARD_CAMERA_FACING
            #pragma shader_feature_local _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE

            #pragma shader_feature_local_fragment _ _FOGTYPE_LERP _FOGTYPE_COLOR _FOGTYPE_ALPHA
            #pragma shader_feature_local_fragment HEIGHT_FOG

            #pragma multi_compile_fragment _ BLOOM_FOG
            #define FOG defined(_FOGTYPE_LERP) || defined(_FOGTYPE_COLOR) || defined(_FOGTYPE_ALPHA)

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomBloom.hlsl"
            #include "ShaderLibrary/CustomTime.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"
            #include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"

            // SECONDARY_COLOR
            sampler2D _SecondaryColorTex;
            float4 _SecondaryColorTex_ST;
            float4 _SecondaryColorPanning;
            // --

            // COLOR_GRADIENT
            sampler2D _ColorGradient;
            float4 _ColorGradient_ST;
            float _GradientPosition;
            float _GradientPanningSpeed;
            // --

            // SPECTROGRAM_COLOR
            float _SpectrogramBaseValue;
            float _SpectrogramRange;
            // --

            // _SECONDARY_UVS_IMPORT
            float _EnableRotateUV;
            float _RotateUV;
            float _RotateMainUVOnly;
            // --

            // VERTEX_DISPLACEMENT
            sampler2D _DisplacementTex;
            float4 _DisplacementTex_ST;
            float _DisplacementStrength;
            // SPATIAL_DISPLACEMENT
            float4 _DisplacementAxes;
            // --
            float _DisplacementPanningSpeed;
            float4 _DisplacementPanning;
            // _SPECTROGRAM_FULL
            float _UV3Offset;
            float _UV3Scale;
            // --
            // --

            // MAIN_TEXTURE
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BaseLayer;
            // --

            // PIXELATE
            float2 _PixelateResolution;
            // --

            float _Intensity;
            float4 _UvPanning;

            // COLOR_ARRAY
            #if defined(COLOR_ARRAY)
            float4 _ColorsArray[150];
            float _ColorsArrayOffset;
            #endif
            // --

            // CUSTOM_WRAPPING
            float2 _CustomPadding;
            // --

            // TEXTURE_FLIPBOOK
            float _FlipbookColumns;
            float _FlipbookRows;
            float _FlipbookNonloopableFrames;
            float _FlipbookSpeed;
            // --

            // MASK
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            // --

            // MASK2
            sampler2D _Mask2Tex;
            float4 _Mask2Tex_ST;
            // --

            sampler2D _DistortionTex;
            float4 _DistortionTex_ST;

            #define USE_BILLBOARD defined(_BILLBOARD_FULL) || defined(_BILLBOARD_Y_AXIS) || defined(_BILLBOARD_CAMERA_FACING)

            

            #if defined(UNITY_INSTANCING_ENABLED)
            UNITY_INSTANCING_BUFFER_START (Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _SecondaryColor)
            UNITY_DEFINE_INSTANCED_PROP(float4, unity_SpriteRendererColorArray)
            UNITY_DEFINE_INSTANCED_PROP(half2, unity_SpriteFlipArray)
            UNITY_DEFINE_INSTANCED_PROP(float, _MaskStrength)
            UNITY_DEFINE_INSTANCED_PROP(float, _Mask2Strength)
            UNITY_DEFINE_INSTANCED_PROP(float, _TimeOffset)
            UNITY_DEFINE_INSTANCED_PROP(float, _MeshPackingId)
            UNITY_INSTANCING_BUFFER_END (Props)
            #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(Props, unity_SpriteRendererColorArray)
            #define _Flip           UNITY_ACCESS_INSTANCED_PROP(Props, unity_SpriteFlipArray)
            #endif

            CBUFFER_START(UnityPerMaterial)
                #if !defined(UNITY_INSTANCING_ENABLED)
                float4 _Color;
                float4 _SecondaryColor;
                float4 _RendererColor;
                half2 _Flip;
                float _MaskStrength;
                float _Mask2Strength;
                float _TimeOffset;
                float _MeshPackingId;
                #endif
                float _EnableExternalAlpha;
                float2 _MaskPanning;
                float4 _Mask2Panning;
                float4 _DistortionPanning;
                float _DistortionStrength;
                float4 _DistortionAxes;
                float _Cutout;
                float _AlphaMultiplier;
                float _SquareAngleForViewAlignDisappear;
                float _ViewAlignFactor;
                float _ViewAlignOffset;
                float _BloomMultiplier;
                float _BloomWhiteMultiplier;
                float _WhiteBoostRemapStart;
                float _QuestWhiteboostMultiplier;
                float _BaseColorBoost;
                float _BaseColorBoostThreshold;
                float _BillboardScale;
                float _FogStartOffset;
                float _FogScale;
                float _FogHeightOffset;
                float _FogHeightScale;
            CBUFFER_END

            struct appdata_t
            {
                float4 vertex : POSITION;
                #if defined(VERTEX_COLOR)
                float4 color : COLOR;
                #endif
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv1 : TEXCOORD0;
                #if defined(_SECONDARY_UVS_IMPORT)
                float2 uv2 : TEXCOORD1;
                #endif
                #if defined(_SPECTROGRAM_FULL) || defined(SPECTROGRAM_COLOR)
                float2 uv3 : TEXCOORD2;
                #endif
                #if defined(MESH_PACKING)
                float2 packingUv : TEXCOORD3;
                #endif
                #if defined(COLOR_ARRAY)
                float2 colorIndexUv : TEXCOORD4;  // ADD — encodes color index as (tens, units)
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if defined(VERTEX_COLOR)
                float4 color : COLOR;
                #endif
                #if defined(_SECONDARY_UVS_IMPORT)
                float4 uv : TEXCOORD0;
                #else
                float2 uv : TEXCOORD0;
                #endif
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;

                #if defined(COLOR_ARRAY)
                float2 colorIndexUv : TEXCOORD4;
                #endif

                #if defined(SPECTROGRAM_COLOR)
                float2 spectrogramUv : TEXCOORD5;
                #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float4 UnityFlipSprite(in float3 pos, in half2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }

            v2f vert(appdata_t i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                #if USE_BILLBOARD
                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

                // TODO: figure out what's the difference between the 2
                #if defined(_BILLBOARD_CAMERA_FACING) || defined(_BILLBOARD_FULL)
                // Transform only the object origin to view space (not the vertex)
                float4 viewOrigin = mul(UNITY_MATRIX_V, float4(worldOrigin, 1));
                // Only offset XY in view space — zero Z so depth stays anchored at the object origin.
                // Use vertex.xy only (ignore Z), and scale uniformly via _BillboardScale scalar.
                float4 billboardViewPos = viewOrigin + float4(i.vertex.xy * _BillboardScale, 0.0, 0.0);
                // Store real world-space position (from the unmodified vertex) for fog/lighting in frag.
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.vertex = mul(UNITY_MATRIX_P, billboardViewPos);
                #endif

                #if defined(_BILLBOARD_Y_AXIS)
                float3 localUp = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
                float3 dirToCam = _WorldSpaceCameraPos - worldOrigin;
                float3 look = normalize(dirToCam - localUp * dot(dirToCam, localUp));
                float3 right = -normalize(cross(localUp, look));

                o.worldPos = worldOrigin + right * i.vertex.x * _BillboardScale + localUp * i.vertex.y *
                    _BillboardScale;
                o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.0));
                #endif

                #else

                #if defined(VERTEX_DISPLACEMENT)
                float4 time = GET_TIME(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) / 2;
                float2 dispUV = TRANSFORM_TEX(i.uv1, _DisplacementTex) 
                              + _DisplacementPanning.xy * time.y * _DisplacementPanningSpeed;
                float3 dispSample = tex2Dlod(_DisplacementTex, float4(dispUV, 0, 0)).xyz * 2.0 - 1.0;

                #if defined(SPATIAL_DISPLACEMENT)
                float3 bitangent = i.tangent.yzx * i.normal.zxy - i.normal.yzx * i.tangent.zxy;
                float3 dispDir = dispSample.x * i.tangent.xyz
                               + dispSample.y * bitangent
                               + dispSample.z * i.normal.xyz;
                dispDir = normalize(dispDir);

                #if defined(_SPECTROGRAM_FLAT) || defined(_SPECTROGRAM_FULL)
                float spectrogramIndex = i.uv3.x * _UV3Scale + _UV3Offset;
                float4 audioData = AudioLinkLerpMultiline(ALPASS_DFT + uint2(spectrogramIndex * AUDIOLINK_ETOTALBINS, 0));
                float dispAmount = _DisplacementStrength * audioData.b*2;
                #else
                float dispAmount = _DisplacementStrength;
                #endif
                i.vertex.xyz += dispDir * dispAmount * _DisplacementAxes.xyz;
                #else
                i.vertex.y += dispSample.x * _DisplacementStrength;
                #endif
                #endif

                float angle, s, c;
                #if defined(_CURVE_VERTICES_AROUND_X)
                angle = i.vertex.y;
                sincos(angle, s, c);
                i.vertex.xyz = float3(i.vertex.x, i.vertex.y * c - i.vertex.z * s, i.vertex.y * s + i.vertex.z * c);
                float3 normal = float3(i.normal.x, i.normal.y * c - i.normal.z * s, i.normal.y * s + i.normal.z * c);
                #elif defined(_CURVE_VERTICES_AROUND_Y)
                angle = i.vertex.x;
                sincos(angle, s, c);
                i.vertex.xyz = float3(i.vertex.x * c - i.vertex.z * s, i.vertex.y, i.vertex.x * s + i.vertex.z * c);
                float3 normal = float3(i.normal.x * c - i.normal.z * s, i.normal.y, i.normal.x * s + i.normal.z * c);
                #elif defined(_CURVE_VERTICES_AROUND_Z)
                angle = i.vertex.y / i.vertex.x;
                sincos(angle, s, c);
                i.vertex.xyz = float3(i.vertex.x * c, i.vertex.x * s, i.vertex.z);
                float3 normal = float3(i.normal.x * c, i.normal.x * s, i.normal.z);
                #endif

                o.vertex = UnityFlipSprite(i.vertex, _Flip);
                o.worldPos = mul(unity_ObjectToWorld, o.vertex).xyz;
                o.vertex = UnityObjectToClipPos(o.vertex);
                #endif
                #if !defined(VERTEX_DISPLACEMENT)
                float4 time = GET_TIME(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) / 2;
                #endif
                #if defined(MAIN_TEXTURE)
                {
                    float2 panOffset = time.y * _UvPanning.xy * _MainTex_ST.xy;
                    o.uv.xy = i.uv1.xy * _MainTex_ST.xy + _MainTex_ST.zw + panOffset;
                }
                #else
                o.uv.xy = i.uv1.xy;
                #endif
                #if defined(_SECONDARY_UVS_IMPORT)
                o.uv.zw = i.uv2.xy;
                #endif
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                #if defined(VERTEX_COLOR)
                o.color = i.color * _RendererColor * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #if defined(VERTEX_RED_IS_ALPHA)
                o.color = float4(1, 1, 1, o.color.r);
                #endif
                #if defined(VERTEX_SQUARE_ALPHA)
                o.color.a *= o.color.a;
                #if defined(_VERTEXCHANNELS_A)
                o.color.rgb = 0;
                #elif defined(_VERTEXCHANNELS_RGB)
                o.color.a = 0;
                #endif
                #endif

                #endif
                #if defined(MESH_PACKING)
                float packingCull = abs(i.packingUv.y - UNITY_ACCESS_INSTANCED_PROP(Props, _MeshPackingId)) > 0.1;
                o.vertex.xyz = packingCull ? float3(0.0, 0.0, 0.0) : o.vertex.xyz;
                #endif

                #if defined(COLOR_ARRAY)
                o.colorIndexUv.x = i.colorIndexUv.x;
                o.colorIndexUv.y = i.colorIndexUv.y + _ColorsArrayOffset;
                #endif

                #if defined(SPECTROGRAM_COLOR)
                // Apply the same scale/offset that the displacement spectrogram path uses,
                // so _UV3Scale and _UV3Offset control which frequency band range is sampled.
                o.spectrogramUv.x = i.uv3.x * _UV3Scale + _UV3Offset;
                o.spectrogramUv.y = i.uv3.y;
                #endif

                /**
                float rawId = UNITY_ACCESS_INSTANCED_PROP (Props, _MeshPackingId);
                float meshPackingId = fmod (rawId, 10.0) ;
                float meshSubId = floor ((rawId + 0.001) / 10.0) ;
                #if defined (MESH_PACKING)
                float packingCull = (abs (i.packingUv.y - meshPackingId) > 0.1) ||
                (abs (i.packingUv.x - meshSubId) > 0.1) ;
                o.vertex.xyz = packingCull ? float3(0.0, 0.0, 0.0) : o.vertex.xyz;
                #endif
                **/


                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 time = GET_TIME(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset))/2;

                #if defined(_SECONDARY_UVS_IMPORT)
                // TODO: secondary uv stuff
                float2 uv2 = i.uv.zw;
                #else
                float2 uv2 = i.uv.xy;
                #endif

                #if defined(VERTEX_COLOR)
                float4 color = i.color;
                #elif defined(COLOR_ARRAY)
                // Decode packed index: tens digit in x, units digit in y (with offset applied in vert)
                float _colorIdx = round(i.colorIndexUv.x * 10.0 + i.colorIndexUv.y);
                float4 color = _ColorsArray[_colorIdx];
                #else
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * _RendererColor;
                color.rgb *= _Intensity;
                #endif

                #if !defined(TEXTURE_FLIPBOOK) && defined(TEXTURE_COLOR)
                float4 albedo = float4(1, 1, 1, color.a);
                #else
                float4 albedo = color;
                #endif
                #if defined(MAIN_TEXTURE)
                #if defined(PIXELATE)
                float2 uv = floor(i.uv.xy * _PixelateResolution) / _PixelateResolution;
                #else
                // Step 1: start from interpolated UV
                float2 uv = i.uv.xy;
                // Step 2: apply distortion to base UV first, so flipbook inherits it
                #if defined(_DISTORTION_SIMPLE)
                {
                    float2 distortScrollUv = uv * _DistortionTex_ST.xy + _DistortionTex_ST.zw
                                           + time.y * _DistortionPanning.xy * _DistortionTex_ST.xy;
                    float2 distortionSample = tex2D(_DistortionTex, distortScrollUv).rg;
                    uv += (distortionSample * 2.0 - 1.0) * (_DistortionStrength * 0.1) * _DistortionAxes.xy;
                }
                #endif
                #endif
                // Step 3: apply flipbook offset on top of (potentially distorted) UV
                #if defined(TEXTURE_FLIPBOOK)
                {
                    float2 flipUv = uv;
                    flipUv.x /= _FlipbookColumns;
                    flipUv.y /= _FlipbookRows;
                    float flipbookTime = time.y * _FlipbookSpeed;
                    flipUv += float2(
                        floor(flipbookTime % _FlipbookColumns) / _FlipbookColumns,
                        ((_FlipbookRows - 1.0) - floor(flipbookTime / _FlipbookColumns) % _FlipbookRows) / _FlipbookRows
                    );
                    #if defined(CUSTOM_WRAPPING)
                    // TODO: custom wrapping with flipbook
                    #endif
                    #if defined(TEXTURE_COLOR)
                    float4 _texSample = tex2D(_MainTex, flipUv) * _BaseLayer;
                    // Frame blending: sample next frame and lerp by sub-frame fraction
                    #if !defined(FLIPBOOK_BLENDING_OFF)
                    {
                        float2 flipUv2 = uv;
                        flipUv2.x /= _FlipbookColumns;
                        flipUv2.y /= _FlipbookRows;
                        flipUv2 += float2(
                            floor((flipbookTime + 1) % _FlipbookColumns) / _FlipbookColumns,
                            ((_FlipbookRows - 1.0) - floor((flipbookTime + 1) / _FlipbookColumns) % _FlipbookRows) / _FlipbookRows
                        );
                        _texSample = lerp(_texSample, tex2D(_MainTex, flipUv2) * _BaseLayer, frac(flipbookTime));
                    }
                    #endif
                    albedo.rgb *= _texSample.rgb;
                    #if defined(_ALPHACHANNEL_RED)
                    albedo.a *= _texSample.r;
                    #else
                    albedo.a *= _texSample.a;
                    #endif
                    #else
                    // Non-texture-color: only alpha channel drives transparency
                    float4 _texSample = tex2D(_MainTex, flipUv);
                    #if !defined(FLIPBOOK_BLENDING_OFF)
                    {
                        float2 flipUv2 = uv;
                        flipUv2.x /= _FlipbookColumns;
                        flipUv2.y /= _FlipbookRows;
                        flipUv2 += float2(
                            floor((flipbookTime + 1) % _FlipbookColumns) / _FlipbookColumns,
                            ((_FlipbookRows - 1.0) - floor((flipbookTime + 1) / _FlipbookColumns) % _FlipbookRows) / _FlipbookRows
                        );
                        _texSample = lerp(_texSample, tex2D(_MainTex, flipUv2), frac(flipbookTime));
                    }
                    #endif
                    #if defined(_ALPHACHANNEL_RED)
                    albedo.a *= _texSample.r * _BaseLayer;
                    #else
                    albedo *= _texSample.a * _BaseLayer;
                    #endif
                    #endif
                }
                #else
                // Non-flipbook path: sample using distorted uv
                #if defined(CUSTOM_WRAPPING)
                // TODO: honestly, how does this work
                #endif
                #if defined(TEXTURE_COLOR)
                // Sample full RGBA — RGB multiplies into color, alpha drives transparency
                float4 _texSample = tex2D(_MainTex, uv) * _BaseLayer;
                albedo.rgb *= _texSample.rgb;
                #if defined(_ALPHACHANNEL_RED)
                albedo.a *= _texSample.r;
                #else
                albedo.a *= _texSample.a;
                #endif
                #else
                // Non-texture-color: only alpha channel drives transparency
                #if defined(_ALPHACHANNEL_RED)
                    albedo.a *= tex2D(_MainTex, uv).r * _BaseLayer;
                #else
                    albedo *= tex2D(_MainTex, uv).a * _BaseLayer;
                #endif
                #endif
                #endif
                #endif

                #if defined(SECONDARY_COLOR)
                float4 secondaryColorTex = tex2D(_SecondaryColorTex, TRANSFORM_TEX(i.uv, _SecondaryColorTex) + _SecondaryColorPanning * time.yy);
                float3 blendedColor = lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _Color).rgb, UNITY_ACCESS_INSTANCED_PROP(Props, _SecondaryColor).rgb,
                    saturate(secondaryColorTex.r));
                albedo.rgb *= blendedColor;
                #endif

                #if defined(COLOR_GRADIENT)
                albedo.rgb += tex2D(_ColorGradient,
                                    TRANSFORM_TEX(i.uv, _ColorGradient) + _GradientPosition.xx * time.yy)
                    .rgb;
                #endif

                #if defined(MASK)
                #if defined(SECONDARY_UVS_MASK)
                float2 maskUv = uv2.xy;
                #else
                float2 maskUv = i.uv.xy;
                #endif
                float4 _maskSample = tex2D(_MaskTex, TRANSFORM_TEX(maskUv, _MaskTex) + _MaskPanning * time.yy);
                float4 mask = lerp(float4(1,1,1,1), _maskSample, UNITY_ACCESS_INSTANCED_PROP(Props, _MaskStrength));
                #if defined(MASK_RED_IS_ALPHA)
                mask.a = mask.r;
                mask.rgb = 0;
                #endif
                #if defined(_MASKBLEND_ADD)
                albedo.rgb += mask.rgb;
                albedo.a *= mask.a;
                #elif defined(_MASKBLEND_MASKED_ADD)
                albedo.rgb += albedo.rgb * mask.rgb;
                albedo.a *= mask.a;
                #else
                #if defined(MASK_RED_IS_ALPHA)
                albedo.a *= mask.a;
                #else
                albedo *= mask;
                #endif
                #endif
                #endif

                #if defined(MASK2)
                #if defined(SECONDARY_UVS_MASK2)
                float2 mask2Uv = uv2.xy;
                #else
                float2 mask2Uv = i.uv.xy;
                #endif
                float4 _mask2Sample = tex2D(_Mask2Tex, TRANSFORM_TEX(mask2Uv, _Mask2Tex) + _Mask2Panning * time.yy);
                float4 mask2 = lerp(float4(1,1,1,1), _mask2Sample, UNITY_ACCESS_INSTANCED_PROP(Props, _Mask2Strength));
                #if defined(MASK2_RED_IS_ALPHA)
                mask2.a = mask2.r;
                mask2.rgb = 1;
                #endif
                #if defined(_MASK2BLEND_ADD)
                albedo.rgb += mask2.rgb;
                albedo.a *= mask2.a;
                #elif defined(_MASK2BLEND_MASKED_ADD)
                albedo.rgb += albedo.rgb * mask2.rgb;
                albedo.a *= mask2.a;
                #else
                #if defined(MASK2_RED_IS_ALPHA)
                albedo.a *= mask2.a;
                #else
                albedo *= mask2;
                #endif
                #endif
                #endif

                albedo.a *= _AlphaMultiplier;

                #if defined(SQUARE_ALPHA)
                albedo.a *= albedo.a;
                #endif

                #if defined(SPECTROGRAM_COLOR)
                {
                    float binIndex = i.spectrogramUv.x * AUDIOLINK_ETOTALBINS;
                    float4 audioData = AudioLinkLerpMultiline(ALPASS_DFT + uint2(binIndex, 0));
                    float binValue = audioData.b; // blue channel = amplitude

                    // smoothstep remap: how far up the bar is this fragment?
                    float rangeDivisor = 1.0 / max(binValue - _SpectrogramRange * binValue, 0.0001);
                    float t = saturate(rangeDivisor * (i.spectrogramUv.y - _SpectrogramRange * binValue));
                    float sm = t * t * (3.0 - 2.0 * t);
                    float brightness = sm * binValue * 1.5;
                    brightness = max(brightness, _SpectrogramBaseValue);
                    // Fragments above the bar peak are hidden via alpha only, not by zeroing RGB.
                    // Previously `albedo *= mask * brightness` killed both RGB and alpha when mask=0,
                    // making the color vanish even though alpha-blending would have discarded it anyway.
                    // Matches CustomParticles where brightness scaled vertex color intensity and the
                    // bar mask only gated the final alpha output.
                    float barMask = (float)(binValue >= i.spectrogramUv.y);
                    albedo.rgb *= brightness;
                    albedo.a   *= barMask * brightness;
                }
                #endif

                #if defined(_CUTOUTTYPE_ALPHA_CLIP)
                if (albedo.a < _Cutout) discard;
                #endif

                #if defined(REMAP_WHITEBOOST_START)
                {
                    float remapped = (albedo.a * _QuestWhiteboostMultiplier - _WhiteBoostRemapStart)
                                     / (1.0 - _WhiteBoostRemapStart);
                    remapped = max(remapped, 0.0);
                    float boost = remapped * remapped * _BaseColorBoost - _BaseColorBoostThreshold;
                    // Previously boost was computed but never used — albedo.rgb only got the
                    // saturate(rgb*alpha) term. Now we add the white boost, matching CustomParticles.
                    albedo.rgb = saturate(albedo.rgb * albedo.a + boost);
                }
                #endif

                #if defined(_BLOOMTYPE_PP)
                CUSTOM_BLOOM_PP_APPLY(albedo, _BloomMultiplier);
                #elif defined(_BLOOMTYPE_FRAG)
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _BloomWhiteMultiplier);
                #else
                CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(albedo);
                #endif

                ACES_TONE_MAPPING_APPLY(albedo);

                #if FOG
                {
                    float _fogFactor = 1.0;

                    #if defined(HEIGHT_FOG)
                    {
                        // Exact CustomParticles formula
                        float _hf = i.worldPos.y * _FogHeightScale + _FogHeightOffset;
                        _hf = _hf - (_CustomFogHeightFogHeight + _CustomFogHeightFogStartY);
                        _hf = saturate(_hf / _CustomFogHeightFogHeight);
                        float _hfSq = _hf * _hf;
                        _hf = -_hf * 2.0 + 3.0;
                        _hf = -_hfSq * _hf + 1.0; // smoothstep: 1=bottom(fogged), 0=top(clear)
                        _fogFactor = 1.0 - _hf;    // invert: 0=bottom(fade), 1=top(visible)
                    }
                    #else
                    // Distance-only fog when height fog is off
                    {
                        float3 _toFrag = i.worldPos.xyz - _WorldSpaceCameraPos;
                        float _distSq  = max(dot(_toFrag, _toFrag) - _FogStartOffset, 0.0);
                        _fogFactor = 1.0 / (_distSq * _FogScale + 1.0);
                    }
                    #endif

                    #if defined(_FOGTYPE_ALPHA)
                    albedo *= _fogFactor;
                    #elif defined(_FOGTYPE_COLOR)
                    albedo.rgb *= _fogFactor;
                    #else
                    albedo *= _fogFactor;
                    #endif
                }
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}