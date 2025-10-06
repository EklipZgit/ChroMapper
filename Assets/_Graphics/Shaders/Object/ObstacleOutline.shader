Shader "ChroMapper/Object/Obstacle Outline"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _WorldScale("World Scale", Vector) = (1, 3.5, 1, 1)

        [Header(Beat Saber)]
        [Space(10)]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutEdgeWidth("Cutout Edge Width", Range(0, 0.2)) = 0.05
        _CutoutEdgeGlow("Cutout Edge Glow", Range(0, 1)) = 0.25
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
        uniform float _OutsideAlpha = 1;
        uniform float _ObstacleFadeRadius = 8;

        // Define instanced properties
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
        UNITY_INSTANCING_BUFFER_END(Props)

        float _CutoutEdgeGlow;
        float _CutoutEdgeWidth;
        ENDHLSL

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 localPos : TEXCOORD1;
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

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));
                float uvXScalar = 0;
                float uvYScalar = 0;

                if (i.normal.x != 0)
                {
                    uvXScalar = worldScale.z;
                    uvYScalar = worldScale.y;
                }
                else if (i.normal.y != 0)
                {
                    uvYScalar = worldScale.z;
                    uvXScalar = worldScale.x;
                }
                else
                {
                    uvXScalar = worldScale.x;
                    uvYScalar = worldScale.y;
                }

                float2 halfUv = 0.5 - abs(0.5 - i.uv);
                if (halfUv.x * uvXScalar >= 0.05 && halfUv.y * uvYScalar >= 0.05)
                {
                    discard;
                }

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                // TexOffset is apparently different
                float noise = simplex((i.localPos + cutoutTexOffset.xyz * 2) * worldScale);
                float c = noise - cutout;
                clip(c);

                float alpha = saturate(color.a * 0.05);
                return float4(log2(color.rgb + 1.0), alpha);
            }
            ENDHLSL
        }
    }
}