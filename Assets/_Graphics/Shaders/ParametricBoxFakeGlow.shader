Shader "ChroMapper/Parametric Box Fake Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(None, PP, Frag)] _BloomType ("Bloom Type", float) = 0
        _BloomWhiteMultiplier ("White Multiplier", float) = 1
        [KeywordEnum(Before Emissive, After Emissive)] _AcesTonemap ("ACES Tonemapping", float) = 1

        _SizeParams("Size Params", Vector) = (3,2,0,0.3)

        [Header(Fog Settings)] [Space]
        [Toggle(ENABLE_FOG)] _EnableFog ("Enable Fog", float) = 1
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
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ ENABLE_HEIGHT_FOG
            #pragma multi_compile _FOGTYPE_ALPHA
            #pragma multi_compile _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG
            #pragma multi_compile _ACESTONEMAP_BEFORE_EMISSIVE _ACESTONEMAP_AFTER_EMISSIVE
            #pragma multi_compile ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SizeParams)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                float4 customScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _BloomWhiteMultiplier;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                float center;
                if (i.vertex.x < 0)
                {
                    center = -1;
                    i.vertex.x = (i.vertex.x - center) / sizeParams.x * sizeParams.w + center;
                }
                else if (i.vertex.x > 0)
                {
                    center = 1;
                    i.vertex.x = (i.vertex.x - center) / sizeParams.x * sizeParams.w + center;
                }

                if (i.vertex.y < 0)
                {
                    center = -1;
                    i.vertex.y = (i.vertex.y - center) / sizeParams.y * sizeParams.w + center;
                }
                else if (i.vertex.y > 0)
                {
                    center = 1;
                    i.vertex.y = (i.vertex.y - center) / sizeParams.y * sizeParams.w + center;
                }

                o.vertex = UnityObjectToClipPos(i.vertex);

                o.uv = i.uv;
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                fixed4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));

                #if _BLOOMTYPE_PP
                CUSTOM_BLOOM_PP_APPLY(albedo, 1);
                #elif _BLOOMTYPE_FRAG
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _BloomWhiteMultiplier);
                #else
                CUSTOM_BLOOM_NONE_APPLY(albedo);
                #endif

                #if defined(ENABLE_HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif

                return albedo;
            }
            ENDCG
        }
    }
}