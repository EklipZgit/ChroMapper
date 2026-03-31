Shader "Custom/Stencil" {
    Properties {
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader {
        Tags {
            "Queue" = "Geometry-1"
            "RenderType" = "Opaque"
        }
        Pass {
            Name ""
            Blend Zero One, Zero One
            ZClip On
            ZWrite Off
            Cull Off

            Stencil {
                Ref [_StencilRefValue]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 worldPos;
                worldPos  = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
                worldPos += unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx;
                worldPos += unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz;
                worldPos += unity_ObjectToWorld._m03_m13_m23_m33;

                float4 clipPos;
                clipPos  = worldPos.yyyy * unity_MatrixVP._m01_m11_m21_m31;
                clipPos += unity_MatrixVP._m00_m10_m20_m30 * worldPos.xxxx;
                clipPos += unity_MatrixVP._m02_m12_m22_m32 * worldPos.zzzz;
                clipPos += unity_MatrixVP._m03_m13_m23_m33 * worldPos.wwww;

                o.position = clipPos;
                return o;
            }

            struct fout
            {
                float4 sv_target : SV_Target;
            };

            fout frag(v2f inp)
            {
                fout o;
                o.sv_target = float4(0.0, 0.0, 0.0, 0.0);
                return o;
            }
            ENDCG
        }
    }
}
