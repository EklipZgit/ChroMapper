Shader "ChroMapper/Parametric Box Fake Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}

        _SizeParams("Size Params", Vector) = (3,2,0,0.3)

        [Header(Fog Settings)]
        [Space]
        _FogStartOffset ("Fog Start Offset", Float) = 0
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] ENABLE_HEIGHT_FOG ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        ZWrite Off
        Blend SrcColor OneMinusSrcColor

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma shader_feature ENABLE_HEIGHT_FOG

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"

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

                // TODO: figure out color blending and glow intensity
                if (albedo.a > 1.0) albedo.rgb *= albedo.a;
                albedo.a = saturate(albedo.a);
                albedo.rgb *= albedo.a * 4;

                #ifdef ENABLE_HEIGHT_FOG
                BLOOM_FOG_HEIGHT_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif

                return saturate(log(1 + albedo));
            }
            ENDCG
        }
    }
}