Shader "ChroMapper/Unlit"
{
    Properties
    {
        [KeywordEnum(Opaque, Cutout)] _Mode ("Rendering Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _CullMode ("Culling Mode", Float) = 2

        [Space(10)]
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _Glow ("Glow", Range(0, 1)) = 0.0
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
            #pragma multi_compile _MODE_OPAQUE _MODE_CUTOUT
            #pragma shader_feature SOLID_COLOR

            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Glow)
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
                UNITY_VERTEX_OUTPUT_STEREO
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

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 albedo = _Color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                #if _MODE_CUTOUT
                if (albedo.a == 0) discard;
                #endif
                albedo.a = _Glow;
                return albedo;
            }
            ENDCG
        }
    }
}