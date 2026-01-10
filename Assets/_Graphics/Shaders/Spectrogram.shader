Shader "ChroMapper/Spectrogram"
{
    Properties
    {
        [Enum(Off,0,Front,1,Back,2)] _CullMode ("Culling Mode", Float) = 2

        [Space(10)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        [Space(10)]
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Fog Settings)]
        [Space]
        _FogStartOffset ("Fog Start Offset", Float) = 1
        _FogScale ("Fog Scale", Float) = 1
        [Space]
        [Toggle(ENABLE_HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", Float) = 0
        _FogHeightOffset ("Fog Height Offset", Float) = 0
        _FogHeightScale ("Fog Height Scale", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
            "PassFlags"="OnlyDirectional"
        }

        Pass
        {
            Cull [_CullMode]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ENABLE_BLOOM_FOG
            #pragma shader_feature ENABLE_HEIGHT_FOG

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "CGIncludes/BloomFog.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
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
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 customScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Glossiness;
            float _Metallic;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;
                o.worldNormal = UnityObjectToWorldNormal(i.normal);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.customScreenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed glossiness = _Glossiness;
                fixed metallic = _Metallic;

                fixed4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));

                fixed3 worldNormal = normalize(i.worldNormal);
                // this looks awful, but it's camera's fault
                // fixed3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                fixed3 lightDirection = normalize(float3(0.0, 1.0, -1.0));
                fixed3 lightColor = _LightColor0.rgb;
                fixed3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                float diffuse = saturate(dot(worldNormal, lightDirection));
                float3 halfDirection = normalize(lightDirection + viewDirection);
                float specular = pow(saturate(dot(worldNormal, halfDirection)), glossiness * 128) * metallic;

                fixed3 col = albedo.rgb * UNITY_LIGHTMODEL_AMBIENT.rgb;
                col += diffuse * lightColor * albedo.rgb;
                col += specular * lightColor;

                float alpha = log(1 + albedo.a);

                fixed4 bloomfog_color = fixed4(col.rgb, saturate(alpha));

                #ifdef ENABLE_HEIGHT_FOG
                    BLOOM_FOG_HEIGHT_FOG_APPLY(bloomfog_color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale, _FogHeightOffset, _FogHeightScale);
                #else
                    BLOOM_FOG_APPLY(bloomfog_color, i.customScreenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif

                return bloomfog_color;
            }
            ENDCG
        }
    }
}