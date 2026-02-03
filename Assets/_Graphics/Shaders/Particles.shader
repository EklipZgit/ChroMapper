Shader "ChroMapper/Particles"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        [Space(20)]
        [Toggle(SECONDARY_COLOR)] _EnableSecondaryColor ("Use Secondary Color", float) = 0
        _SecondaryColor ("Secondary Color", Vector) = (1,1,1,1)
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
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1



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

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            #pragma shader_feature SECONDARY_COLOR

            #pragma shader_feature COLOR_GRADIENT

            #pragma shader_feature SPECTROGRAM_COLOR

            #pragma shader_feature COLOR_ARRAY

            #pragma multi_compile _ _SECONDARY_UVS_IMPORT

            #pragma shader_feature VERTEX_COLOR
            #pragma shader_feature VERTEX_SQUARE_ALPHA
            #pragma shader_feature VERTEX_RED_IS_ALPHA
            #pragma multi_compile _ _VERTEXCHANNELS_A _VERTEXCHANNELS_RGB

            #pragma shader_feature VERTEX_DISPLACEMENT
            #pragma shader_feature SPATIAL_DISPLACEMENT
            #pragma multi_compile _ _SPECTROGRAM_FLAT _SPECTROGRAM_FULL

            #pragma multi_compile _ _CURVE_VERTICES_AROUND_X _CURVE_VERTICES_AROUND_Y _CURVE_VERTICES_AROUND_Z

            #pragma shader_feature MAIN_TEXTURE

            #pragma shader_feature PIXELATE

            #pragma shader_feature CUSTOM_WRAPPING

            #pragma shader_feature TEXTURE_FLIPBOOK
            #pragma shader_feature FLIPBOOK_BLENDING_OFF

            #pragma shader_feature MASK
            #pragma shader_feature SECONDARY_UVS_MASK
            #pragma shader_feature MASK_RED_IS_ALPHA
            #pragma multi_compile _ _MASKBLEND_ADD _MASKBLEND_MASKED_ADD

            #pragma shader_feature MASK2
            #pragma shader_feature SECONDARY_UVS_MASK2
            #pragma shader_feature MASK2_RED_IS_ALPHA
            #pragma multi_compile _ _MASKBLEND2_ADD _MASKBLEND2_MASKED_ADD

            #pragma shader_feature SQUARE_ALPHA
            #pragma shader_feature VIEW_ALIGN_DISAPPEAR

            #pragma multi_compile _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG
            #pragma shader_feature REMAP_WHITEBOOST_START

            #pragma multi_compile _ _BILLBOARD_FULL _BILLBOARD_Y_AXIS _BILLBOARD_CAMERA_FACING
            #pragma multi_compile _ _CUSTOM_TIME_SONG_TIME _CUSTOM_TIME_FREEZE

            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ _FOGTYPE_LERP _FOGTYPE_COLOR _FOGTYPE_ALPHA
            #define ENABLE_FOG defined(_FOGTYPE_LERP) || defined(_FOGTYPE_COLOR) || defined(_FOGTYPE_ALPHA)
            #pragma multi_compile _ ENABLE_HEIGHT_FOG

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/CustomTime.cginc"
            #include "CGIncludes/CustomTonemapping.cginc"

            #if defined(SECONDARY_COLOR)
            #if !defined(UNITY_INSTANCING_ENABLED)
            float4 _SecondaryColor;
            #endif
            float4 _SecondaryColor;
            sampler2D _SecondaryColorTex;
            float4 _SecondaryColorTex_ST;
            float4 _SecondaryColorPanning;
            #endif

            #if defined(COLOR_GRADIENT)
            sampler2D _ColorGradient;
            float4 _ColorGradient_ST;
            float _GradientPosition;
            float _GradientPanningSpeed;
            #endif

            #if defined(SPECTROGRAM_COLOR)
            float _SpectrogramBaseValue;
            float _SpectrogramRange;
            #endif

            #if defined(_SECONDARY_UVS_IMPORT)
            float _EnableRotateUV;
            float _RotateUV;
            float _RotateMainUVOnly;
            #endif

            #if defined(VERTEX_DISPLACEMENT)
            sampler2D _DisplacementTex;
            float4 _DisplacementTex_ST;
            float _DisplacementStrength;
            #if defined(SPATIAL_DISPLACEMENT)
            float4 _DisplacementAxes;
            #endif
            float _DisplacementPanningSpeed;
            float4 _DisplacementPanning;
            #if defined(SPECTROGRAM_FULL)
            float _UV3Offset;
            float _UV3Scale;
            #endif
            #endif

            #if defined(MAIN_TEXTURE)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            #endif

            #if defined(PIXELATE)
            float4 _PixelateResolution;
            #endif

            float _Intensity;
            float4 _UvPanning;

            #if defined(CUSTOM_WRAPPING)
            float4 _CustomPadding;
            #endif

            #if defined(TEXTURE_FLIPBOOK)
            float _FlipbookColumns;
            float _FlipbookRows;
            float _FlipbookNonloopableFrames;
            float _FlipbookSpeed;
            #endif

            #if defined(MASK)
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            #if !defined(UNITY_INSTANCING_ENABLED)
            float _MaskStrength;
            #endif
            float4 _MaskPanning;
            #endif

            #if defined(MASK2)
            sampler2D _Mask2Tex;
            float4 _Mask2Tex_ST;
            #if !defined(UNITY_INSTANCING_ENABLED)
            float _Mask2Strength;
            #endif
            float4 _Mask2Panning;
            #endif

            float _AlphaMultiplier;

            #if defined(VIEW_ALIGN_DISAPPEAR)
            float _SquareAngleForViewAlignDisappear;
            float _ViewAlignFactor;
            float _ViewAlignOffset;
            #endif

            float _BloomMultiplier;
            float _BloomWhiteMultiplier;
            #if defined(REMAP_WHITEBOOST_START)
            float _WhiteBoostRemapStart;
            #endif

            #define USE_BILLBOARD defined(_BILLBOARD_FULL) || defined(_BILLBOARD_Y_AXIS) || defined(_BILLBOARD_CAMERA_FACING)
            #if USE_BILLBOARD
            float _BillboardScale;
            #endif

            #if !defined(UNITY_INSTANCING_ENABLED)
            float _TimeOffset;
            #endif

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            #if defined(UNITY_INSTANCING_ENABLED)
            UNITY_INSTANCING_BUFFER_START (Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            #if defined(SECONDARY_COLOR)
            UNITY_DEFINE_INSTANCED_PROP(float4, _SecondaryColor)
            #endif
            UNITY_DEFINE_INSTANCED_PROP(float4, unity_SpriteRendererColorArray)
            UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
            #if defined(MASK)
            UNITY_DEFINE_INSTANCED_PROP(float, _MaskStrength)
            #endif
            #if defined(MASK2)
            UNITY_DEFINE_INSTANCED_PROP(float, _Mask2Strength)
            #endif
            UNITY_DEFINE_INSTANCED_PROP(float, _TimeOffset)
            UNITY_INSTANCING_BUFFER_END (Props)
            #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(Props, unity_SpriteRendererColorArray)
            #define _Flip           UNITY_ACCESS_INSTANCED_PROP(Props, unity_SpriteFlipArray)
            #endif

            CBUFFER_START(UnityProps)
                #if !defined(UNITY_INSTANCING_ENABLED)
                float4 _Color;
                float4 _RendererColor;
                fixed2 _Flip;
                #endif
                float _EnableExternalAlpha;
            CBUFFER_END

            struct appdata_t
            {
                float4 vertex : POSITION;
                #if defined(VERTEX_COLOR)
                float4 color : COLOR;
                #endif
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if defined(VERTEX_COLOR)
                float4 color : COLOR;
                #endif
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 customScreenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float4 UnityFlipSprite(in float3 pos, in fixed2 flip)
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
                o.vertex = UnityFlipSprite(i.vertex, _Flip);
                o.worldPos = mul(unity_ObjectToWorld, o.vertex).xyz;
                o.vertex = UnityObjectToClipPos(o.vertex);
                #endif
                o.uv = i.texcoord;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);
                
                #if defined(VERTEX_COLOR)
                o.color = i.color * _RendererColor * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #if defined(VERTEX_RED_IS_ALPHA)
                o.color.a = o.color.r;
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

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                #if defined(_SECONDARY_UVS_IMPORT)
                // TODO: secondary uv stuff
                float2 uv2 = i.uv;
                #else
                float2 uv2 = i.uv;
                #endif
                
                #if defined(VERTEX_COLOR)
                float4 color = i.color;
                #else
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                #endif
                color.rgb *= _Intensity;

                float4 albedo = color;
                #if defined(MAIN_TEXTURE)
                #if defined(PIXELATE)
                float2 uv = floor(i.uv * _PixelateResolution) / _PixelateResolution;
                #else
                float2 uv = i.uv;
                #endif
                // TODO: honestly, how does this work
                #if defined(CUSTOM_WRAPPING)
                #endif
                albedo *= tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex));
                #endif
                
                #if defined(MASK)
                #if defined(SECONDARY_UVS_MASK)
                float4 mask = tex2D(_MaskTex, TRANSFORM_TEX(uv2 + _MaskPanning, _MaskTex)) * UNITY_ACCESS_INSTANCED_PROP(Props, _MaskStrength);
                #else
                float4 mask = tex2D(_MaskTex, TRANSFORM_TEX(i.uv + _MaskPanning, _MaskTex)) * UNITY_ACCESS_INSTANCED_PROP(Props, _MaskStrength);
                #endif
                #if defined(MASK_RED_IS_ALPHA)
                mask = mask.r;
                #endif
                #if defined(_MASKBLEND_ADD)
                albedo += mask;
                #elif defined(_MASKBLEND_MASKED_ADD)
                albedo = albedo * mask + mask;
                #else
                albedo *= mask;
                #endif
                #endif

                #if defined(MASK2)
                #if defined(SECONDARY_UVS_MASK2)
                float4 mask2 = tex2D(_Mask2Tex, TRANSFORM_TEX(uv2 + _Mask2Panning, _Mask2Tex)) * UNITY_ACCESS_INSTANCED_PROP(Props, _Mask2Strength);
                #else
                float4 mask2 = tex2D(_Mask2Tex, TRANSFORM_TEX(i.uv + _Mask2Panning, _Mask2Tex)) * UNITY_ACCESS_INSTANCED_PROP(Props, _Mask2Strength);
                #endif
                #if defined(MASK2_RED_IS_ALPHA)
                mask2 = mask2.r;
                #endif
                #if defined(_MASK2BLEND_ADD)
                albedo += mask2;
                #elif defined(_MASK2BLEND_MASKED_ADD)
                albedo = albedo * mask2 + mask2;
                #else
                albedo *= mask2;
                #endif
                #endif

                albedo.a *= _AlphaMultiplier;

                #if defined(SQUARE_ALPHA)
                albedo.a *= albedo.a;
                #endif

                #if defined(_BLOOMTYPE_PP)
                CUSTOM_BLOOM_PP_APPLY(albedo, _BloomMultiplier);
                #elif defined(_BLOOMTYPE_FRAG)
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _BloomWhiteMultiplier);
                #else
                CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(albedo);
                #endif
                
                ACES_TONE_MAPPING_APPLY(albedo);
                
                #if ENABLE_FOG
                #if defined(ENABLE_HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_FOG_APPLY(color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
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