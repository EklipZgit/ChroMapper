Shader "ChroMapper/Look At Bloom Fog"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off
        ZTest Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Pass
        {
            CGPROGRAM
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
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float3 directionToCamera()
            {
                return _WorldSpaceCameraPos - unity_ObjectToWorld._m03_m13_m23;
            }

            float3x3 matrixFromBasis(float3 x, float3 y, float3 z)
            {
                return float3x3(
                    x.x, y.x, z.x,
                    x.y, y.y, z.y,
                    x.z, y.z, z.z
                );
            }

            float3 rotateLook(float3 forward, float3 p)
            {
                forward = normalize(forward);

                float3 up = float3(0, 1, 0);

                float3 right = normalize(cross(forward, up));
                up = cross(right, forward);

                float3x3 m = matrixFromBasis(right, up, forward);

                return -mul(m, p);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed4 albedo = color * tex2D(_MainTex, i.uv);
                return albedo;
            }
            ENDCG
        }
    }
}