// _Offset remains serialized for material compatibility; no shader path reads it.
Shader "ChroMapper/Clouds Opaque"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [ShowIfAny(WORLD_NOISE)] _NoiseTex ("Noise Texture", 2D) = "white" {}

        [Space(20)]
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScale ("World Noise Scale", float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityScale ("World Noise Intensity Scale", float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityOffset ("World Noise Intensity Offset", float) = 0
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScrolling ("World Noise Scrolling", Vector) = (0, 0, 0, 1)

        [Space(20)]
        [ShowIfAny(WORLD_NOISE)] _Speed ("Speed", float) = 1
        _Offset ("Offset", float) = 0

        [Space(20)]
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", float) = 0
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", float) = 1
        [ShowIfAny(FOG)] _HeightFogOffset ("Height Fog Offset", float) = 1

        [Space(20)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", float) = 2

        [Space(20)]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Enable Diffuse", float) = 1
        [ToggleShowIfAny(BOTH_SIDES_DIFFUSE, DIFFUSE)] _EnableBothSidesDiffuse ("Both Sides Diffuse", float) = 0
        [ToggleShowIfAny(INVERT_DIFFUSE_NORMAL, DIFFUSE)] _InvertDiffuseNormal ("Invert Diffuse Normal", float) = 0
        [Toggle(WORLD_NOISE)] _EnableWorldNoise ("Enable World Noise", float) = 0
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        [Toggle(NOISE_DITHERING)] _EnableNoiseDithering ("Noise Dithering", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Cull [_CullMode]
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment BOTH_SIDES_DIFFUSE
            #pragma shader_feature_local_vertex WORLD_NOISE
            #pragma shader_feature_local_fragment INVERT_DIFFUSE_NORMAL
            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment NOISE_DITHERING
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Lighting.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float3 world : TEXCOORD2;
                float3 nor : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float4 noiseScreenPos : TEXCOORD5;
                float4 position : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float4 _TimeHelperOffset;
            float _WorldNoiseScale;
            float _WorldNoiseIntensityScale;
            float _WorldNoiseIntensityOffset;
            float4 _WorldNoiseScrolling;
            float _Speed;
            float _Offset;
            float _FogStartOffset;
            float _FogScale;
            float _HeightFogOffset;

            #if defined(SHADER_STAGE_VERTEX) && defined(WORLD_NOISE)
            float _GlobalRandomValue;
            #endif

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Build the radial swirl in object space before world-noise displacement.
                float phase = sin(v.vertex.z * 12.345);
                float wave = sign(phase) * (phase * 0.5 + 1.0);

                // AudioTimeSyncController offsets the swirl phase for mapper playback.
                float angle = (v.vertex.x + wave * _Speed *
                    (_Time.y + _TimeHelperOffset.y)) / v.vertex.z;
                float3 pos = float3(sin(angle) * v.vertex.z, v.vertex.y, cos(angle) * v.vertex.z);

                // Fog and the radial normal use pre-noise world space; only clip
                // position receives the vertical noise displacement.
                float3 world = mul(unity_ObjectToWorld, float4(pos, 1.0)).xyz;
                o.world = world;
                o.nor = -normalize(world);

                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.color = v.color;

                #if defined(WORLD_NOISE)
                float2 scroll = _WorldNoiseScrolling.xy *
                    (_Time.x + _TimeHelperOffset.x);
                float2 nuv = world.xz * _NoiseTex_ST.xy + _NoiseTex_ST.zw + scroll;
                float noise = tex2Dlod(_NoiseTex, float4(nuv, 0, 0)).x;
                float displacement = noise * _WorldNoiseIntensityScale + _WorldNoiseIntensityOffset;
                world = mul(unity_ObjectToWorld,
                            float4(pos.x, pos.y + displacement, pos.z, 1.0)).xyz;
                #endif

                o.position = mul(unity_MatrixVP, float4(world, 1.0));
                o.screenPos = ComputeScreenPosCustom(o.position);
                #if defined(SHADER_STAGE_VERTEX) && defined(WORLD_NOISE)
                o.noiseScreenPos = ComputeNonStereoScreenPos(o.position);
                o.noiseScreenPos.xy = o.noiseScreenPos.xy * _GlobalBlueNoiseParams
                    + o.position.w * _GlobalRandomValue;
                o.noiseScreenPos.xy += float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13);
                #else
                o.noiseScreenPos = o.screenPos;
                o.noiseScreenPos.xy *= _GlobalBlueNoiseParams;
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 cameraPosition = _WorldSpaceCameraPos;
                #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                cameraPosition = unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
                #endif
                float3 d = i.world - cameraPosition;
                // Apply the global squared-distance fog domain before material offset and scale.
                float dist2 = dot(d, d);
                float distFade = 1.0 / (1.0 +
                    max(0.0, max(0.0, dist2 - _CustomFogOffset) *
                        _CustomFogAttenuation - _FogStartOffset) * _FogScale);

                // Ordinary fog uses this height ramp; BLOOM_FOG also applies distance fade.
                float hFade = CalculateCustomHeightFogFactor(
                    i.world, _HeightFogOffset, 1.0);
                float fade = 1.0 - distFade * hFade;

                #if defined(DIFFUSE) && defined(INVERT_DIFFUSE_NORMAL)
                float3 normal = i.nor;
                #else
                float3 normal = normalize(i.nor);
                #endif
                #if defined(INVERT_DIFFUSE_NORMAL)
                normal = -normal;
                #endif
                float3 color = tex2D(_MainTex, i.uv).rgb * i.color.rgb;
                #if defined(DIFFUSE)
                // Clouds use CM's five directional lights without an ambient term.
                color *= CalculateLightDiffuse(normal);
                color = saturate(color);
                #endif

                #if defined(ACES_TONE_MAPPING)
                color = ApplyAcesTonemapping(float4(color, 0.0)).rgb;
                #endif

                #if defined(FOG)
                #if defined(BLOOM_FOG)
                // Bloom fog samples the prepass with direct projected coordinates.
                color = lerp(color, SampleBloomPrePass(i.screenPos).rgb, fade);
                #else
                // Ordinary fog blends toward 0.1 with the inverse height ramp.
                color = lerp(color, 0.1.xxx, 1.0 - hFade);
                #endif
                #endif

                #if defined(NOISE_DITHERING)
                // Dither runs after fog; ApplyNoiseDither supplies the /255 amplitude.
                float4 result = ApplyNoiseDither(
                    float4(color, 0), i.noiseScreenPos, _GlobalBlueNoiseTex);
                #else
                float4 result = float4(color, 0);
                #endif

                // Opaque cloud routes always clear the bloom alpha channel.
                return result;
            }
            ENDHLSL
        }
    }
}
