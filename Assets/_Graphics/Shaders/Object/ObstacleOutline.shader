Shader "ChroMapper/Object/Obstacle Outline"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _WorldScale("World Scale", Vector) = (1, 1, 1, 1)

        [Header(Beat Saber)]
        [Space(10)]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        LOD 100

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "../CGIncludes/Noise.cginc"

        // These are global properties and should not be instanced
        uniform float _MainAlpha = 0.5;
        uniform float _ObstacleGlow = 0;

        // Define instanced properties
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
        UNITY_INSTANCING_BUFFER_END(Props)
        ENDHLSL

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #include "../CGIncludes/BloomFog.cginc"
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 localPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 customScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex;
                o.uv = v.uv;
                o.normal = v.normal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.pos);

                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));
                float2 uvScalar = 0;
                if (i.normal.x != 0)
                {
                    uvScalar.xy = worldScale.zy;
                }
                else if (i.normal.y != 0)
                {
                    uvScalar.xy = worldScale.xz;
                }
                else
                {
                    uvScalar.xy = worldScale.xy;
                }

                float2 halfUv = 0.5 - abs(0.5 - i.uv);
                if (halfUv.x * uvScalar.x >= 0.05 && halfUv.y * uvScalar.y >= 0.05)
                {
                    discard;
                }

                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                // TexOffset is apparently different
                float noise = simplex((i.localPos + cutoutTexOffset.xyz * 2) * worldScale);
                float c = noise - cutout;
                clip(c);

                float _FogScale = 5;
                float _FogAttenuation = 0.00002;
                float distance = length(i.worldPos - _WorldSpaceCameraPos);
                float factor = max(dot(distance, distance), 0);
                factor = max(factor * _FogScale, 0);
                factor = 1 / (factor * _FogAttenuation + 1);
                // factor = -factor + 1;

                // return float4(factor.xxx, 0);

                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed alpha = 0.05;
                if (_ObstacleGlow > 0)
                {
                    alpha = saturate(color.a * 0.5);
                }
                color = float4(log2(color.rgb + 1.0), alpha) * factor;
                BLOOM_FOG_APPLY(color, i.customScreenPos, i.worldPos, 0, 5);
                return color;
            }
            ENDHLSL
        }
    }
}