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

        [Header(Fog Settings)] [Space]
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
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
        [Toggle] _ZWrite ("Z Write", float) = 1
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
            "Queue"="Transparent+50"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

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
        #include "../CGIncludes/CustomTonemapping.cginc"

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
        sampler3D _CutoutTex;

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
            #pragma shader_feature_local HEIGHT_FOG
            #pragma multi_compile _ CM_PREVIEW_MODE

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
                float3 cutoutPos : TEXCOORD5;
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
                o.cutoutPos = mul(unity_ObjectToWorld, v.vertex.xyz);
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

                // Cutout
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                float noise = tex3D(_CutoutTex, (i.cutoutPos + cutoutTexOffset.xyz) * 0.3);
                float cl = noise - cutout;
                clip(cl);

                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

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

                color.a = 0;
                color = color * 0.25 + tex2D(_GrabTexture, screenUV);

                ACES_TONE_MAPPING_APPLY(color);
                
                #if defined(CM_PREVIEW_MODE)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_FOG_APPLY(color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}