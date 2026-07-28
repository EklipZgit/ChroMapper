// Replacement for the Beat Saber game shader Custom/SimpleLightning.
Shader "ChroMapper/Lightning"
{
    Properties
    {
        [Space(10)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        _TargetPoint ("Target Point", Vector) = (0,0,0,0)
        _Width ("Width", Range(0, 5)) = 1
        _Jitter ("Jitter", Range(0, 10)) = 5
        _Speed ("Speed", Range(0, 1)) = 1

        [Header(Fog Settings)] [Space]
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src A", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst A", float) = 0
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", float) = 0

        [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 0
    }
    SubShader
    {
        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment HEIGHT_FOG

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomBloom.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Width;
            float _Speed;
            float _Jitter;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TargetPoint)
            UNITY_INSTANCING_BUFFER_END(Props)

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
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float hash(float n) { return frac(sin(n) * 43758.5453123); }

            float lightningNoise(float v, float time)
            {
                float n = sin(v * 10.0 - time * 15.0) * 1.0;
                n += sin(v * 25.0 + time * 22.0) * 0.5;
                n += sin(v * 50.0 - time * 35.0) * 0.25;
                return n;
            }

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float4 targetPoint = UNITY_ACCESS_INSTANCED_PROP(Props, _TargetPoint);

                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 worldTarget = targetPoint.xyz;

                float3 beamDir = worldOrigin - worldTarget;
                float3 up = float3(1, 0, 0);
                float3 side = normalize(cross(beamDir, up)) * _Width;

                float jump = (frac(sin(floor(_Time.y * 8 * _Speed)) * 43758.5453) - 0.5) * 2;

                // float noise = sin(i.uv.x * 20.0 + _Time.y * _Speed) * _Jitter;
                // float noise = hash(floor(i.uv.x * 10.0 + _Time.y * _Speed)) * _Jitter;
                float mask = i.uv.x * (1 - i.uv.x);
                float noise = (lightningNoise(i.uv.x + _Width, _Time.y * _Speed) + jump) * _Jitter * mask;

                float offset = (i.uv.y - 0.5) * 2;
                float3 lerpedPos = lerp(worldOrigin, worldTarget, i.uv.x);
                float3 finalWorldPos = lerpedPos + side * (offset + noise);

                o.vertex = mul(UNITY_MATRIX_VP, float4(finalWorldPos, 1));
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.uv.xy = TRANSFORM_TEX(i.uv.xy, _MainTex);
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                half mask = saturate(sin(i.uv.x * 3.14159) * 4);
                i.uv.x = (i.uv.x + _Time.x) % 1;
                half4 albedo = color * mask * tex2D(_MainTex, i.uv);

                CUSTOM_BLOOM_FRAG_APPLY(albedo, 1);

                ACES_TONE_MAPPING_APPLY(albedo);

                #if defined(BLOOM_FOG)
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