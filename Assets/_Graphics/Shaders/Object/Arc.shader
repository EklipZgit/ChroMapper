Shader "ChroMapper/Object/Arc"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _FadeSize("Fade Size", Range(0, 10)) = 5
        [HideInInspector] _Rotation("Rotation", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent+50" "RenderType"="Transparent"
        }
        LOD 100
        Blend SrcColor OneMinusSrcColor, One OneMinusSrcColor
        Cull Off
        ZTest LEqual
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ CM_PREVIEW_MODE

            #include "UnityCG.cginc"

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
                UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            uniform float _EditorDistance;

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
                float4 worldPos : TEXCOORD1;
                float4 rotatedPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // necessary only if you want to access instanced properties in the fragment Shader.

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = v.normal;

                // Calculate the world position coordinates to pass to the fragment shader
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                //Global platform offset
                float4 offset = float4(0, -0.5, -1.5, 0);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = (o.worldPos.x - offset.x) * cos(rotationInRadians) - (o.worldPos.z - offset.z) * sin(
                    rotationInRadians);
                float newZ = (o.worldPos.z - offset.z) * cos(rotationInRadians) + (o.worldPos.x - offset.x) * sin(
                    rotationInRadians);

                o.rotatedPos = float4(newX + offset.x, o.worldPos.y, newZ + offset.z, o.worldPos.w);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                /// Coloring ///
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                float mag = length(color);

                if (mag > 1)
                {
                    color = normalize(color) * sqrt(mag);
                }

                #ifdef CM_PREVIEW_MODE
                float fadeSize = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize);

                float distance = i.rotatedPos.z;
                float startDistance = fadeSize;
                float endDistance = _EditorDistance - fadeSize;

                float fade = 1.0;
                if (distance <= startDistance) fade = clamp(distance / startDistance, 0.0, 1.0);
                else if (distance >= endDistance) fade = 1.0 - clamp((distance - endDistance) / fadeSize, 0.0, 1.0);

                return fixed4(color.rgb * fade * 2, fade * 0.1);
                #else
                return fixed4(color.rgb * 2, 0.1);
                #endif
            }
            ENDCG
        }
    }
}