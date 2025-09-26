Shader "ChroMapper/Lit"
{
    Properties
    {
        [KeywordEnum(Opaque, Cutout)] _Mode ("Rendering Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _CullMode ("Culling Mode", Float) = 2

        [Space(10)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Glow ("Glow", Range(0, 5)) = 0.0

        [Space(10)]
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "LightMode" = "ForwardBase"
        }

        Pass
        {
            Cull [_CullMode]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _MODE_OPAQUE _MODE_CUTOUT

            #include "UnityPBSLighting.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Glow)
                UNITY_DEFINE_INSTANCED_PROP(float, _Glossiness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
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
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, v2f o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed glow = UNITY_ACCESS_INSTANCED_PROP(Props, _Glow);
                fixed glossiness = UNITY_ACCESS_INSTANCED_PROP(Props, _Glossiness);
                fixed metallic = UNITY_ACCESS_INSTANCED_PROP(Props, _Metallic);

                fixed4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));

                #if _MODE_CUTOUT
                if (albedo.a == 0) discard;
                #endif

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

                return fixed4(col.rgb, log2(glow + 1.0));
            }
            ENDCG
        }
    }
}