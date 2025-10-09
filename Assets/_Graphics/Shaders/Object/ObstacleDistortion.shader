Shader "ChroMapper/Object/Obstacle Distortion"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _WorldScale("World Scale", Vector) = (1, 3.5, 1, 1)

        [Header(Beat Saber)]
        [Space(10)]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        // it should be cull off so it still distorts when inside
        // but this causes visual problem
        // whatever beat saber is doing is black magic
        Cull Back
        ZWrite Off
        LOD 100

        // do not pass texture, this breaks (stacked) visual
        GrabPass {}

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "../CGIncludes/Noise.cginc"

        // These are global properties and should not be instanced
        uniform float _MainAlpha = 0.5;
        uniform float _ObstacleDistortionStrength = 0.1;

        // Define instanced properties
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
        UNITY_INSTANCING_BUFFER_END(Props)

        sampler2D _GrabTexture;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
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
                float3 localPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // necessary only if you want to access instanced properties in the fragment Shader.

                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex;
                o.uv = v.uv;
                o.normal = v.normal;
                o.screenPos = ComputeGrabScreenPos(o.pos);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));

                float noise = simplex((i.localPos + cutoutTexOffset.xyz) * worldScale * 0.6);
                float c = noise - cutout;
                clip(c);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float mag = length(color.rgb);
                if (mag > 1)
                {
                    color.rgb = normalize(color.rgb) * min(sqrt(mag), 16) * color.a;
                    color.rgb = saturate(color.rgb);
                }
                color *= 0.1;
                color.a = 0;

                float3 uvScalar = 0;
                if (i.normal.x != 0)
                {
                    uvScalar.xyz = worldScale.zyx;
                }
                else if (i.normal.y != 0)
                {
                    uvScalar.xyz = worldScale.xzy;
                }
                else
                {
                    uvScalar.xyz = worldScale.xyz;
                }

                // float2 halfUv = 0.5 - abs(0.5 - i.uv);
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                // obstacle distortion need to be stable, cannot be based on screen space position
                // horribad
                screenUV.x += (simplex((i.uv * uvScalar + cutoutTexOffset * 2) / 4) - 0.5) *
                    _ObstacleDistortionStrength;
                screenUV.y += (simplex((i.uv.yx * uvScalar.yx + cutoutTexOffset * 2) / 4) - 0.5) *
                    _ObstacleDistortionStrength;

                fixed4 col = color + tex2D(_GrabTexture, screenUV);
                return col;
            }
            ENDHLSL
        }
    }
}