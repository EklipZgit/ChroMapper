Shader "ChroMapper/Particles"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        [Toggle(MAIN_TEXTURE)] _UseMainTex ("Base Texture", float) = 1
        _BaseLayer ("Base Color", float) = 1
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity("Color Intensity", float) = 1

        [KeywordEnum(None, Full, Y Axis, Camera Facing)] _Billboard ("Billboard", float) = 0
        _BillboardScale ("Billboard Scale", float) = 1

        [KeywordEnum(None, PP, Frag)] _BloomType ("Bloom Type", float) = 0
        _BloomWhiteMultiplier ("White Multiplier", float) = 1
        _BloomMultiplier ("Bloom Multiplier", float) = 1
        [Toggle(REMAP_WHITEBOOST_START)] _EnableRemapWhiteBoostStart ("Remap White Boost Start", float) = 0
        _WhiteBoostRemapStart ("Alpha for no White Boost", Range(0, 1)) = 0
        [KeywordEnum(Before Emissive, After Emissive)] _AcesTonemap ("ACES Tonemapping", float) = 1

        [Toggle(VERTEX_COLOR)] _EnableVertexColor ("Vertex Color", float) = 0
        [Toggle(VERTEX_SQUARE_ALPHA)] _SquareVertexAlpha ("Square Vertex Alpha", float) = 0
        [Toggle(VERTEX_RED_IS_ALPHA)] _RedIsVertexAlpha ("Red is Vertex Alpha", float) = 0
        [KeywordEnum(RGBA, A, RGB)] _VertexChannels ("Vertex Channels", float) = 0

        _AlphaMultiplier ("Alpha Multiplier", float) = 1
        [Toggle(SQUARE_ALPHA)] _SquareAlpha("Square Alpha", float) = 1

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
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ _FOGTYPE_LERP _FOGTYPE_COLOR _FOGTYPE_ALPHA
            #define ENABLE_FOG defined(_FOGTYPE_LERP) || defined(_FOGTYPE_COLOR) || defined(_FOGTYPE_ALPHA)
            #pragma multi_compile _ ENABLE_HEIGHT_FOG

            #pragma shader_feature MAIN_TEXTURE
            #pragma shader_feature REMAP_WHITEBOOST_START

            #pragma shader_feature VERTEX_COLOR
            #pragma shader_feature VERTEX_SQUARE_ALPHA
            #pragma shader_feature VERTEX_RED_IS_ALPHA
            #pragma multi_compile _ _BILLBOARD_FULL _BILLBOARD_Y_AXIS _BILLBOARD_CAMERA_FACING

            #pragma multi_compile _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG

            #pragma multi_compile _ACESTONEMAP_BEFORE_EMISSIVE _ACESTONEMAP_AFTER_EMISSIVE
            #pragma multi_compile ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/BloomFog.cginc"

            #if defined(UNITY_INSTANCING_ENABLED)

            UNITY_INSTANCING_BUFFER_START (PerDrawSprite)
            // SpriteRenderer.Color while Non-Batched/Instanced.
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
            // this could be smaller but that's how bit each entry is regardless of type
            UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
            UNITY_INSTANCING_BUFFER_END (PerDrawSprite)

            #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
            #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

            #endif // instancing

            CBUFFER_START(UnityPerDrawSprite)
                #if !defined(UNITY_INSTANCING_ENABLED)
                fixed4 _Color;
                fixed4 _RendererColor;
                fixed2 _Flip;
                #endif
                float _EnableExternalAlpha;
            CBUFFER_END

            #if defined(MAIN_TEXTURE)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            #endif

            float _AlphaMultiplier;
            float _BloomMultiplier;
            float _BloomWhiteMultiplier;
            float _Intensity;

            #define USE_BILLBOARD defined(_BILLBOARD_FULL) || defined(_BILLBOARD_Y_AXIS) || defined(_BILLBOARD_CAMERA_FACING)
            #if USE_BILLBOARD
            float _BillboardScale;
            #endif

            #if defined(REMAP_WHITEBOOST_START)
            float _WhiteBoostRemapStart;
            #endif

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if defined(VERTEX_COLOR)
                fixed4 color : COLOR;
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

                #if _BILLBOARD_CAMERA_FACING || _BILLBOARD_FULL
                float3 viewPos = mul(UNITY_MATRIX_V, float4(worldOrigin, 1)).xyz;
                float3 billboardPos = viewPos + i.vertex.xyz * _BillboardScale;
                o.worldPos = billboardPos;
                o.vertex = mul(UNITY_MATRIX_P, float4(billboardPos, 1));
                #endif

                #if _BILLBOARD_Y_AXIS
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
                o.color = i.color * _RendererColor * UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, _Color);
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                #if defined(VERTEX_COLOR)
                fixed4 color = i.color;
                #else
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, _Color);
                #endif
                color.rgb *= _Intensity;

                #if defined(MAIN_TEXTURE)
                fixed4 albedo = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex)) * color;
                #else
                fixed4 albedo = color;
                #endif

                #if SQUARE_ALPHA
                albedo.a *= albedo.a;
                #endif

                albedo.a *= _AlphaMultiplier;

                #if _BLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(albedo, _BloomMultiplier);
                #elif _BLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _BloomWhiteMultiplier);
                #else
                CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(albedo);
                #endif

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