Shader "ChroMapper/BloomfogMesh"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../CGIncludes/CustomTonemapping.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
                //float4 viewPos : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint id : TEXCOORD1;
            };

            // Need a dedicated color buffer because vertex colors dont support HDR
            uniform float4x4 _VertexTransformMatrix;
            uniform StructuredBuffer<float4> _BloomfogColorBuffer;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(_VertexTransformMatrix, v.vertex);
                o.uv = v.uv;
                o.id = v.id;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 color = _BloomfogColorBuffer[i.id];
                REINHARD_TONE_MAPPING_APPLY(color)
                return color;
            }
            ENDCG
        }
    }
}
