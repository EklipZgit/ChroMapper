Shader "ChroMapper/Editor/Grid/XY"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        
        _GridSpacing("Grid Spacing", Vector) = (1, 0.25, 0.125, 0.0625)
        _GridThickness("Grid Thickness", Vector) = (0.1, 0.05, 0.025, 0.0125)
        _GridOffset("Grid Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Cull Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            uniform float _Rotation = 0;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridSpacing)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridThickness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridOffset)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float4 rotatedPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = _Rotation * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = o.worldPos.x * cos(rotationInRadians) - o.worldPos.z * sin(
                    rotationInRadians);
                float newZ = o.worldPos.z * cos(rotationInRadians) + o.worldPos.x * sin(
                    rotationInRadians);

                o.rotatedPos = float4(newX, o.worldPos.y, newZ, o.worldPos.w);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 gridSpacing = UNITY_ACCESS_INSTANCED_PROP(Props, _GridSpacing);
                float4 gridThickness = UNITY_ACCESS_INSTANCED_PROP(Props, _GridThickness);
                float4 gridOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _GridOffset);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                color.a = 0;

                float xPos = i.rotatedPos.x + gridOffset.x;
                float yPos = i.rotatedPos.y + gridOffset.y;

                // Grid
                for (int idx = 0; idx < 4; idx++)
                {
                    if (abs(xPos) % gridSpacing[idx] / gridSpacing[idx] <= gridThickness[idx] / 2 ||
                        abs(xPos) % gridSpacing[idx] / gridSpacing[idx] >= 1 - gridThickness[idx] / 2 ||
                        abs(yPos) % gridSpacing[idx] / gridSpacing[idx] <= gridThickness[idx] / 2 ||
                        abs(yPos) % gridSpacing[idx] / gridSpacing[idx] >= 1 - gridThickness[idx] / 2)
                    {
                        return color;
                    }
                }

                // why it needs to return anyway idk, compiler complained
                if (!color.a) discard;
                return color;
            }
            ENDHLSL
        }
    }
}