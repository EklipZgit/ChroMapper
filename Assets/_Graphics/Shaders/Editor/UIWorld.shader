Shader "ChroMapper/Editor/UI World"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        ZWrite Off
        Cull Off
        Blend One OneMinusSrcColor

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                half4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                albedo.rgb *= albedo.a;
                albedo.a = 0;

                return saturate(albedo);
            }
            ENDHLSL
        }
    }
}