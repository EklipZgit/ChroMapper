Shader "ChroMapper/Object/Selectable"
{
    Properties
    {
        _Color("Main Color", Color) = (0.5,0.5,0.5,1)

        _FirstOutlineColor("Outline color", Color) = (1,0,0,0.5)
        _FirstOutlineWidth("Outlines width", Range(0.0, 2.0)) = 0.15

        _Angle("Switch shader on angle", Range(0.0, 180.0)) = 89
    }

    HLSLINCLUDE
    #include "UnityCG.cginc"

    uniform float4 _FirstOutlineColor;
    uniform float _FirstOutlineWidth;

    uniform float4 _Color;
    uniform float _Angle;
    ENDHLSL

    SubShader
    {
        //Surface shader

        Pass
        {
            Tags
            {
                "Queue"="Transparent"
                "IgnoreProjector"="True"
                "RenderType"="Transparent"
            }
            ZWrite On
            Blend SrcColor OneMinusSrcColor
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return _Color;
            }
            ENDHLSL
        }

        //First outline
        Pass
        {
            Tags
            {
                "Queue"="Transparent"
                "IgnoreProjector"="True"
                "RenderType"="Transparent"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float dist : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                float3 scaleDir = normalize(v.vertex.xyz - float4(0, 0, 0, 1));
                //scaleDir = float3(scaleDir.x, 0.0175, scaleDir.z);
                float3 originalPos = UnityObjectToClipPos(v.vertex).xyz;
                //This shader consists of 2 ways of generating outline that are dynamically switched based on demiliter angle
                //If vertex normal is pointed away from object origin then custom outline generation is used (based on scaling along the origin-vertex vector)
                //Otherwise the old-school normal vector scaling is used
                //This way prevents weird artifacts from being created when using either of the methods
                if (degrees(acos(dot(scaleDir.xyz, v.normal.xyz))) > _Angle)
                {
                    v.vertex.xyz += normalize(v.normal.xyz) * _FirstOutlineWidth;
                }
                else
                {
                    v.vertex.xyz += scaleDir * _FirstOutlineWidth;
                }
                v.vertex.xyz = float3(v.vertex.xyz.x, v.vertex.xyz.y, v.vertex.xyz.z);
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dist = distance(originalPos, o.pos);
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                return _FirstOutlineColor;
            }
            ENDHLSL
        }


    }
    Fallback "Diffuse"
}