// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ChroMapper/Object/Event"
{
    Properties
    {
        _ColorTint("Color Tint", Color) = (1, 0, 0, 0)
        _Color("Base Color", Color) = (0, 0, 0, 0)
        _Position("Point Position", Vector) = (0, 0, 0, 0)
        _CircleRadius("Spotlight Size", float) = 0.2
        _FadeSize("Fade Size", float) = 0.5
        _MainAlpha("Base Alpha", float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/CustomTonemapping.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Position)
                UNITY_DEFINE_INSTANCED_PROP(float, _CircleRadius)
                UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
                UNITY_DEFINE_INSTANCED_PROP(float, _MainAlpha)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : POSITION0; // clip space position
                float4 vertex_Object : POSITION1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex_Object = v.vertex;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 position = UNITY_ACCESS_INSTANCED_PROP(Props, _Position);
                half4 colorTint = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorTint);
                half4 colorBase = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                half circleRadius = UNITY_ACCESS_INSTANCED_PROP(Props, _CircleRadius);
                half fadeSize = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize);
                half mainAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _MainAlpha);

                half distance = abs(i.vertex_Object.z - position.z);

                half t = (distance - circleRadius) / fadeSize;

                if (distance < circleRadius + fadeSize && distance > circleRadius)
                {
                    half4 transitionColor = lerp(colorTint, colorBase, t);

                    transitionColor.a = 0;
                    ACES_TONE_MAPPING_APPLY(transitionColor);

                    return transitionColor;
                }

                if (distance > circleRadius + fadeSize)
                {
                    colorBase.a = 0;
                    ACES_TONE_MAPPING_APPLY(colorBase);
                    return colorBase;
                }

                colorTint.a = 0;
                ACES_TONE_MAPPING_APPLY(colorBase);
                return colorTint;
            }
            ENDHLSL
        }
    }
}