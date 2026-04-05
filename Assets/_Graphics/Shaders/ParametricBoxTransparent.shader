Shader "ChroMapper/Parametric Box Transparent"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _AlphaWidth("Alpha Width", Vector) = (1,1,1,1)

        [Header(Neon Settings)] [Space]
        _BaseColorBoost ("Base Color Boost", float) = 1
        _BaseColorBoostThreshold ("Base Color Boost Threshold", float) = 0.5

        [Space]
        [Toggle(ENABLE_WORLD_NOISE)] _EnableWorldNoise ("Enable World Noise", float) = 0
        _WorldNoiseScale ("World Noise Scale", float) = 1
        _WorldNoiseIntensityOffset ("World Noise Intensity Offset", float) = 0
        _WorldNoiseIntensityScale ("World Noise Intensity Scale", float) = 1
        _WorldNoiseScrolling ("World Noise Scrolling", Vector) = (0,0,0,1)

        [Header(Fog Settings)] [Space]
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1

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

        [Header(Stencil)] [Space]
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Stencil
        {
            Ref [_StencilRefValue]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local ENABLE_WORLD_NOISE

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomBloom.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            float _BaseColorBoost;
            float _BaseColorBoostThreshold;

            #if defined(ENABLE_WORLD_NOISE)
            sampler3D _CutoutTex;
            float4 _TimeHelperOffset;
            float _WorldNoiseScale;
            float _WorldNoiseIntensityOffset;
            float _WorldNoiseIntensityScale;
            float3 _WorldNoiseScrolling;
            #endif

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlphaWidth)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                // neon: per-vertex alpha factor (cubic, like texcoord3 in NeonLight)
                float alphaFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);

                o.uv.w = (1 + i.vertex.y) / 2;
                float width = lerp(alphaWidth.z, alphaWidth.w, o.uv.w);

                i.vertex.x = i.vertex.x * width;
                i.vertex.z = i.vertex.z * width;

                o.vertex = UnityObjectToClipPos(i.vertex);

                o.uv.xyz = float3(i.uv * width / alphaWidth.z, width / alphaWidth.w);
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                // Pick alpha factor based on top/bottom vertex like NeonLight does with AlphaStart/AlphaEnd
                half alphaFactor = lerp(alphaWidth.x, alphaWidth.y, o.uv.w);
                o.alphaFactor = alphaFactor;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                // Cubic alpha ramp matching NeonLight: alpha^3 * Color.w
                float a = i.alphaFactor;
                float cubicAlpha = pow(a,3) * color.a;

                #if defined(ENABLE_WORLD_NOISE)
                // Sample 3D worldspace noise to modulate alpha (from NeonLight)
                float time = _TimeHelperOffset.x + _Time.x;
                float3 noiseUv = _WorldNoiseScrolling * time + i.worldPos.xyz;
                noiseUv *= _WorldNoiseScale;
                float4 noiseSample = tex3D(_CutoutTex, noiseUv);
                float noiseIntensity = noiseSample.w * _WorldNoiseIntensityScale + _WorldNoiseIntensityOffset;
                cubicAlpha = noiseIntensity * cubicAlpha;
                #endif

                // Square the combined alpha (NeonLight does alpha^2 before boost)
                float alpha2 = cubicAlpha * cubicAlpha;

                // BaseColorBoost: bright emissive white push matching NeonLight
                float boost = alpha2 * alpha2 * _BaseColorBoost - _BaseColorBoostThreshold;
                float4 albedo;
                albedo.rgb = saturate(color.rgb * cubicAlpha);
                albedo.a = cubicAlpha;

                #if defined(BLOOM_FOG)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale,
                                       _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                CUSTOM_BLOOM_PP_APPLY(albedo, 1);
                ACES_TONE_MAPPING_APPLY(albedo);

                return albedo;
            }
            ENDHLSL
        }
    }
}