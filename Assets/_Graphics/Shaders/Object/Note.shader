// Example Shader for Universal RP
// Written by @Cyanilux
// https://cyangamedev.wordpress.com/urp-shader-code/
Shader "ChroMapper/Object/Note"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 0)
        _MainTex("Texture", 2D) = "white" {}
        _Glow("Glow", Range(0, 1)) = 0.0

        [Space(10)]
        _OutlineWidth("Outline Width", Float) = 0.05
        _OverNoteInterfaceColor("Over Note Interface Color", Color) = (1, 1, 1, 0)
        _Rotation("Rotation", Float) = 0
        _ObjectTime("Object Time", Float) = 0
        [Toggle] _Lit("Lit", Float) = 0
        _AnimationSpawned("Animation Spawned", Float) = 0

        [Header(Beat Saber)]
        [Space(10)]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutSize("CutoutSize", Range(0.2,10)) = 1.0
        _CutoutEdgeWidth("Cutout Edge Width", Range(0, 0.2)) = 0.05
        _CutoutEdgeGlow("Cutout Edge Glow", Range(0, 1)) = 0.5
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)
        _CutPlane("Cut Plane", Vector) = (0, 0, 0, 0)

        [Header(Fog Settings)] [Space]
        [Toggle(ENABLE_FOG)] _EnableFog ("Enable Fog", Float) = 1
        _FogStartOffset ("Fog Start Offset", Float) = 1
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1

        [Header(Editor)]
        [Space(10)]
        [Toggle] _AlwaysTranslucent("Always Translucent", Float) = 0.0
        _TranslucentAlpha("Translucent Alpha", Float) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
            "PassFlags"="OnlyDirectional"
        }
        Cull Off

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "../CGIncludes/BloomFog.cginc"
        #pragma multi_compile_instancing

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _OverNoteInterfaceColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _OutlineWidth)
            UNITY_DEFINE_INSTANCED_PROP(float, _TranslucentAlpha)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
            UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
            UNITY_DEFINE_INSTANCED_PROP(float, _Lit)
            UNITY_DEFINE_INSTANCED_PROP(float, _AlwaysTranslucent)
            UNITY_DEFINE_INSTANCED_PROP(float, _AnimationSpawned)
            UNITY_DEFINE_INSTANCED_PROP(float, _ObjectTime)
        UNITY_INSTANCING_BUFFER_END(Props)

        float _Glow;
        float _CutoutEdgeGlow;
        float _CutoutEdgeWidth;
        float _CutoutSize;
        float _FogStartOffset;
        float _FogScale;
        float _FogHeightOffset;
        float _FogHeightScale;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x gles

            //#pragma target 4.5 // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html

            #pragma vertex vert
            #pragma fragment frag

            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ ENABLE_HEIGHT_FOG
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ CM_PREVIEW_MODE

            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            // Hello! We're global shader variables.
            uniform float _EnableNoteSurfaceGridLine = 1;
            uniform float _SongTime;
            sampler3D _CutoutTex;


            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 customScreenPos : POSITION1;
                float4 rotatedPos : POSITION2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 localPos : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float3 cutoutPos : TEXCOORD4;
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
                // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.localPos = i.vertex;

                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                //Global platform offset
                const float4 offset = float4(0, -0.5, -1.5, 0);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180);

                float objectTime = UNITY_ACCESS_INSTANCED_PROP(Props, _ObjectTime);

                o.rotatedPos = float4(
                    ComputeRotatedPosition(o.worldPos - offset, rotationInRadians) + offset,
                    objectTime + 0.001 - _SongTime
                );

                o.worldNormal = UnityObjectToWorldNormal(i.normal);
                o.cutoutPos = mul(unity_ObjectToWorld, i.vertex.xyz);
                return o;
            }

            float isDithered(float2 pos, float alpha)
            {
                pos *= _ScreenParams.xy;

                // Define a dither threshold matrix which can
                // be used to define how a 4x4 set of pixels
                // will be dithered
                const float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
                };

                int index = (int(pos.x) % 4) * 4 + int(pos.y) % 4;
                return alpha - DITHER_THRESHOLDS[index];
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float isTranslucent = UNITY_ACCESS_INSTANCED_PROP(Props, _AlwaysTranslucent);
                float4 interfaceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverNoteInterfaceColor);
                float4 noteColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float outlineWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineWidth);
                float lit = UNITY_ACCESS_INSTANCED_PROP(Props, _Lit);
                float animation = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationSpawned);
                float translucentAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _TranslucentAlpha);
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);

                float rotatedZ = abs(i.rotatedPos.z);

                float3 albedo = _EnableNoteSurfaceGridLine > 0 && rotatedZ < outlineWidth && isTranslucent < 1
                                    ? interfaceColor
                                    : noteColor.rgb;

                // For the sake of simplicity I'm not supporting the metallic/specular map or occlusion map
                // for an example of that see : https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl

                float metallic = lit == 1 ? 0.5 : 0;
                float smoothness = lit == 1 ? 0.7 : 0;
                float occlusion = 1;

                float alpha = animation < 1 && (isTranslucent >= 1 || i.rotatedPos.w <= 0)
                                  ? translucentAlpha
                                  : 1;

                clip(isDithered(i.customScreenPos.xy / i.customScreenPos.w, alpha));

                float noise = tex3D(_CutoutTex, (i.cutoutPos + cutoutTexOffset.xyz) * 0.25 * _CutoutSize);
                float c = noise - cutout;
                clip(c);
                if (c < _CutoutEdgeWidth * sqrt(cutout))
                {
                    return fixed4(length(albedo.rgb) / 2 + albedo.rgb, _CutoutEdgeGlow);
                }

                float3 worldNormal = normalize(i.worldNormal);

                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                fixed3 lightColor = _LightColor0.rgb;
                float diffuse = saturate(dot(worldNormal, lightDirection));

                fixed3 color = albedo.rgb * UNITY_LIGHTMODEL_AMBIENT.rgb;
                color += diffuse * lightColor * albedo.rgb;

                fixed4 bloomfog_color = fixed4(color, saturate(noteColor.a * _Glow));
                
                #ifdef CM_PREVIEW_MODE
                    #ifdef ENABLE_HEIGHT_FOG
                        BLOOM_FOG_HEIGHT_FOG_APPLY(bloomfog_color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset, _FogHeightScale);
                    #else
                        BLOOM_FOG_APPLY(bloomfog_color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                    #endif
                #endif

                return bloomfog_color;
            }
            ENDHLSL
        }
    }
}