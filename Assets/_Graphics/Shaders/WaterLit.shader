// Replacement for the Beat Saber game shader Custom/WaterLit.
Shader "ChroMapper/Water Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _SpecularIntensity ("Specular Intensity", float) = 1

        [Space]
        [Toggle(Z_FADE)] _ZFade ("Z Fade", float) = 0
        _ZFadePosition ("Z Fade Position", float) = 0
        _ZFadeScale ("Z Fade Scale", float) = 1

        [Space]
        [Toggle(NORMAL_MAP)] _EnableNormalMap ("Enable Normal Map", float) = 0
        _NormalTex ("Normal Texture", 2D) = "bump" {}
        _NormalScale ("Normal Scale", float) = 1
        _NormalScaleVertical ("Falling Normal Scale", float) = 1
        _NormalTexScrolling ("Texture Scrolling", Vector) = (0,2,0,0)

        [Header(Fog Settings)] [Space]
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 0
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

        [Space]
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

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment Z_FADE
            #pragma multi_compile_local_fragment DIFFUSE
            #pragma multi_compile_local_fragment SPECULAR
            #pragma shader_feature_local_fragment NORMAL_MAP
            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomLighting.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"

            float _Metallic;
            float _Smoothness;
            
            float _ZFadePosition;
            float _ZFadeScale;

            sampler2D _NormalTex;
            float4 _NormalTex_ST;
            float _NormalScale;
            float _NormalScaleVertical;
            float2 _NormalTexScrolling;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                #if defined(NORMAL_MAP)
                float4 tangent : TANGENT;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                #if defined(NORMAL_MAP)
                float4 tangent : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                o.uv.xy = i.uv.xy;
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                #if defined(NORMAL_MAP)
                o.tangent = float4(UnityObjectToWorldDir(i.tangent.xyz), i.tangent.w);
                #endif

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                albedo.a = 0;

                float3 worldNormal = i.worldNormal;

                #if defined(NORMAL_MAP)
                float3 normalTangent = UnpackNormalWithScale(
                    tex2D(_NormalTex, TRANSFORM_TEX(i.uv, _NormalTex) + _NormalTexScrolling.xy * _Time.xx),
                    _NormalScale);
                normalTangent = normalize(normalTangent);
                float3 worldTangent = normalize(i.tangent.xyz);
                float3 worldBitangent = cross(worldNormal, worldTangent) * i.tangent.w;
                float3x3 tbn = float3x3(worldTangent, worldBitangent, worldNormal);
                worldNormal = normalize(mul(normalTangent, tbn));
                #endif

                float3 calculated = 0;
                float metallic = _Metallic;
                float smoothness = _Smoothness;
                float specIntensity = 1;
                float diffuseBothSides = 0;
                CUSTOM_LIGHTING_APPLY(calculated, albedo, metallic, smoothness, specIntensity,
                                      diffuseBothSides, i.worldPos, worldNormal);
                albedo.rgb = calculated;
                
                ACES_TONE_MAPPING_APPLY(albedo);

                #if defined(BLOOM_FOG) && defined(FOG)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset,
                                       _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}