Shader "ChroMapper/Object/Arc"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _MainTex("Texture", 2D) = "white" {}
        _FadeSize("Fade Size", Range(0, 10)) = 5
        [HideInInspector] _Rotation("Rotation", float) = 0

        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src A", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst A", float) = 1
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
            "Queue"="Transparent+50"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ CM_PREVIEW_MODE

            #include "UnityCG.cginc"
            #include "../CGIncludes/CustomBloom.cginc"

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
                UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _ObjectTime)
            UNITY_INSTANCING_BUFFER_END(Props)

            uniform float _SongTime;
            uniform float _EditorDistance;
            uniform float _TrackLaneYPosition; // we are keeping this name because Vivify uses this too

            sampler2D _MainTex;

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
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD1;
                float4 rotatedPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 ComputeRotatedPosition(float3 position, float theta)
            {
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);

                return float3(position.x * cosTheta - position.z * sinTheta,
                              position.y,
                              position.z * cosTheta + position.x * sinTheta);
            }

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.worldPos = mul(unity_ObjectToWorld, i.vertex);
                o.worldPos.y = max(_TrackLaneYPosition + 0.01, o.worldPos.y); // save me

                o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1));
                o.uv = i.uv;
                o.normal = i.normal;

                //Global platform offset
                const float4 offset = float4(0, -0.5, -1.5, 0);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180);

                float objectTime = UNITY_ACCESS_INSTANCED_PROP(Props, _ObjectTime);

                o.rotatedPos = float4(
                    ComputeRotatedPosition(o.worldPos - offset, rotationInRadians) + offset,
                    objectTime + 0.001 - _SongTime
                );

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                /// Coloring ///
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                fixed mask = saturate(sin(i.uv.x * 3.14159) * 5);
                #if CM_PREVIEW_MODE
                i.uv.x = (i.uv.x + _Time.y) % 1;
                #endif
                float4 albedo = color * tex2D(_MainTex, i.uv);
                albedo *= mask;

                #ifdef CM_PREVIEW_MODE
                float fadeSize = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize);

                float distance = i.rotatedPos.z;
                float startDistance = fadeSize;
                float endDistance = _EditorDistance - fadeSize;

                float fade = 1;
                if (distance <= startDistance) fade = saturate(distance / startDistance);
                else if (distance >= endDistance) fade = 1 - saturate((distance - endDistance) / fadeSize);

                albedo *= fade;
                #endif

                CUSTOM_BLOOM_PP_APPLY(albedo, 1);
                albedo.a *= albedo.a * albedo.a;
                return albedo;
            }
            ENDHLSL
        }
    }
}