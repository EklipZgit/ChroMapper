Shader "ChroMapper/Object/Obstacle Core LW"
{
    Properties
    {
        _TintColor ("Tint Color", Vector) = (1, 1, 1, 1)
        _AddColor ("Add Color", Vector) = (0, 0, 0, 0)
        _TintColorAlpha ("Tint Color Alpha", Float) = 1
        _Cutout ("Cutout", Range(0, 1)) = 0
        [Toggle(FOG)] _EnableFog ("Enable Fog", Float) = 1
        _FogStartOffset ("Fog Start Offset", Float) = 100
        _FogScale ("Fog Scale", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
        [Toggle] _ZWrite ("Z Write", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+1" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha, Zero Zero
        Cull [_CullMode]
        ZWrite [_ZWrite]
        ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment FOG
            #pragma multi_compile_fragment _ BLOOM_FOG
            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Camera.hlsl"
            #include "../ShaderLibrary/Families/BloomFogComposition.hlsl"

            float _TintColorAlpha;
            float _FogStartOffset;
            float _FogScale;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TintColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AddColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // ObstacleCoreLW dissolves by shrinking its cross-section rather than sampling displacement noise.
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                v.vertex.xy *= 1.0 - cutout * cutout;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _TintColor).rgb;
                float3 addColor = UNITY_ACCESS_INSTANCED_PROP(Props, _AddColor).rgb;
                float visibility = 1.0;
                #if defined(FOG)
                visibility -= CalculateCustomFogFactor(distanceSquared(i.worldPos), _FogStartOffset, _FogScale);
                #endif
                return float4(SampleBloomPrePass(i.screenPos).rgb * tint + addColor, visibility * _TintColorAlpha);
            }
            ENDHLSL
        }
    }
}
