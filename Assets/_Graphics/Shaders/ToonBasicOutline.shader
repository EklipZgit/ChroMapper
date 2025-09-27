//Shader courtesy of Unity
Shader "ChroMapper/Toon Outline Basic"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _Outline ("Outline width", Range (.002, 0.05)) = .005
    }
    SubShader
    {
        Tags
        {
            "Queue"="Geometry" "RenderType"="Opaque"
        }

        Cull Front
        ZWrite Off
        ColorMask RGB
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "OUTLINE"

            HLSLPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            CBUFFER_START(UnityPerMaterial)
                float _Outline;
                float4 _OutlineColor;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half fogCoord : TEXCOORD0;
                half4 color : COLOR;
            };

            v2f vert(appdata i)
            {
                v2f o = (v2f)0;

                if (_Outline > 0.01)
                {
                    i.vertex.xyz += i.vertex * _Outline;
                    o.vertex = UnityObjectToClipPos(i.vertex);
                    o.color = _OutlineColor;
                    // output.fogCoord = ComputeFogFactor(output.positionCS.z);
                }
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                if (_Outline <= 0.01) clip(-1);
                // i.color.rgb = MixFog(i.color.rgb, i.fogCoord);
                return i.color;
            }
            ENDHLSL
        }
    }
}