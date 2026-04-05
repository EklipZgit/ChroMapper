Shader "ChroMapper/Parametric Slice Billboard"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _CapUVSize ("Cap UV Size", Float) = 0.25

        _SizeParams("Size Params", Vector) = (0.25,10,0,0.5)
        [Toggle(ALPHA_WIDTH_SCALE)] _EnableAlphaWidthScale ("Alpha Width Scale", float) = 0
        _AlphaWidth("Alpha Width", Vector) = (1,1,1,1)

        [Space(12)]
        [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", float) = 0
        _WhiteBoostMultiplier ("White Boost Multiplier", float) = 1
        _BaseColorBoost ("Base Color Boost", float) = 1
        _BaseColorBoostThreshold ("Base Color Boost Threshold", float) = 0.1
        _BloomMultiplier ("Bloom Multiplier", float) = 1

        [Header(Others)] [Space]
        [Toggle(SQUARE_ALPHA)] _SquareAlpha("Square Alpha", float) = 1
        [Toggle(ANGLE_DISAPPEAR)] _EnableAngleDisappear("Angle Disappear", float) = 1
        [Toggle(Y_AXIS_BILLBOARD)] _EnableYAxisBillboard ("Y Axis Billboard", float) = 1

        [Space(12)]
        [Toggle(ENABLE_WORLD_NOISE)] _EnableWorldNoise ("Enable World Noise", float) = 0
        _WorldNoiseScale ("World Noise Scale", float) = 1
        _WorldNoiseIntensityOffset ("World Noise Intensity Offset", float) = 0
        _WorldNoiseIntensityScale ("World Noise Intensity Scale", float) = 1
        _WorldNoiseScrolling ("World Noise Scrolling", Vector) = (0,0,0,1)

        [Space(12)]
        [Toggle(ENABLE_WORLD_SPACE_FADE)] _EnableWorldSpaceFade ("Enable World Space Fade", float) = 0
        _WorldSpaceFadePos ("World Space Fade Position", float) = 0
        _WorldSpaceFadeSlope ("World Space Fade Slope", float) = 1

        [Header(Fog Settings)] [Space]
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1
        [Space]
        [Toggle(USE_FOG_FOR_LIGHTS)] _UseFogForLights("Use Fog For Lights", float) = 1

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
        _OffsetFactor ("Offset Factor", Float) = 0
        _OffsetUnits ("Offset Units", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Offset [_OffsetFactor], [_OffsetUnits]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local ALPHA_WIDTH_SCALE
            #pragma shader_feature_local_fragment SQUARE_ALPHA
            #pragma shader_feature_local ANGLE_DISAPPEAR
            #pragma shader_feature_local Y_AXIS_BILLBOARD
            #pragma shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS

            #pragma shader_feature_local ENABLE_WORLD_NOISE
            #pragma shader_feature_local_fragment ENABLE_WORLD_SPACE_FADE

            #pragma multi_compile_local_fragment _FOGTYPE_ALPHA
            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment USE_FOG_FOR_LIGHTS

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/BloomFog.hlsl"
            #include "ShaderLibrary/CustomBloom.hlsl"
            #include "ShaderLibrary/CustomTonemapping.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _CapUVSize;

            float _BloomMultiplier;
            float _BaseColorBoost;
            float _BaseColorBoostThreshold;
            float _WhiteBoostMultiplier;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            #if defined(ENABLE_WORLD_NOISE)
            sampler3D _CutoutTex;
            float4 _TimeHelperOffset;
            float _WorldNoiseScale;
            float _WorldNoiseIntensityOffset;
            float _WorldNoiseIntensityScale;
            float3 _WorldNoiseScrolling;
            #endif

            #if defined(ENABLE_WORLD_SPACE_FADE)
            float _WorldSpaceFadePos;
            float _WorldSpaceFadeSlope;
            #endif

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SizeParams)
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
                float3 texcoord1 : TEXCOORD1; // xy = UV, z = width divisor
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float alphaFactor : TEXCOORD5; // AlphaStart/AlphaEnd chosen per vertex
                #if defined(ANGLE_DISAPPEAR)
                float angleDisappear : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);
                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                float3 localUp = normalize(mul((float3x3)unity_ObjectToWorld, float3(0,1,0)));
                float3 dirToCam = _WorldSpaceCameraPos - worldOrigin;

                #if defined(ANGLE_DISAPPEAR)
                // Angle disappear: smoothstep based on how face-on the slice is to camera
                float3 localCamDir = normalize(dirToCam - localUp * dot(dirToCam, localUp));
                float3 right2 = normalize(cross(localUp, localCamDir));
                float2 flatCam = normalize(float2(localCamDir.x, localCamDir.z));
                float3 localCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
                float3 toCamLocal = localCamPos + unity_WorldToObject._m03_m13_m23;
                float camDistFlat = sqrt(dot(toCamLocal.xz, toCamLocal.xz));
                float2 toCamDir = toCamLocal.xz / camDistFlat;
                float angleDot = dot(toCamDir, flatCam);
                angleDot = saturate((angleDot - 0.05) * 2.222222);
                float ao = angleDot * angleDot;
                o.angleDisappear = ao * (-2.0 * angleDot + 3.0);
                #endif

                float3 look = normalize(dirToCam - localUp * dot(dirToCam, localUp));
                float3 right = -normalize(cross(localUp, look));

                float width = 1;
                float height;
                float offset = sizeParams.y * sizeParams.z;

                if (i.uv.y < 0.25)
                {
                    float t = 1 - i.uv.y / 0.25;
                    #if defined(ALPHA_WIDTH_SCALE)
                    width = alphaWidth.z;
                    #endif
                    height = -sizeParams.w * t;
                }
                else if (i.uv.y < 0.75)
                {
                    float t = (i.uv.y - 0.25) * 2;
                    #if defined(ALPHA_WIDTH_SCALE)
                    width = lerp(alphaWidth.z, alphaWidth.w, t);
                    #endif
                    height = sizeParams.y * t;
                }
                else
                {

                    float t = (i.uv.y - 0.75) / 0.25;
                    #if defined(ALPHA_WIDTH_SCALE)
                    width = alphaWidth.w;
                    #endif
                    height = sizeParams.y + sizeParams.w * t;
                }

                float maxHeight = sizeParams.y + sizeParams.w * 2;
                float lengthFactor = (height + sizeParams.w) / maxHeight;
                height -= offset;
                width *= sizeParams.x*2;
                float effectiveWidth = lerp(sizeParams.x, width, lengthFactor);
                i.vertex.z *= effectiveWidth;
                i.vertex.x /= pow(-(i.vertex.x)*2,2);
                i.vertex.y = height * length(mul((float3x3)unity_ObjectToWorld, float3(0,1,0)));

                #if defined(Y_AXIS_BILLBOARD)
                float3 worldPos = worldOrigin + right * i.vertex.x + localUp * i.vertex.y;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos = worldPos;
                #else
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                #endif

                // texcoord1: xy=uv, z=width ratio for perspective-correct UV
                float uvCentered = i.uv.y - 0.5;
                bool isCap = abs(uvCentered) >= 0.49;
                float uvSign = 0;
                if (uvCentered > 0) uvSign = 1;
                if (uvCentered < 0) uvSign = -1;
                float capOffset = isCap ? 0.0 : ((0.25 - _CapUVSize) * floor(uvSign));

                o.texcoord1 = float3(i.uv.x, i.uv.y + capOffset, width/ sizeParams.x);
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                // AlphaStart/AlphaEnd: top half uses AlphaEnd, bottom uses AlphaStart
                // matches Output4's texcoord5 pattern: (0.5 - sizeParams.z) < height
                o.alphaFactor = lerp(alphaWidth.x, alphaWidth.y, lengthFactor);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                // Perspective-correct UV sample
                float2 adjustedUv = float2(
    (i.texcoord1.x - 0.5) / i.texcoord1.z + 0.5, // remap X based on width ratio
    i.texcoord1.y
);
float4 texSample = tex2D(_MainTex, adjustedUv);

                // Cubic alpha from vertex alpha factor * Color.a, then squared with tex alpha
                // Matches Output4: alpha = (texAlpha^2) * (alphaFactor^3 * Color.w)
                float texAlpha = texSample.w * texSample.w;
                float cubicAlpha = i.alphaFactor * i.alphaFactor * color.a;
                float alpha = texAlpha * cubicAlpha;

                #if defined(ENABLE_WORLD_NOISE)
                float time = _TimeHelperOffset.x + _Time.x;
                float3 noiseUv = (_WorldNoiseScrolling * time + i.worldPos) * _WorldNoiseScale;
                float4 noiseSample = tex3D(_CutoutTex, noiseUv);
                float noiseIntensity = noiseSample.w * _WorldNoiseIntensityScale + _WorldNoiseIntensityOffset;
                alpha *= noiseIntensity;
                #endif

                #if defined(ENABLE_WORLD_SPACE_FADE)
                float worldFade = saturate((i.worldPos.y - _WorldSpaceFadePos) * _WorldSpaceFadeSlope);
                alpha *= worldFade;
                #endif

                #if defined(ANGLE_DISAPPEAR)
                alpha *= i.angleDisappear;
                #endif

                #if defined(SQUARE_ALPHA)
                alpha *= alpha;
                #endif


                #if defined(_WHITEBOOSTTYPE_ALWAYS) || defined(_WHITEBOOSTTYPE_MAINEFFECT)
                float boost = alpha * alpha;
                boost *= _WhiteBoostMultiplier;
                boost *= boost;
                boost = boost * _BaseColorBoost - _BaseColorBoostThreshold;
                float4 albedo;
                albedo.rgb = saturate(color.rgb * alpha + boost);
                albedo.a = alpha * _BloomMultiplier;
                #else
                float4 albedo;
                albedo.rgb = color.rgb * alpha;
                albedo.a = alpha * _BloomMultiplier;
                #endif

                #if defined(BLOOM_FOG) && defined(FOG)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale,
                                       _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                ACES_TONE_MAPPING_APPLY(albedo);

                return albedo;
            }
            ENDHLSL
        }
    }
}