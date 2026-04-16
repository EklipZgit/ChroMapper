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

        [KeywordEnum(None, PP, Frag)] _BloomType ("Bloom White", float) = 0
        [ShowIfAny(_BLOOMTYPE_PP, _BLOOMTYPE_FRAG)] _BloomWhiteMultiplier ("White Multiplier", float) = 1
        _BloomMultiplier ("Bloom Multiplier", float) = 1

        [Header(Others)] [Space]
        [Toggle(SQUARE_ALPHA)] _SquareAlpha("Square Alpha", float) = 1
        [Toggle(ANGLE_DISAPPEAR)] _EnableAngleDisappear("Angle Disappear", float)= 1
        [Toggle(Y_AXIS_BILLBOARD)] _EnableYAxisBillboard ("Y Axis Billboard", float) = 1

        [Header(Fog Settings)] [Space]
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", float) = 1
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", float) = 1
        [Space]
        [ToggleShowIfAny(HEIGHT_FOG, FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", float) = 0
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", float) = 1
        [Space]
        [ToggleShowIfAny(USE_FOG_FOR_LIGHTS, FOG)] _UseFogForLights("Use Fog For Lights", float) = 1

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
        [Header(Stencil)] [Space]
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", Float) = 0
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

            #pragma shader_feature_local_vertex ALPHA_WIDTH_SCALE
            #pragma shader_feature_local_fragment SQUARE_ALPHA
            #pragma shader_feature_local_fragment ANGLE_DISAPPEAR
            #pragma shader_feature_local_vertex Y_AXIS_BILLBOARD
            #pragma shader_feature_local_fragment _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG

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
            float _CapUVSize; // Fixed: was incorrectly declared as float2

            float _BloomMultiplier;
            float _BloomWhiteMultiplier;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

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
                float4 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);
                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 localUp = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
                float3 dirToCam = _WorldSpaceCameraPos - worldOrigin;
                float3 look = normalize(dirToCam - localUp * dot(dirToCam, localUp));
                float3 right = -normalize(cross(localUp, look));

                float width = 1;
                float height;
                float offset = sizeParams.y * sizeParams.z;
                // TODO: replace t and lerp with vertex access
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
                o.uv.w = (height + sizeParams.w) / maxHeight;
                height -= offset;
                width *= sizeParams.x;

                i.vertex.x *= width;
                i.vertex.y = height * length(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));

                #if defined(Y_AXIS_BILLBOARD)
                float3 worldPos = worldOrigin + right * i.vertex.x + localUp * i.vertex.y;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos.xyz = worldPos;
                #else
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                #endif

                // Cap UV shift: nudge cap UVs inward to avoid sampling texture edges.
                // Matches Output4 behaviour: shift = (0.25 - _CapUVSize) * sign of cap direction.
                // Middle region (0.25 - 0.75) is left untouched.
                float uvY = i.uv.y;
                bool isMiddle = uvY >= 0.25 && uvY <= 0.75;
                float capSign = floor((uvY < 0.5 ? -1.0 : 0.0) - (uvY > 0.5 ? 1.0 : 0.0));
                float capShift = isMiddle ? 0.0 : (0.25 - _CapUVSize) * capSign;
                i.uv.y += capShift;

                o.uv.xyz = float3(i.uv * width / sizeParams.x, width / sizeParams.x);
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);

                float2 adjustedUv = i.uv.xy / i.uv.z;
                half4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(adjustedUv, _MainTex));

                #if defined(USE_FOG_FOR_LIGHTS)
                #if defined(SQUARE_ALPHA)
                albedo.a *= albedo.a;
                #endif
                half alphaFactor = lerp(alphaWidth.x, alphaWidth.y, i.uv.w);
                #if defined(SQUARE_ALPHA)
                alphaFactor *= alphaFactor;
                #endif
                albedo *= alphaFactor;

                #if defined(_BLOOMTYPE_PP)
                CUSTOM_BLOOM_PP_APPLY(albedo, _BloomMultiplier);
                #elif defined(_BLOOMTYPE_FRAG)
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _BloomWhiteMultiplier);
                #else
                CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(albedo);
                #endif

                ACES_TONE_MAPPING_APPLY(albedo);

                #endif

                #if defined(BLOOM_FOG) && defined(FOG)
                #if defined(HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset,
                                       _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                #if !defined(USE_FOG_FOR_LIGHTS)
                #if defined(SQUARE_ALPHA)
                albedo.a *= albedo.a;
                #endif
                half alphaFactor = lerp(alphaWidth.x, alphaWidth.y, i.uv.w);
                #if defined(SQUARE_ALPHA)
                alphaFactor *= alphaFactor;
                #endif
                albedo *= alphaFactor;

                #if defined(_BLOOMTYPE_PP)
                CUSTOM_BLOOM_PP_APPLY(albedo, _BloomMultiplier);
                #elif defined(_BLOOMTYPE_FRAG)
                CUSTOM_BLOOM_FRAG_APPLY(albedo, _BloomWhiteMultiplier);
                #else
                CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(albedo);
                #endif

                ACES_TONE_MAPPING_APPLY(albedo);

                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}