Shader "ChroMapper/Parametric Slice Billboard"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}

        _SizeParams("Size Params", Vector) = (0.25,10,0,0.5)
        _AlphaWidth("Alpha Width", Vector) = (1,1,1,1)

        [Header(Fog Settings)]
        [Space]
        _FogStartOffset ("Fog Start Offset", Float) = 1
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        ZWrite Off
        Cull Front
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
                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 localUp = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
                float3 dirToCam = _WorldSpaceCameraPos - worldOrigin;
                float3 look = normalize(dirToCam - localUp * dot(dirToCam, localUp));
                float3 right = normalize(cross(localUp, look));

                float width;
                float height;
                float offset = sizeParams.y * sizeParams.z;
                // TODO: replace t and lerp with vertex access
                if (i.uv.y < 0.25)
                {
                    float t = i.uv.y / 0.25;
                    width = alphaWidth.z;
                    height = lerp(-sizeParams.w, 0, t) - offset;
                }
                else if (i.uv.y < 0.75)
                {
                    float t = (i.uv.y - 0.25) * 2;
                    width = lerp(alphaWidth.z, alphaWidth.w, t);
                    height = lerp(0, sizeParams.y, t) - offset;
                }
                else
                {
                    float t = (i.uv.y - 0.75) / 0.25;
                    width = alphaWidth.w;
                    height = lerp(sizeParams.y, sizeParams.y + sizeParams.w, t) - offset;
                }

                width *= sizeParams.x;

                o.lengthFactor = i.vertex.y;
                float horizontal = i.vertex.x * width;
                float3 worldPos = worldOrigin + right * horizontal + localUp * height;

                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

                o.uv = float3(i.uv * width / sizeParams.x, width / sizeParams.x);
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
                albedo.a = sqrt(max(albedo.a - 0.5, 0));
                albedo.a *= pow(alphaFactor, 4);

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