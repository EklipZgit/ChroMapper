Shader "ChroMapper/Object/Obstacle"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _FadeSize("Fade Size", Float) = 1
        _OpaqueAlpha("OpaqueAlpha", Float) = 1
        _Rotation("Rotation", Float) = 0
        _WorldScale("World Scale", Vector) = (1, 3.5, 1, 1)
        _AnimationSpawned("Animation is Spawned", Float) = 0

        [Header(Beat Saber)]
        [Space(10)]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutEdgeWidth("Cutout Edge Width", Range(0, 0.2)) = 0.05
        _CutoutEdgeGlow("Cutout Edge Glow", Range(0, 1)) = 0.25
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent-200" "RenderType"="Transparent"
        }
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
            UNITY_DEFINE_INSTANCED_PROP(float, _OpaqueAlpha)
            UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
            UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
            UNITY_DEFINE_INSTANCED_PROP(float, _AnimationSpawned)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
        UNITY_INSTANCING_BUFFER_END(Props)

        float _Glow;
        float _CutoutEdgeGlow;
        float _CutoutEdgeWidth;
        ENDHLSL

        Pass
        {
            Blend SrcColor OneMinusSrcColor
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma target 3.0
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
                float4 worldPos : TEXCOORD1;
                float4 rotatedPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
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
                float mainAlpha = _MainAlpha;

                float mag = length(color.rgb);
                if (mag > 1)
                {
                    color.rgb = normalize(color.rgb) * min(sqrt(mag), 16) * mainAlpha;
                    color.rgb = saturate(color.rgb);
                }

                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));
                float noise = simplex((i.localPos + cutoutTexOffset.xyz) * worldScale * 0.6);
                float c = noise - cutout;
                clip(c);

                float animationSpawned = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationSpawned);
                if (animationSpawned > 0)
                {
                    return float4(color.rgb, 0);
                }
                if (animationSpawned < 0)
                {
                    return float4(color.rgb, 0);
                }

                return float4(color.rgb, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma target 3.0
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
                float4 worldPos : TEXCOORD1;
                float4 rotatedPos : TEXCOORD2;
                float4 localPos : TEXCOORD3;
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

                /// Outline ///
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
                float noise = simplex((i.localPos + cutoutTexOffset.xyz) * worldScale * 0.5);
                float c = noise - cutout;
                clip(c);

                // alpha should be as color alpha itself but current bloom makes it very overblown
                float alpha = saturate(color.a * 0.05);
                return float4(log2(color.rgb + 1.0), alpha);
            }
            ENDHLSL
        }
    }
}