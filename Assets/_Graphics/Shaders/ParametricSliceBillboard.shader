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
        _BloomMultiplier ("Bloom Multiplier", float) = 1
        _BloomWhiteMultiplier ("White Multiplier", float) = 1

        [Header(Others)] [Space]
        [Toggle(SQUARE_ALPHA)] _SquareAlpha("Square Alpha", float) = 1
        [Toggle(ANGLE_DISAPPEAR)] _EnableAngleDisappear("Angle Disappear", float)= 1
        [Toggle(Y_AXIS_BILLBOARD)] _EnableYAxisBillboard ("Y Axis Billboard", float) = 1

        [Header(Fog Settings)] [Space]
        [Toggle(ENABLE_FOG)] _EnableFog ("Enable Fog", float) = 1
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature ALPHA_WIDTH_SCALE
            #pragma shader_feature SQUARE_ALPHA
            #pragma shader_feature ANGLE_DISAPPEAR
            #pragma shader_feature Y_AXIS_BILLBOARD
            #pragma multi_compile _ _BLOOMTYPE_PP _BLOOMTYPE_FRAG

            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma multi_compile _FOGTYPE_ALPHA
            #pragma multi_compile _ ENABLE_FOG
            #pragma multi_compile _ ENABLE_HEIGHT_FOG
            #pragma shader_feature USE_FOG_FOR_LIGHTS

            #include "UnityCG.cginc"
            #include "CGIncludes/BloomFog.cginc"
            #include "CGIncludes/CustomBloom.cginc"
            #include "CGIncludes/CustomTonemapping.cginc"

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
                float3 uv : TEXCOORD0;
                float lengthFactor : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 customScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _CapUVSize;

            float _BloomMultiplier;
            float _BloomWhiteMultiplier;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

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
                o.lengthFactor = (height + sizeParams.w) / maxHeight;
                height -= offset;
                width *= sizeParams.x;

                i.vertex.x *= width;
                i.vertex.y = height;

                #if defined(Y_AXIS_BILLBOARD)
                float3 worldPos = worldOrigin + right * i.vertex.x + localUp * i.vertex.y;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                #else
                o.vertex = UnityObjectToClipPos(i.vertex);
                #endif

                o.uv = float3(i.uv * width / sizeParams.x, width / sizeParams.x);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);

                // TODO: what does cap UV size supposed to do
                // i.uv.xy = min(adjustedUv, i.uv.xy);
                float2 adjustedUv = i.uv.xy / i.uv.z;
                fixed4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(adjustedUv, _MainTex));

                #if defined(USE_FOG_FOR_LIGHTS)
                #if defined(SQUARE_ALPHA)
                albedo.a *= albedo.a;
                #endif
                fixed alphaFactor = lerp(alphaWidth.x, alphaWidth.y, i.lengthFactor);
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

                #if defined(ENABLE_FOG)
                #if defined(ENABLE_HEIGHT_FOG)
                BLOOM_FOG_HEIGHT_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale,
                                           _FogHeightOffset, _FogHeightScale);
                #else
                BLOOM_FOG_APPLY(albedo, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                #if !defined(USE_FOG_FOR_LIGHTS)
                #if defined(SQUARE_ALPHA)
                albedo.a *= albedo.a;
                #endif
                fixed alphaFactor = lerp(alphaWidth.x, alphaWidth.y, i.lengthFactor);
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
            ENDCG
        }
    }
}