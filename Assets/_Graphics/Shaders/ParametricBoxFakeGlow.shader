// Additive parametric box glow with face-relative deformation and cutout controls.
Shader "ChroMapper/Parametric Box Fake Glow"
{
    Properties
    {
        [Space] _MainTex ("Main Texture", 2D) = "white" {}

        [Space] _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        [ShowIfAny(HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", float) = 1
        [ShowIfAny(HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", float) = 0

        [Space] _AngleDisappearParam ("Angle disappear param", float) = 1
        [Space] [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", Float) = 1
        [Toggle(CUTOUT)] _EnableCutout ("Enable Vertex Cutout", float) = 0
        [ToggleShowIfAny(WORLDSPACE_NOISE_CUTOUT, CUTOUT)] _WorldspaceNoiseCutout ("Worldspace Noise Cutout", float) = 0
        [ShowIfAny(2, CUTOUT, WORLDSPACE_NOISE_CUTOUT)] _CutoutTexScale ("Cutout Noise Scale", float) = 1
        [Toggle(CLIPPING)] _EnableClipping ("Enable Clipping", float) = 0

        [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp Add
        Cull Off
        ZTest LEqual
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON
            // Global: enabled by the bloom-fog renderer during its pass.
            #pragma multi_compile _ BLOOM_FOG
            #pragma shader_feature_local HEIGHT_FOG
            #pragma shader_feature_local MAIN_EFFECT_WHITE_BOOST
            #pragma shader_feature_local _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS
            #pragma shader_feature_local CUTOUT
            #pragma shader_feature_local_fragment WORLDSPACE_NOISE_CUTOUT
            #pragma shader_feature_local_fragment CLIPPING
            // Global: the post-process bloom gate suppresses MainEffect white boost.
            #pragma multi_compile _ POST_BLOOM

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Cutout.hlsl"
            #include "ShaderLibrary/Families/ParametricShared.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SizeParams)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

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
                float3 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            float _AngleDisappearParam;
            sampler3D _CutoutTex;
            float _CutoutTexScale;
            float4 _ClippingPlane;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                // Scale offsets from each face so _SizeParams.w remains a fixed border.
                float3 faceSide = sign(i.vertex.xyz);
                i.vertex.xyz = faceSide +
                    (i.vertex.xyz - faceSide) * (2.0 * sizeParams.w / sizeParams.xyz);

                #if defined(CUTOUT)
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                i.vertex.xy *= 1.0 - cutout * cutout;
                #endif

                o.vertex = UnityObjectToClipPos(i.vertex);

                // Texture sampling uses raw UV0; this material has no ST transform.
                o.uv.xy = i.uv.xy;
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;

                float3 viewDirection = normalize(o.worldPos - GetParametricCameraPosition());
                float3 worldNormal = UnityObjectToWorldNormal(i.normal);
                // Compute angle fade per vertex so it follows the deformed surface.
                o.uv.z = min(abs(dot(viewDirection, worldNormal) * _AngleDisappearParam), 1.0);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                // Height fog is always active; the keyword selects material overrides.
                float alpha = tex2D(_MainTex, i.uv.xy).a;
                alpha *= alpha;

                float heightScale = 1.0;
                float heightOffset = 0.0;
                #if defined(HEIGHT_FOG)
                heightScale = _FogHeightScale;
                heightOffset = _FogHeightOffset;
                #endif
                float heightRamp = CalculateParametricHeightRamp(
                    i.worldPos.y, heightScale, heightOffset,
                    _CustomFogHeightFogHeight, _CustomFogHeightFogStartY);

                #if defined(WORLDSPACE_NOISE_CUTOUT)
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float3 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset).xyz;
                float3 objectOrigin = unity_ObjectToWorld._m03_m13_m23;
                float3 cutoutPosition = CalculateObjectSpaceCutoutPosition(
                    i.worldPos, objectOrigin, cutoutTexOffset, _CutoutTexScale);
                ApplyCutoutNoise(
                    tex3D(_CutoutTex, cutoutPosition).w, cutout);
                #endif
                #if defined(CLIPPING)
                clip(dot(float4(i.worldPos, 1.0), _ClippingPlane));
                #endif

                #if defined(BLOOM_FOG)
                // Bloom fog attenuates alpha here; it does not contribute a sampled color.
                float3 cameraPosition = GetParametricCameraPosition();
                float fogInverse = CalculateParametricDistanceTransmission(
                    i.worldPos, cameraPosition, _FogStartOffset, _FogScale, 1.0,
                    _CustomFogOffset, _CustomFogAttenuation);
                alpha *= saturate(heightRamp * fogInverse * i.uv.z * color.a);
                #else
                alpha *= saturate(heightRamp * i.uv.z * color.a);
                #endif

                half4 result = half4(color.rgb * alpha, alpha);
                // White boost operates on premultiplied color; POST_BLOOM gates MainEffect only.
                #if defined(MAIN_EFFECT_WHITE_BOOST) && \
                    (defined(_WHITEBOOSTTYPE_ALWAYS) || \
                     (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)))
                result.rgb = CalculateBloomComposition(
                    color.rgb, alpha, alpha, 1,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                return result;
            }
            ENDHLSL
        }
    }
}