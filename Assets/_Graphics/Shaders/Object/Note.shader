Shader "ChroMapper/Object/Note"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 0)
        _ColorMultiplier("Color Multiplier", Range(0, 10)) = 1
        _MainTex("Texture", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.95

        [Header(Rim Dim)] [Space(10)]
        [Toggle(RIM_DIM)] _EnableRimDim("Rim Dim", float) = 1
        _RimScale ("Rim Scale", Range(0, 4)) = 2
        _RimOffset ("Rim Offset", Range(-1, 1)) = 0
        _RimDistanceScale ("Rim Distance Scale", Range(0, 4)) = 0.03
        _RimDistanceOffset ("Rim Distance Offset", float) = 5
        _RimDarkening ("Rim Darkening", Range(0, 1)) = 0

        [Space(10)]
        _OutlineWidth("Outline Width", float) = 0.05
        _OverNoteInterfaceColor("Over Note Interface Color", Color) = (1, 1, 1, 0)
        _Rotation("Rotation", float) = 0
        _AnimationSpawned("Animation Spawned", float) = 0

        [Header(Beat Saber)] [Space]
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutSize("CutoutSize", Range(0.2,10)) = 1
        _CutoutEdgeWidth("Cutout Edge Width", Range(0, 0.2)) = 0.05
        _CutoutEdgeGlow("Cutout Edge Glow", Range(0, 1)) = 0.5
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)
        _CutPlane("Cut Plane", Vector) = (0, 0, 0, 0)

        [Header(Fog Settings)] [Space]
        [Toggle(ENABLE_FOG)] _EnableFog ("Enable Fog", float) = 1
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Header(Editor)] [Space]
        [Toggle] _AlwaysTranslucent("Always Translucent", float) = 0
        _TranslucentAlpha("Translucent Alpha", float) = 0.5

        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "../CGIncludes/BloomFog.cginc"
        #include "../CGIncludes/CustomLighting.cginc"
        #include "../CGIncludes/CustomTonemapping.cginc"
        #pragma multi_compile_instancing

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _ColorMultiplier)
            UNITY_DEFINE_INSTANCED_PROP(float4, _OverNoteInterfaceColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _TranslucentAlpha)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
            UNITY_DEFINE_INSTANCED_PROP(float4, _CutPlane)
            UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
            UNITY_DEFINE_INSTANCED_PROP(float, _AlwaysTranslucent)
            UNITY_DEFINE_INSTANCED_PROP(float, _AnimationSpawned)
            UNITY_DEFINE_INSTANCED_PROP(float, _ObjectTime)
        UNITY_INSTANCING_BUFFER_END(Props)

        float _Intensity;
        float _Smoothness;

        float _RimScale;
        float _RimOffset;
        float _RimDistanceScale;
        float _RimDistanceOffset;
        float _RimDarkening;

        float _OutlineWidth;
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile DIFFUSE
            #pragma multi_compile BOTH_SIDES_DIFFUSE
            #pragma multi_compile HALF_LAMBERT
            #pragma multi_compile SPECULAR
            #pragma shader_feature RIM_DIM
            #pragma multi_compile _ ENABLE_FOG
            #pragma multi_compile _ ENABLE_HEIGHT_FOG
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _ CM_PREVIEW_MODE
            #pragma multi_compile_fog
            #pragma multi_compile LOW_QUALITY_SHADER // GGX makes a dot

            uniform float _SongTime;
            sampler3D _CutoutTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 customScreenPos : POSITION1;
                float4 rotatedPos : POSITION2;
                float4 localPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float3 cutoutPos : TEXCOORD4;
                #if defined(RIM_DIM)
                float dist : TEXCOORD5;
                #endif
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
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                #if defined(RIM_DIM)
                o.dist = distance(_WorldSpaceCameraPos, o.worldPos);
                #endif
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

                int index = int(pos.x) % 4 * 4 + int(pos.y) % 4;
                return alpha - DITHER_THRESHOLDS[index];
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float isTranslucent = UNITY_ACCESS_INSTANCED_PROP(Props, _AlwaysTranslucent);
                float4 interfaceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverNoteInterfaceColor);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float colorMultiplier = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorMultiplier);
                float animation = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationSpawned);
                float translucentAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _TranslucentAlpha);
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);

                float rotatedZ = abs(i.rotatedPos.z);

                #if defined(CM_PREVIEW_MODE)
                float4 albedo = float4(color.rgb * colorMultiplier, 0);
                #else
                float4 albedo = float4(rotatedZ < _OutlineWidth && isTranslucent < 1
                                           ? interfaceColor
                                           : color.rgb * colorMultiplier, 0);
                #endif

                float alpha = animation < 1 && (isTranslucent >= 1 || i.rotatedPos.w <= 0)
                                  ? translucentAlpha
                                  : 1;

                clip(isDithered(i.customScreenPos.xy / i.customScreenPos.w, alpha));

                float noise = tex3D(_CutoutTex, (i.cutoutPos + cutoutTexOffset.xyz) * 0.25 * _CutoutSize);
                float cl = noise - cutout;
                clip(cl);
                if (cl < _CutoutEdgeWidth * cutout)
                {
                    return fixed4(albedo.rgb, _CutoutEdgeGlow);
                }

                float3 worldNormal = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 calculated = 0;
                CALCULATE_DIRECTIONAL_LIGHTING(calculated, albedo, 0, _Smoothness, 0.1, 1, 0, 0,
                                               viewDir, 1, i.worldPos, worldNormal);
                albedo.rgb = calculated.rgb;

                #if defined(RIM_DIM)
                float rim = 1 - saturate(dot(worldNormal, i.viewDir));
                float distFactor = (i.dist + _RimDistanceOffset) * _RimDistanceScale;
                float finalRim = saturate((rim + _RimOffset) * _RimScale) * distFactor;
                albedo *= (1 - finalRim * _RimDarkening);
                #endif

                ACES_TONE_MAPPING_APPLY(albedo);
                
                #if defined(CM_PREVIEW_MODE) && defined(ENABLE_FOG)
                #if defined(ENABLE_HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif
                
                return albedo;
            }
            ENDHLSL
        }
    }
}