Shader "ChroMapper/Lit"
{
    Properties
    {
        [KeywordEnum(Opaque, Cutout)] _Mode ("Rendering Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _CullMode ("Culling Mode", Float) = 2

        [Space(10)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Glow ("Glow", Range(0, 1)) = 0.0

        [Space(10)]
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Cull [_CullMode]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature ENABLE_CUTOUT
            #pragma multi_compile _MODE_OPAQUE _MODE_CUTOUT
    
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Glow)
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
            };

            sampler2D _MainTex;
			float4 _MainTex_ST;

            float _Glossiness;
            float _Metallic;

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, v2f o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = worldPos.xyz;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 albedo = _Color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                
                #if _MODE_CUTOUT
                if (albedo.a == 0) discard;
                #endif

                float3 worldNormal = normalize(i.worldNormal);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = float3(1, 1, 1);

                half NdotL = lerp(0.0, 1.0, max(0.0, dot(worldNormal, lightDirection)));
                float3 diffuse = NdotL * lightColor * albedo.rgb;

                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 halfDirection = normalize(lightDirection + viewDirection);
                float NdotH = max(0.0, dot(worldNormal, halfDirection));
                float specularPower = exp2(10.0 * _Glossiness + 1.0);
                float3 specular = pow(NdotH, specularPower) * lightColor * _Metallic;

                float3 col = diffuse + specular;

                return float4(col.rgb, _Glow);
            }
            ENDCG
        }
    }
}