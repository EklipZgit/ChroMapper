Shader "ChroMapper/Mirror"
{
    Properties
    {
        _NormalTex ("Normal Texture", 2D) = "white" {}
        _BumpIntensity ("Bump Intensity", float) = 0.1
        _ReflectionIntensity ("Reflection Intensity", float) = 0.5
        _TextureScrolling ("Texture Scrolling", Vector) = (0,0,0,0)

        [Space(20)]
        [Toggle(DETAIL_NORMAL_MAP)] _DetailNormalMap ("Detail Normal Map", float) = 0
        _DetailNormalTextureScale ("Scale", float) = 1
        _DetailNormalIntensity ("Intensity", float) = 0
        _DetailNormalTexScrolling ("Scrolling", Vector) = (0.05,2,0,0)

        [Space(20)]
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Space(20)]
        [Toggle(DIRT)] _EnableDirt ("Dirt", float) = 0
        _DirtTex ("Texture", 2D) = "white" {}
        _DirtIntensity ("Intensity", float) = 1

        [Space(20)]
        [Toggle(LIGHTMAP)] _EnableLightmap ("Enable Lightmap", float) = 0
        [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse", float) = 1
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Light Falloff", float) = 0

        [Header(Fog Settings)] [Space]
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 1

        [PerRendererData] _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _StencilRefValue ("Stencil Ref Value", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", float) = 1
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

            #pragma shader_feature_local LIGHTMAP
            #pragma shader_feature_local DIFFUSE
            #pragma shader_feature_local LIGHT_FALLOFF

            #pragma shader_feature_local DETAIL_NORMAL_MAP
            #pragma shader_feature_local DIRT

            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma shader_feature_local HEIGHT_FOG

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomTime.cginc"
            #include "CGIncludes/CustomLighting.cginc"
            #include "CGIncludes/CustomTonemapping.cginc"

            // this has no use in BIRP, but whatever it's still nice
            CBUFFER_START(UnityPerMaterial)
                float4 _NormalTex_ST;

                float _BumpIntensity;
                float _ReflectionIntensity;
                float2 _TextureScrolling;

                #if defined(DETAIL_NORMAL_MAP)
                float _DetailNormalTextureScale;
                float _DetailNormalIntensity;
                float2 _DetailNormalTexScrolling;
                #endif

                float _Metallic;
                float _Smoothness;

                #if defined(DIRT)
                float4 _DirtTex_ST;
                float _DirtIntensity;
                #endif

                float4 _Color; // is tint supposed to be -1/default blue?

                #if defined(ENABLE_BLOOM_FOG)
                float _FogStartOffset;
                float _FogScale;
                #if defined(HEIGHT_FOG)
                float _FogHeightOffset;
                float _FogHeightScale;
                #endif
                #endif
            CBUFFER_END

            sampler2D _NormalTex;
            #if defined(DIRT)
            sampler2D _DirtTex;
            #endif
            sampler2D _ReflectionTex;

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
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                o.uv.xy = i.uv.xy;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 albedo = 1;

                #if defined(DIRT)
                albedo += tex2D(_DirtTex, TRANSFORM_TEX(i.uv, _DirtTex) + _TextureScrolling * _Time.yy) *
                    _DirtIntensity;
                #endif

                #if defined(DIFFUSE)
                float3 calculated = 0;
                CUSTOM_LIGHTING_APPLY(calculated, albedo, _Metallic, _Smoothness, 1, 1, i.worldPos,
                                      i.worldNormal);
                albedo.rgb += calculated.rgb;
                #endif

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float4 reflectionCol = tex2D(_ReflectionTex, screenUV) * _ReflectionIntensity;
                albedo *= reflectionCol;

                ACES_TONE_MAPPING_APPLY(albedo);

                #if defined(ENABLE_BLOOM_FOG)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset,
                                       _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                return albedo;
            }
            ENDCG
        }
    }
}