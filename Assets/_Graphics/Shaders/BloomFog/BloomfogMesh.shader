Shader "ChroMapper/BloomfogMesh"
{
    Properties
    {
        _BloomfogAlphaMask("Bloomfog Alpha Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        BlendOp Max
        Blend One One, Zero Zero
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile REINHARD_TONE_MAPPING

            #include "../CGIncludes/CustomTonemapping.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // UV1/UV2 is used for HDR color data (and also allows for easy interpolation)
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            uniform float4x4 _VertexTransformMatrix;

            sampler2D _BloomfogAlphaMask;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(_VertexTransformMatrix, v.vertex);
                o.uv = v.uv;
                o.uv1 = v.uv1;
                o.uv2 = v.uv2;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Construct HDR color from UV1 and UV2
                float4 color = float4(i.uv1.xy, i.uv2.xy);

                // Apply alpha mask
                color.rgb *= tex2D(_BloomfogAlphaMask, i.uv).a;
                color.rgb *= color.a;

                // Yeah this should be fine.
                return color;
            }
            ENDCG
        }
    }
}
