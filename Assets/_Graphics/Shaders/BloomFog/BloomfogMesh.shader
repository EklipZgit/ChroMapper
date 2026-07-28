Shader "ChroMapper/BloomfogMesh"
{
    Properties
    {
        _BloomfogAlphaMask("Bloomfog Alpha Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent"
        }
        ZWrite Off
        Cull Off
        BlendOp Max
        Blend One One, Zero Zero
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                float3 tangent : TANGENT;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 tangent : TANGENT;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            uniform float4x4 _VertexTransformMatrix;

            sampler2D _BloomfogAlphaMask;

            v2f vert(appdata v)
            {
                // Constant view matrix, so it lives here
                float4x4 ViewMatrix = float4x4(
                    2, 0, 0, 0,
                    0, -2, 0, 0,
                    0, 0, -1, 0,
                    -1, 1, 0, 1
                );

                v2f o;
                o.vertex = mul(transpose(ViewMatrix), float4(v.vertex, 1.0));
                o.uv = v.uv;

                float4 color = v.color;
                color.rgb = GammaToLinearSpace(color.rgb);
                o.color = color;

                o.tangent.xyz = v.tangent / v.tangent.z;
                o.tangent.w = 1.0 / v.tangent.z;
                return o;
            }

            // im NGL a lot of this came from Owen's decompilation help
            // this is unreadable as fuck but it is 1:1 equivalent to Beat Saber's bloomfog mesh
            float4 frag(v2f i) : SV_Target
            {
                float3 dir = i.tangent.xyz / i.tangent.w;

                float dir2 = dot(dir.xyz, dir.xyz);

                float alpha = max(i.color.a, 1);
                alpha = 1 / alpha;

                float u0 = dir2 * alpha - 10;

                u0 = max(u0, 0);

                u0 = u0 * 0.01 + 1.0;

                u0 = 1.0 / u0;

                float2 uv = float2(i.uv.x / i.uv.z, i.uv.y);

                // sample generated here, but sample_indexable in DXBC
                // this will be functionally equivalent
                float4 line_mask = tex2D(_BloomfogAlphaMask, uv);

                float a2 = i.color.a * i.color.a;

                float4 color = float4(i.color.rgb, a2);

                float4 bloom_color = line_mask * color;

                float bloom_alpha = u0 * bloom_color.a;

                return float4(bloom_color.rgb * bloom_alpha, bloom_alpha);
            }
            ENDHLSL
        }
    }
}