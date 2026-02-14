Shader "ChroMapper/Editor/Grid/XZ"
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

            uniform float _SongBPM = 120;
            uniform float _BPMChange_Times[170];
            uniform float _BPMChange_Json_Times[170];
            uniform float _BPMChange_BPMs[170];
            uniform int _BPMChange_Count;
            uniform float _Offset = 0;
            uniform float _Rotation = 0;
            uniform float _EditorScale = 4;
            uniform float _CurrentHJD = 2;
            uniform int _DisplayHJDLine = 1;

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
                float newX = o.worldPos.x * cos(rotationInRadians) - o.worldPos.z * sin(rotationInRadians);
                float newZ = o.worldPos.z * cos(rotationInRadians) + o.worldPos.x * sin(rotationInRadians);

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

                float editorScaleMult = _EditorScale / 4;

                //WHERE'S THE LAMB SAUCE (unedited beat time)
                float timeButRAWWW = (i.rotatedPos.z + _Offset) / _EditorScale;

                //To plugerino into shader after dealing with BPM Changes
                float time = timeButRAWWW;
                if (_BPMChange_Count > 1)
                {
                    time = 0;
                    for (int bpmIdx = 0; bpmIdx < _BPMChange_Count - 1; bpmIdx++)
                    {
                        float currBpmTime = _BPMChange_Times[bpmIdx];
                        float nextBpmTime = _BPMChange_Times[bpmIdx + 1];
                        if (timeButRAWWW < 0) //Check for negative beats
                        {
                            time = timeButRAWWW;
                            break;
                        }
                        if (currBpmTime <= timeButRAWWW && timeButRAWWW < nextBpmTime)
                        {
                            float difference = timeButRAWWW - currBpmTime;
                            float timeInSecond = 60 / _SongBPM * difference;
                            float timeInNewBeat = _BPMChange_BPMs[bpmIdx] / 60 * timeInSecond;
                            time = timeInNewBeat + _BPMChange_Json_Times[bpmIdx];
                        }
                    }
                    float lastBpmTime = _BPMChange_Times[_BPMChange_Count - 1];
                    if (lastBpmTime < timeButRAWWW)
                    {
                        float difference = timeButRAWWW - lastBpmTime;
                        float timeInSecond = 60 / _SongBPM * difference;
                        float timeInNewBeat = _BPMChange_BPMs[_BPMChange_Count - 1] / 60 * timeInSecond;
                        time = timeInNewBeat + _BPMChange_Json_Times[_BPMChange_Count - 1];
                    }
                }

                // HJD line
                float timeOffsetToCursor = timeButRAWWW - _Offset / _EditorScale;
                float hjdRange = gridThickness / 10;
                if (_DisplayHJDLine && _CurrentHJD - hjdRange < timeOffsetToCursor && timeOffsetToCursor < _CurrentHJD +
                    hjdRange)
                {
                    return half4(0.5, 0, 0, 0);
                }

                // Sub-beat
                for (int idx = 0; idx < 4; idx++)
                {
                    if (abs(time * editorScaleMult) % gridSpacing[idx] / gridSpacing[idx] <= gridThickness[idx] / 2 ||
                        abs(time * editorScaleMult) % gridSpacing[idx] / gridSpacing[idx] >= 1 - gridThickness[idx] / 2)
                    {
                        return color;
                    }
                }

                float xPos = i.rotatedPos.x + gridOffset.x;

                // Lane line
                if (abs(xPos) % 1.0 / 1.0 <= 0.1 / 2 ||
                    abs(xPos) % 1.0 / 1.0 >= 1 - 0.1 / 2)
                {
                    return color;
                }

                discard;
                // why it needs to return anyway idk, compiler complained
                return color;
            }
            ENDHLSL
        }
    }
}