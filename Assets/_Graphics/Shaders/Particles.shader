Shader "ChroMapper/Particles"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        [Space(20)]
        [Toggle(SECONDARY_COLOR)] _EnableSecondaryColor ("Use Secondary Color", float) = 0
        _SecondaryColor ("Secondary Color", Color) = (1,1,1,1)
        _SecondaryColorTex ("Secondary Color Texture", 2D) = "white" {}
        _SecondaryColorPanning ("Secondary Color Panning", Vector) = (0,0,0,0)

        [Space(20)]
        [Toggle(COLOR_GRADIENT)] _UseColorGradient ("Use Color Gradient", float) = 0
        _ColorGradient ("Gradient LUT", 2D) = "white" {}
        _GradientPosition ("Gradient Position", Range(0, 1)) = 0.5
        _GradientPanningSpeed ("Gradient Panning Speed", float) = 0

        [Space(20)]
        [Toggle(SPECTROGRAM_COLOR)] _UseSpectrogram ("Color by Spectrogram", float) = 0
        _SpectrogramBaseValue ("Spectrogram Base Value", Range(0, 1)) = 0.2
        _SpectrogramRange ("Spectrogram Range", Range(0, 1)) = 0.2

        [Space(20)]
        [Toggle(COLOR_ARRAY)] _UseColorArray ("Use Color Array", float) = 0

        [Space(20)]
        [KeywordEnum(None, Import)] _Secondary_UVs ("Secondary UVs", float) = 0
        [Toggle] _EnableRotateUV ("Rotate UVs 90", float) = 0
        _RotateUV ("Rotation Angle", float) = 0
        [Toggle] _RotateMainUVOnly ("Rotate Main UV Only", float) = 0



        [Header(Vertex)] [Space]
        [Toggle(VERTEX_COLOR)] _EnableVertexColor ("Vertex Color", float) = 0
        [Toggle(VERTEX_SQUARE_ALPHA)] _SquareVertexAlpha ("Square Vertex Alpha", float) = 0
        [Toggle(VERTEX_RED_IS_ALPHA)] _RedIsVertexAlpha ("Red is Vertex Alpha", float) = 0
        [KeywordEnum(RGBA, A, RGB)] _VertexChannels ("Vertex Channels", float) = 0

        [Space(20)]
        [Toggle(VERTEX_DISPLACEMENT)] _VertexDisplacement ("Use Vertex Displacement", float) = 0
        _DisplacementTex ("Displacement Texture", 2D) = "white" {}
        [Toggle(SPATIAL_DISPLACEMENT)] _3DDisplacement ("3D Displacement", float) = 0
        _DisplacementStrength ("Strength", float) = 0.1
        _DisplacementAxes ("Per Axis Strength", Vector) = (1,1,1,0)
        _DisplacementPanningSpeed ("Panning Speed", float) = 1
        _DisplacementPanning ("Panning", Vector) = (0,0,0,0)
        [KeywordEnum(None, Flat, Full)] _Spectrogram ("Spectrogram Influence", float) = 0
        _UV3Offset ("UV3 Offset", float) = 0
        _UV3Scale ("UV3 Scale", float) = 1

        [Space(20)]
        [KeywordEnum(None, Around_X, Around_Y, Around_Z)] _Curve_Vertices ("Curve Vertices (Object Space)", float) = 0



        [Header(Texture)] [Space]
        [Toggle(MAIN_TEXTURE)] _UseMainTex ("Base Texture", float) = 1
        _BaseLayer ("Base Color", float) = 1
        _MainTex ("Texture", 2D) = "white" {}

        [Space(20)]
        [Toggle(PIXELATE)] _Pixelate ("Pixelate", float) = 0
        _PixelateResolution ("Pixelate Resolution", Vector) = (64,64,0,0)

        [Space(20)]
        [Toggle(TEXTURE_COLOR)] _EnableTextureColor ("Use Texture Color", float) = 0
        [KeywordEnum(Alpha, Red)] _AlphaChannel ("Alpha Channel", float) = 0

        [Space(20)]
        _Intensity("Color Intensity", float) = 1
        _UvPanning ("UV Panning", Vector) = (0,0,0,0)

        [Space(20)]
        [Toggle(CUSTOM_WRAPPING)] _EnableCustomPadding ("Custom Repeat Wrapping", float) = 0
        _CustomPadding ("Custom Padding", Vector) = (0,0,0,0)

        [Space(20)]
        [Toggle(TEXTURE_FLIPBOOK)] _UseTextureFlipbook ("Use Texture Flipbook", float) = 0
        _FlipbookColumns ("Flipbook Columns", float) = 8
        _FlipbookRows ("Flipbook Rows", float) = 8
        _FlipbookNonloopableFrames ("Full Non-loopable frames", float) = 0
        _FlipbookSpeed ("Flipbook Speed", float) = 1
        [Toggle(FLIPBOOK_BLENDING_OFF)] _FlipbookBlendingOff ("No Frame Blending", float) = 0

        [Space(20)]
        [Toggle(MASK)] _EnableMask ("Mask", float) = 0
        [Toggle(SECONDARY_UVS_MASK)] _MaskSecondaryUVs ("Use Secondary UVs", float) = 0
        [Toggle(MASK_RED_IS_ALPHA)] _MaskRedIsAlpha ("Red is Alpha", float) = 0
        [KeywordEnum(Multiply, Add, Masked Add)] _MaskBlend ("Mask Blend", float) = 0
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskStrength ("Mask Strength", float) = 1
        _MaskPanning ("Mask Panning", Vector) = (0,0,0,0)

        [Space(20)]
        [Toggle(MASK2)] _EnableMask2 ("Secondary Mask", float) = 0
        [Toggle(SECONDARY_UVS_MASK2)] _Mask2SecondaryUVs ("Use Secondary UVs", float) = 0
        [Toggle(MASK2_RED_IS_ALPHA)] _Mask2RedIsAlpha ("Red is Alpha", float) = 0
        [KeywordEnum(Multiply, Add, Masked Add)] _Mask2Blend ("Secondary Mask Blend", float) = 0
        _Mask2Tex ("Secondary Mask Texture", 2D) = "white" {}
        _Mask2Strength ("Secondary Mask Strength", float) = 1
        _Mask2Panning ("Secondary Mask Panning", Vector) = (0,0,0,0)

        [Space(20)]
        [KeywordEnum(None, Simple)] _Distortion ("Distortion", float) = 0
        _DistortionTex ("Distortion Texture", 2D) = "black" {}
        _DistortionStrength ("Distortion Strength", float) = 0.2
        _DistortionAxes ("Distortion Axes", Vector) = (1, 1, 0, 0)
        _DistortionPanning ("Distortion Panning", Vector) = (0, 0, 0, 0)

        [Header(Dissolve)] [Space]
        [KeywordEnum(None, Alpha Clip)] _CutoutType ("Cutout", float) = 0
        _Cutout ("Threshold", Range(0, 1)) = 0.5



        [Header(Alpha Handling)] [Space]
        _AlphaMultiplier ("Alpha Multiplier", float) = 1
        [Toggle(SQUARE_ALPHA)] _SquareAlpha("Square Alpha", float) = 1

        [Space(20)]
        [Toggle(VIEW_ALIGN_DISAPPEAR)] _EnableViewAlignDisappear ("View Align Disappear", float) = 0
        [Toggle] _SquareAngleForViewAlignDisappear ("Square Angle", float) = 0
        _ViewAlignFactor ("View Align Factor", float) = 1.5
        _ViewAlignOffset ("View Align Offset", float) = 0



        [Header(Others)] [Space]
        [KeywordEnum(None, PP, Frag)] _BloomType ("Bloom Type", float) = 0
        _BloomWhiteMultiplier ("White Multiplier", float) = 1
        _BloomMultiplier ("Bloom Multiplier", float) = 1
        [Toggle(REMAP_WHITEBOOST_START)] _EnableRemapWhiteBoostStart ("Remap White Boost Start", float) = 0
        _WhiteBoostRemapStart ("Alpha for no White Boost", Range(0, 1)) = 0

        [Space(20)]
        [KeywordEnum(None, Full, Y Axis, Camera Facing)] _Billboard ("Billboard", float) = 0
        _BillboardScale ("Billboard Scale", float) = 1

        [Space(20)]
        [KeywordEnum(Standard, Song Time, Freeze)] _Custom_Time ("Time Behavior", float) = 0



        [Header(Fog Settings)] [Space]
        [KeywordEnum(None, Lerp, Color, Alpha)] _FogType ("Fog Type", float) = 0
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Toggle(MESH_PACKING)] _MeshPacking ("Use Mesh Packed Instancing", Float) = 0
        _MeshPackingId ("Mesh Packing ID", float) = 0



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

            #pragma shader_feature_local_fragment SECONDARY_COLOR

            #pragma shader_feature_local_fragment COLOR_GRADIENT

            #pragma shader_feature_local_fragment SPECTROGRAM_COLOR

            #pragma shader_feature_local_fragment COLOR_ARRAY

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

            #pragma shader_feature_local_fragment MAIN_TEXTURE

            #pragma shader_feature_local_fragment PIXELATE

            #pragma shader_feature_local_fragment TEXTURE_COLOR
            #pragma shader_feature_local_fragment _ _ALPHACHANNEL_RED

            #pragma shader_feature_local_fragment CUSTOM_WRAPPING

            #pragma shader_feature_local_fragment TEXTURE_FLIPBOOK
            #pragma shader_feature_local_fragment FLIPBOOK_BLENDING_OFF

            #pragma shader_feature_local_fragment MASK
            #pragma shader_feature_local_fragment SECONDARY_UVS_MASK
            #pragma shader_feature_local_fragment MASK_RED_IS_ALPHA
            #pragma shader_feature_local_fragment _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD

            #pragma shader_feature_local_fragment MASK2
            #pragma shader_feature_local_fragment SECONDARY_UVS_MASK2
            #pragma shader_feature_local_fragment MASK2_RED_IS_ALPHA
            #pragma shader_feature_local_fragment _ _MASK2BLEND_ADD _MASK2BLEND_MASKED_ADD

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
            #define FOG defined(BLOOM_FOG) && (defined(_FOGTYPE_LERP) || defined(_FOGTYPE_COLOR) || defined(_FOGTYPE_ALPHA))

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
            float2 _MaskPanning;
            // --

            // MASK2
            sampler2D _Mask2Tex;
            float4 _Mask2Tex_ST;
            float4 _Mask2Panning;
            // --

            sampler2D _DistortionTex;
            float4 _DistortionTex_ST;
            float2 _DistortionPanning;
            float _DistortionStrength;
            float2 _DistortionAxes;

            // _CUTOUTTYPE_ALPHA_CLIP
            float _Cutout;
            // --

            float _AlphaMultiplier;

            // VIEW_ALIGN_DISAPPEAR
            float _SquareAngleForViewAlignDisappear;
            float _ViewAlignFactor;
            float _ViewAlignOffset;
            // --

            float _BloomMultiplier;
            float _BloomWhiteMultiplier;
            // REMAP_WHITEBOOST_START
            float _WhiteBoostRemapStart;
            // --

            #define USE_BILLBOARD defined(_BILLBOARD_FULL) || defined(_BILLBOARD_Y_AXIS) || defined(_BILLBOARD_CAMERA_FACING)
            // USE_BILLBOARD
            float _BillboardScale;
            // --

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            

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
                #if defined(_SPECTROGRAM_FULL)
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

                #if defined(_DISTORTION_SIMPLE)
                float2 distortionUv : TEXCOORD3;
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
                float3 viewPos = mul(UNITY_MATRIX_V, float4(worldOrigin, 1)).xyz;
                float3 billboardPos = viewPos + i.vertex.xyz * _BillboardScale;
                o.worldPos = billboardPos;
                o.vertex = mul(UNITY_MATRIX_P, float4(billboardPos, 1));
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
                o.uv.xy = i.uv1.xy;
                #if defined(_DISTORTION_SIMPLE)
                    #if !defined(VERTEX_DISPLACEMENT)
                    float4 time = GET_TIME(UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset)) / 2;
                    #endif
                    float2 distortionPanOffset = time.y * _DistortionPanning * _DistortionTex_ST.xy;
                    o.distortionUv = i.uv1.xy * _DistortionTex_ST.xy + _DistortionTex_ST.zw
                                     + distortionPanOffset * 0.1;
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
                #else
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * _RendererColor;
                #endif

                #if !defined(TEXTURE_FLIPBOOK) && defined(TEXTURE_COLOR)
                float4 albedo = float4(1, 1, 1, color.a);
                #else
                float4 albedo = color;
                #endif
                #if defined(_DISTORTION_SIMPLE)
                float2 distortionSample = tex2D(_DistortionTex, i.distortionUv).rg;
                #endif
                #if defined(MAIN_TEXTURE)
                #if defined(PIXELATE)
                float2 uv = floor(i.uv * _PixelateResolution) / _PixelateResolution;
                #else
                #if defined(_DISTORTION_SIMPLE)
                float2 uv = (distortionSample * (_DistortionStrength * 0.1) * _DistortionAxes 
            * 2.0 + i.uv.xy) - 1.0;
                #else
                float2 uv = i.uv.xy;
                #endif
                #endif
                #if defined(TEXTURE_FLIPBOOK)
                uv.x /= _FlipbookColumns;
                uv.y /= _FlipbookRows;
                float flipbookTime = time.y * _FlipbookSpeed;
                uv += float2(floor(flipbookTime % _FlipbookColumns) / _FlipbookColumns,
                             floor(flipbookTime / _FlipbookColumns) % _FlipbookRows /
                             _FlipbookRows);
                #endif
                // TODO: honestly, how does this work
                #if defined(CUSTOM_WRAPPING)
                #endif
                #if !defined(TEXTURE_COLOR)
                #if defined(_ALPHACHANNEL_RED)
                    albedo.a *= tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex) + _UvPanning * time.yy).r * _BaseLayer;
                #else
                    albedo *= tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex) + _UvPanning * time.yy).a * _BaseLayer;
                #endif
                #endif
                #endif
                albedo.rgb *= _Intensity;

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
                float2 maskUv = i.uv.xy;
                #else
                float2 maskUv = uv2.xy;
                #endif
                float4 mask = tex2D(_MaskTex, TRANSFORM_TEX(maskUv, _MaskTex) + _MaskPanning * time.yy) *
                    UNITY_ACCESS_INSTANCED_PROP(Props, _MaskStrength);
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
                float2 mask2Uv = i.uv.xy;
                #else
                float2 mask2Uv = uv2.xy;
                #endif
                float4 mask2 = tex2D(_Mask2Tex, TRANSFORM_TEX(mask2Uv, _Mask2Tex) + _Mask2Panning * time.yy) *
                    UNITY_ACCESS_INSTANCED_PROP(Props, _Mask2Strength);
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

                #if defined(_CUTOUTTYPE_ALPHA_CLIP)
                if (albedo.a < _Cutout) discard;
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