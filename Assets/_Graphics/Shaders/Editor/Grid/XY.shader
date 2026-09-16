Shader "ChroMapper/Editor/Grid/XY"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        
        _GridSpacing("Grid Spacing", Vector) = (1, 0.25, 0.125, 0.0625)
        _GridThickness("Grid Thickness", Vector) = (0.1, 0.05, 0.025, 0.0125)
        _GridOffset("Grid Offset", Vector) = (0, 0, 0, 0)
        _GridScale("Grid Scale", Range(0, 2)) = 1
    }
    SubShader
    {
        // GridLineCoverageAA blends line coverage over the lane surface: the old opaque pass wrote
        // binary pixels whose only smoothing was post-process AA. Zero Zero writes the zero bloom
        // mask the old `color.a = 0` produced, and clip() keeps off-line pixels from writing it.
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha, Zero Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../../ShaderLibrary/GridCoverage.hlsl"

            uniform float _Rotation = 0;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridSpacing)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridThickness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _GridScale)
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

                float xPos = i.rotatedPos.x + gridOffset.x;
                float yPos = i.rotatedPos.y + gridOffset.y;
                float xFilter = fwidth(xPos);
                float yFilter = fwidth(yPos);

                // Grid: spacing 0 marks an unused level (GridRenderingController emits it), and the
                // old mod-by-zero never matched; skipping keeps NaN out of the coverage math.
                float coverage = 0;
                for (int idx = 0; idx < 4; idx++)
                {
                    float spacing = gridSpacing[idx];
                    if (spacing <= 0) continue;
                    float halfWidth = spacing * gridThickness[idx] * 0.5;
                    coverage = max(coverage, GridLineCoverage(xPos, spacing, halfWidth, xFilter));
                    coverage = max(coverage, GridLineCoverage(yPos, spacing, halfWidth, yFilter));
                }

                clip(coverage - 0.004);
                return half4(color.rgb, coverage);
            }
            ENDHLSL
        }
    }
}