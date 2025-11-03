Shader "ChroMapper/Object/Obstacle Distortion"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _WorldScale("World Scale", Vector) = (1, 3.5, 1, 1)
        _DistortionStrength("Distortion Strength", Range(0,0.5)) = 0.05
        _DistortionScale("Distortion Scale", Range(0.1, 4)) = 1.0

        [Header(Beat Saber)]
        [Space(10)]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)

        [Header(Fog Settings)]
        [Space]
        _FogStartOffset ("Fog Start Offset", Float) = 0
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle]
        ENABLE_HEIGHT_FOG ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1
    }
    SubShader
    {
        // Beat Saber uses transparent+3 queue and it helps to get transparent stuff behind gets rendered
        // but this comes at a cost of no instancing
        // however, Geometry+100 causes some transparency to not get rendered correctly
        // but it's more performant as it has instancing
        // perhaps, we can do separate material for performance and quality?
        Tags
        {
            "Queue"="Transparent+50"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        LOD 100

        GrabPass
        {
            "_GrabTexture"
            Tags
            {
                "Queue"="Transparent"
            }
        }

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "../CGIncludes/Noise.cginc"
        #include "../CGIncludes/BloomFog.cginc"

        // These are global properties and should not be instanced
        uniform float _MainAlpha = 0.5;

        // Define instanced properties
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
        UNITY_INSTANCING_BUFFER_END(Props)

        float _DistortionStrength;
        float _DistortionScale;
        sampler2D _GrabTexture;

        float _FogStartOffset;
        float _FogScale;
        float _FogHeightOffset;
        float _FogHeightScale;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ CM_UIMODE_PREVIEW
            #pragma multi_compile _ CM_UIMODE_PLAYING
            #pragma shader_feature ENABLE_HEIGHT_FOG

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
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float4 customScreenPos : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                o.normal = v.normal;
                o.screenPos = ComputeGrabScreenPos(o.pos);
                o.customScreenPos = ComputeScreenPosCustom(o.pos);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));
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

                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                float noise = simplex((i.localPos + cutoutTexOffset.xyz) * worldScale * 0.6);
                float c = noise - cutout;
                clip(c);

                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed mag = length(color.rgb);
                if (mag > 1)
                {
                    color.rgb = normalize(color.rgb) * min(sqrt(mag), 16) * color.a;
                    color.rgb = saturate(color.rgb);
                }
                color *= 0.2;
                color.a = 0;

                float _FogScale = 5;
                float _FogAttenuation = 0.00002;
                float distance = length(i.worldPos - _WorldSpaceCameraPos);
                float factor = max(dot(distance, distance), 0);
                factor = max(factor * _FogScale, 0);
                factor = 1 / (factor * _FogAttenuation + 1);
                // return float4(factor.xxx, 0);

                // float2 halfUv = 0.5 - abs(0.5 - i.uv);
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                // obstacle distortion need to be stable, cannot be based on screen space position
                // horribad
                screenUV.x +=
                    (simplex((i.uv * uvScalar + cutoutTexOffset * _DistortionScale) / _DistortionScale) - 0.5) *
                    _DistortionStrength;
                screenUV.y +=
                    (simplex((i.uv.yx * uvScalar.yx + cutoutTexOffset * _DistortionScale) / _DistortionScale) - 0.5) *
                    _DistortionStrength;

                fixed4 col = color + tex2D(_GrabTexture, screenUV);
                col = col * factor;
                
                #if defined(CM_UIMODE_PLAYING) || defined(CM_UIMODE_PREVIEW)
                    #ifdef ENABLE_HEIGHT_FOG
                        BLOOM_FOG_HEIGHT_FOG_APPLY(col, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset, _FogHeightScale);
                    #else
                        BLOOM_FOG_APPLY(col, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                    #endif
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}