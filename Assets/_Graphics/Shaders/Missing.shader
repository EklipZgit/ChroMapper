Shader "ChroMapper/Missing"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ CM_PREVIEW_MODE

            #include "UnityCG.cginc"

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

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;

                return o;
            }

            #if defined(CM_PREVIEW_MODE)
            fixed4 frag(v2f i) : SV_Target
            {
                // Force fail so you can at least see the map in preview mode
                clip(-1);
                return fixed4(1, 0, 1, 1);
            }
            #else
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = 0;

                float tileSize = 0.25;
                if ((i.uv.x % tileSize > tileSize / 2 && i.uv.y % tileSize < tileSize / 2) || (i.uv.y % tileSize >
                    tileSize / 2 && i.uv.x % tileSize < tileSize / 2))
                    col.rb = 1;

                return col;
            }
            #endif
            ENDCG
        }
    }
}