Shader "ChroMapper/Parametric Box Transparent"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}

        _AlphaWidth("Alpha Width", Vector) = (1,1,1,1)

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
        Blend SrcAlpha OneMinusSrcAlpha

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
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlphaWidth)
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
                float3 uv : TEXCOORD0;
                float lengthFactor : TEXCOORD1;
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

                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);

                o.lengthFactor = (1 + i.vertex.y) / 2;
                float width = lerp(alphaWidth.z, alphaWidth.w, o.lengthFactor);

                i.vertex.x = i.vertex.x * width;
                i.vertex.z = i.vertex.z * width;

                o.vertex = UnityObjectToClipPos(i.vertex);

                o.uv = float3(i.uv * width / alphaWidth.z, width / alphaWidth.w);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);

                float adjustedLengthFactor = i.lengthFactor;

                float2 adjustedUv = i.uv.xy / i.uv.z;
                fixed4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(adjustedUv, _MainTex));

                // TODO: figure out color blending and glow intensity
                if (albedo.a > 1.0) albedo.rgb *= albedo.a;
                albedo.a = saturate(albedo.a);
                albedo.rgb *= albedo.a;

                float alphaFactor = lerp(alphaWidth.x, alphaWidth.y, adjustedLengthFactor);
                albedo *= alphaFactor;
                // albedo.a = sqrt(max(albedo.a - 0.5, 0));
                // albedo.a *= pow(alphaFactor, 4);

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