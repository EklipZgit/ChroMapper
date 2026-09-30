Shader "ChroMapper/Object/Basic Gradient"
{
    Properties
    {
        _ColorA("Color A", Vector) = (1.0, 0.0, 0.0, 1.0)
        _ColorB("Color B", Vector) = (1.0, 0.0, 0.0, 1.0)
        _StrobeColorA("Strobe Color A", Vector) = (1.0, 0.0, 0.0, 1.0)
        _StrobeColorB("Strobe Color B", Vector) = (1.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _LightDistributionTex("Light Distribution", 2D) = "black" {}
        _LightDistributionWidth("Light Distribution Width", Float) = 0
        _UseLightDistribution("Use Light Distribution", Float) = 0
        _UseLightTimeline("Use Light Timeline", Float) = 0
        _LightTimelineDuration("Light Timeline Duration", Float) = 1
        _LightTimelineStart("Light Timeline Start", Float) = 0
        // The mesh must cover pixel centers outside the authored boundary so
        // the fragment shader can finish the one-pixel coverage ramp.
        _RibbonEdgePadding("Ribbon Edge Padding", Float) = 0
        // Shared lane coordinates prevent adjacent owners disagreeing at an edge.
        _UseRibbonPlane("Use Ribbon Plane", Float) = 0
        _RibbonPlaneOrigin("Ribbon Plane Origin", Vector) = (0,0,0,0)
        _RibbonPlaneTime("Ribbon Plane Time", Vector) = (0,0,1,0)
        _RibbonPlaneWidth("Ribbon Plane Width", Vector) = (1,0,0,0)
        _StrobeDuration("Strobe Duration", Float) = 1
        _StrobeFade("Strobe Fade", Float) = 0
        _StrobeFrequencyA("Strobe Frequency A", Float) = 0
        _StrobeFrequencyB("Strobe Frequency B", Float) = 0
        _UseStrobeColors("Use Strobe Colors", Float) = 0
        _EasingID("Easing ID", Int) = 0
        _UseHSV("Use HSV", Int) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }
        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcColor OneMinusSrcColor

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Easings.hlsl"

            sampler2D _LightDistributionTex;
            float4 _LightDistributionTex_TexelSize;
            // Ribbon lanes use the editor's common scrolling beat coordinate.
            float _Rotation;
            float4 _SongBpmTime;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorB)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColorB)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeDuration)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFade)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFrequencyA)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFrequencyB)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseStrobeColors)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseLightDistribution)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightDistributionWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseLightTimeline)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightTimelineDuration)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightTimelineStart)
                UNITY_DEFINE_INSTANCED_PROP(float, _RibbonEdgePadding)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseRibbonPlane)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RibbonPlaneOrigin)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RibbonPlaneTime)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RibbonPlaneWidth)
                UNITY_DEFINE_INSTANCED_PROP(int, _EasingID)
                UNITY_DEFINE_INSTANCED_PROP(int, _UseHSV)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                // One common plane-to-screen inverse for every owner in a lane
                // avoids independently interpolated UV ownership disagreements.
                nointerpolation float3 laneTime : TEXCOORD1;
                nointerpolation float3 laneWidth : TEXCOORD2;
                nointerpolation float3 laneDenominator : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Preserve separate scalar-HSV and shortest-hue interpolation modes.
            float3 RGBToHSV(float3 color)
            {
                const float epsilon = 1e-10f;
                const float4 constants = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
                float4 p = lerp(float4(color.bg, constants.wz), float4(color.gb, constants.xy), step(color.b, color.g));
                float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
                float chroma = q.x - min(q.w, q.y);
                return float3(abs(q.z + ((q.w - q.y) / ((6.0f * chroma) + epsilon))), chroma / (q.x + epsilon), q.x);
            }

            float3 HSVToRGB(float3 color)
            {
                float3 rgb = abs((frac(color.xxx + float3(0.0f, 2.0f / 3.0f, 1.0f / 3.0f)) * 6.0f) - 3.0f);
                return color.z * lerp(1.0f, saturate(rgb - 1.0f), color.y);
            }

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // GreenMonstercatRibbonEdgesMatchSupersampling: a fragment AA
                // ramp cannot shade pixel centers clipped by the mesh edge.
                // Extend the existing quad by one screen pixel along both axes
                // and extrapolate UVs, keeping the authored boundary unchanged.
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonEdgePadding) > 0.5f)
                {
                    // RandomizedMonstercatRibbonEdgesMatchSupersampling: lane
                    // layout supplies one common plane; live rotation and scroll
                    // do not require per-frame CPU material updates.
                    float sine, cosine;
                    sincos(radians(_Rotation), sine, cosine);
                    float3x3 rotation = float3x3(cosine, 0, sine, 0, 1, 0, -sine, 0, cosine);
                    float3 widthAxis = mul(rotation,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonPlaneWidth).xyz);
                    float3 timeAxis = mul(rotation,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonPlaneTime).xyz);
                    float3 origin = mul(rotation,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonPlaneOrigin).xyz);
                    float3 projectedOrigin = mul(UNITY_MATRIX_VP, float4(origin, 1.0f)).xyw;
                    float3 projectedTime = mul(UNITY_MATRIX_VP, float4(timeAxis, 0)).xyw;
                    float3 projectedWidthAxis = mul(UNITY_MATRIX_VP, float4(widthAxis, 0)).xyw;
                    o.laneTime = cross(projectedWidthAxis, projectedOrigin);
                    o.laneWidth = cross(projectedOrigin, projectedTime);
                    o.laneDenominator = cross(projectedTime, projectedWidthAxis);
                    float4 along = UnityObjectToClipPos(v.vertex + float4(1, 0, 0, 0));
                    float4 across = UnityObjectToClipPos(v.vertex + float4(0, 1, 0, 0));
                    float2 position = o.vertex.xy / o.vertex.w;
                    float2 tangent = (along.xy / along.w - position) * _ScreenParams.xy;
                    float2 normal = normalize(float2(-tangent.y, tangent.x));
                    float projectedWidth = abs(dot((across.xy / across.w - position)
                        * _ScreenParams.xy * 0.5f, normal));
                    float padding = (v.uv.y > 0.5f ? 1.0f : -1.0f)
                        / max(projectedWidth, 1e-4f);
                    // GreenMonstercatRibbonEdgesMatchSupersampling: the next
                    // owner must rasterize the shared temporal pixel as well.
                    float2 widthTangent = (across.xy / across.w - position) * _ScreenParams.xy;
                    float2 timeNormal = normalize(float2(-widthTangent.y, widthTangent.x));
                    float projectedLength = abs(dot(tangent * 0.5f, timeNormal));
                    float timePadding = (v.uv.x > 0.5f ? 1.0f : -1.0f)
                        / max(projectedLength, 1e-4f);
                    o.vertex = UnityObjectToClipPos(v.vertex + float4(timePadding, padding, 0, 0));
                    o.uv.x += timePadding;
                    o.uv.y += padding;
                }

                return o;
            }

            float EvaluateRibbonEase(float t, int id)
            {
                // BasicGradientDispatchMatchesEasingShaderId: these IDs match
                // the CPU easing dispatch, including Beat Saber's extra curves.
                switch (id)
                {
                case 1:
                    t = Quadratic_In(t);
                    break;
                case 2:
                    t = Quadratic_Out(t);
                    break;
                case 3:
                    t = Quadratic_InOut(t);
                    break;
                case 4:
                    t = Cubic_In(t);
                    break;
                case 5:
                    t = Cubic_Out(t);
                    break;
                case 6:
                    t = Cubic_InOut(t);
                    break;
                case 7:
                    t = Quartic_In(t);
                    break;
                case 8:
                    t = Quartic_Out(t);
                    break;
                case 9:
                    t = Quartic_InOut(t);
                    break;
                case 10:
                    t = Quintic_In(t);
                    break;
                case 11:
                    t = Quintic_Out(t);
                    break;
                case 12:
                    t = Quintic_InOut(t);
                    break;
                case 13:
                    t = Sinusoidal_In(t);
                    break;
                case 14:
                    t = Sinusoidal_Out(t);
                    break;
                case 15:
                    t = Sinusoidal_InOut(t);
                    break;
                case 16:
                    t = Exponential_In(t);
                    break;
                case 17:
                    t = Exponential_Out(t);
                    break;
                case 18:
                    t = Exponential_InOut(t);
                    break;
                case 19:
                    t = Circular_In(t);
                    break;
                case 20:
                    t = Circular_Out(t);
                    break;
                case 21:
                    t = Circular_InOut(t);
                    break;
                case 22:
                    t = Back_In(t);
                    break;
                case 23:
                    t = Back_Out(t);
                    break;
                case 24:
                    t = Back_InOut(t);
                    break;
                case 25:
                    t = Elastic_In(t);
                    break;
                case 26:
                    t = Elastic_Out(t);
                    break;
                case 27:
                    t = Elastic_InOut(t);
                    break;
                case 28:
                    t = Bounce_In(t);
                    break;
                case 29:
                    t = Bounce_Out(t);
                    break;
                case 30:
                    t = Bounce_InOut(t);
                    break;
                case 31:
                    t = Step(t);
                    break;
                case 32:
                    t = BeatSaberInOutBack(t);
                    break;
                case 33:
                    t = BeatSaberInOutElastic(t);
                    break;
                case 34:
                    t = BeatSaberInOutBounce(t);
                    break;
                default:
                    break;
                }

                return t;
            }

            // Match BasicEventColorLerp: mode 1 preserves legacy scalar HSV and mode 2 uses shortest-path trueHSV.
            float4 InterpolateRibbonColor(float4 startColor, float4 endColor, float t, int colorLerpType)
            {
                float4 color;
                if (colorLerpType != 0)
                {
                    float4 startHsv = float4(RGBToHSV(startColor.rgb), startColor.a);
                    float4 endHsv = float4(RGBToHSV(endColor.rgb), endColor.a);
                    float hue;
                    if (colorLerpType == 1)
                    {
                        // Existing HSV data linearly interpolates normalized hue even when that takes the long arc through green.
                        hue = lerp(startHsv.x, endHsv.x, t);
                    }
                    else
                    {
                        // trueHSV mirrors Mathf.LerpAngle's shortest signed delta and hue-only easing clamp.
                        float hueDelta = frac(endHsv.x - startHsv.x);
                        if (hueDelta > 0.5f) hueDelta -= 1.0f;
                        hue = frac(startHsv.x + (hueDelta * saturate(t)));
                    }

                    float3 hsv = float3(
                        hue,
                        lerp(startHsv.y, endHsv.y, t),
                        lerp(startHsv.z, endHsv.z, t));
                    color = float4(HSVToRGB(hsv), lerp(startHsv.a, endHsv.a, t));
                }
                else
                {
                    color = lerp(startColor, endColor, t);
                }

                return color;
            }

            float4 InterpolatePhysicalRibbonColor(
                float4 startColor,
                float4 endColor,
                float t,
                int colorLerpType,
                bool preserveConstantOutputPeak)
            {
                float4 color = InterpolateRibbonColor(startColor, endColor, t, colorLerpType);
                float startPeak = max(startColor.r, max(startColor.g, startColor.b));
                float endPeak = max(endColor.r, max(endColor.g, endColor.b));
                // Preserve true easing overshoot and genuine brightness changes.
                // Only compensate the bloom-less strip when equal-brightness endpoints should retain equal visual energy.
                if (preserveConstantOutputPeak
                    && colorLerpType == 0
                    && t >= 0.0f
                    && t <= 1.0f
                    && abs(startColor.a - endColor.a) <= 0.00001f
                    && abs(startPeak - endPeak) <= 0.00001f)
                {
                    float targetPeak = lerp(startPeak, endPeak, t);
                    float mixedPeak = max(color.r, max(color.g, color.b));
                    if (mixedPeak > 0.00001f)
                    {
                        color.rgb *= targetPeak / mixedPeak;
                    }
                }

                return color;
            }

            // Keep ribbon brightness and overbright color response independently tunable.
            static const float RibbonAlphaAtLightLevel100 = 0.6f;
            static const float RibbonHalfWhiteLightLevel = 4.0f;
            static const float RibbonMaximumWhiteBlend = 0.85f;

            float AsymptoticRibbonAlpha(float lightLevel)
            {
                lightLevel = max(lightLevel, 0.0f);
                float scale = (1.0f - RibbonAlphaAtLightLevel100) / RibbonAlphaAtLightLevel100;
                return lightLevel / (lightLevel + scale);
            }

            float4 DisplayRibbonColor(float4 color)
            {
                float lightLevel = max(color.a, 0.0f);
                float ribbonAlpha = AsymptoticRibbonAlpha(color.a);
                // Represent excess light as a bounded white drift instead of additive clipping.
                float colorPeak = max(color.r, max(color.g, color.b));
                // Clamp negative easing overshoot before display compensation.
                float3 normalizedColor = max(color.rgb / max(colorPeak, 1.0f), 0.0f);
                // Measure overbright above level 100, making level 400 three units above baseline.
                float overbright = max(lightLevel - 1.0f, 0.0f);
                // Derive the scale so the half-white point stays fixed when the cap changes.
                float halfWhiteOverbright = RibbonHalfWhiteLightLevel - 1.0f;
                float whiteCurveScaleSquared = (halfWhiteOverbright * halfWhiteOverbright)
                    * ((RibbonMaximumWhiteBlend / 0.5f) - 1.0f);
                float whiteMix = RibbonMaximumWhiteBlend * (overbright * overbright)
                    / ((overbright * overbright) + whiteCurveScaleSquared);
                color.rgb = lerp(normalizedColor, 1.0f, whiteMix) * ribbonAlpha;
                color.a = 0;
                return color;
            }

            float4 DisplayLightStripColor(float4 color)
            {
                color = DisplayRibbonColor(color);
#ifndef UNITY_COLORSPACE_GAMMA
                color.rgb = float3(
                    GammaToLinearSpaceExact(color.r),
                    GammaToLinearSpaceExact(color.g),
                    GammaToLinearSpaceExact(color.b));
#endif
                color.rgb = sqrt(color.rgb);
                return color;
            }

            // RandomizedMonstercatRibbonEdgesMatchSupersampling: primary tween
            // rows stay direct; sorted neighbor tweens follow the row-9 count.
            float4 TimelineRow(float coordinate, float row)
            {
                return tex2Dlod(_LightDistributionTex,
                    float4(coordinate, (row + 0.5f) * _LightDistributionTex_TexelSize.y, 0.0f, 0.0f));
            }

            float EvaluateStrobeCycles(float startFrequency, float endFrequency, float duration, float progress, bool fade)
            {
                float cycles = duration * ((startFrequency * progress)
                    + (0.5f * (endFrequency - startFrequency) * progress * progress));
                if (fade && startFrequency <= 0.0f && endFrequency > 0.0f)
                    cycles -= (startFrequency + endFrequency) * duration * 0.5f;
                return cycles;
            }

            float StrobeDutyIntegral(float cycles)
            {
                return floor(cycles) * 0.5f + max(frac(cycles) - 0.5f, 0.0f);
            }

            float StrobeDutyCoverage(float cycles, float cycleFootprint)
            {
                if (cycleFootprint <= 1e-4f)
                    return step(0.5f, frac(cycles));
                float halfFootprint = cycleFootprint * 0.5f;
                return saturate((StrobeDutyIntegral(cycles + halfFootprint)
                    - StrobeDutyIntegral(cycles - halfFootprint)) / cycleFootprint);
            }

            float4 PresentHardStrobe(float4 normalColor, float4 strobeColor, float mix)
            {
                if (mix <= 0.0f)
                    return DisplayLightStripColor(normalColor);
                if (mix >= 1.0f)
                    return DisplayLightStripColor(strobeColor);
                float4 normalPresented = DisplayLightStripColor(normalColor);
                float4 strobePresented = DisplayLightStripColor(strobeColor);
                return float4(sqrt(lerp(normalPresented.rgb * normalPresented.rgb,
                    strobePresented.rgb * strobePresented.rgb, mix)), 0.0f);
            }

            // Neighbor records use the same tween encoding as the owned record;
            // which draw owns their pixels is resolved by PresentLightTimeline.
            float4 EvaluateLightTimeline(
                float coordinate, float time, float pixelWidth, float bankOffset,
                out float coverage,
                out float4 hardStrobeColor, out float hardStrobeMix)
            {
                float4 times = TimelineRow(coordinate, bankOffset + 4);
                // Compare all owners against the same absolute boundary values;
                // relative texture clocks preserve precision within each tween.
                times += UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                coverage = 0.0f;
                hardStrobeColor = 0.0f;
                hardStrobeMix = -1.0f;
                // An invalid interval owns no pixels, even if endpoint RGB is set.
                if (times.y <= times.x)
                    return 0.0f;
                // DistributedNodeRibbonJoinDoesNotExposeBackground: the fractional row-7 flags
                // keep each lit owner covered through its shared endpoint instead of fading both to black.
                float4 flags = TimelineRow(coordinate, bankOffset + 7);
                float joins = frac(flags.z);
                bool joinedStart = joins >= 0.25f && (joins < 0.5f || joins >= 0.75f);
                bool joinedEnd = joins >= 0.5f;
                if (pixelWidth <= 1e-7f)
                {
                    coverage = time >= times.x && time <= times.y ? 1.0f : 0.0f;
                }
                else
                {
                    float halfPixel = pixelWidth * 0.5f;
                    // AlternatingMonstercatRibbonEdgesMatchSupersampling: adding
                    // subpixel widths to beat 410 rounds them to the beat's ULP
                    // and dims even fully covered pixels. Integrate distances
                    // from the pixel center, keeping the footprint near zero.
                    coverage = saturate((min(halfPixel, times.y - time)
                        - max(-halfPixel, times.x - time)) / pixelWidth);
                    // BrightDistributedNodeRibbonJoinMatchesAdjacentPixels and
                    // DistributedNodeRibbonJoinDoesNotExposeBackground: the next owner alone
                    // supplies shared pixels, including rounding at the common timestamp.
                    if (joinedStart && abs(time - times.x) <= halfPixel)
                        coverage = time >= times.x ? 1.0f : 0.0f;
                    else if (joinedEnd && abs(time - times.y) <= halfPixel)
                        coverage = time < times.y ? 1.0f : 0.0f;
                }
                if (coverage <= 0.0f)
                    return 0.0f;
                time = clamp(time, times.x, times.y);
                float4 rates = TimelineRow(coordinate, bankOffset + 5);
                float4 brightness = TimelineRow(coordinate, bankOffset + 6);
                float4 easings = TimelineRow(coordinate, bankOffset + 8);
                float4 normalFrom = TimelineRow(coordinate, bankOffset);
                float4 normalTo = TimelineRow(coordinate, bankOffset + 1);
                float normalizedAlpha = saturate((time - times.x) / (times.y - times.x));
                float normalizedColor = times.w == times.z ? 0.0f : saturate((time - times.z) / (times.w - times.z));
                float alphaProgress = EvaluateRibbonEase(normalizedAlpha, (int)easings.x);
                bool composedEndpoints = flags.w >= 2.0f;
                if (!composedEndpoints)
                {
                    normalFrom.a = brightness.z;
                    normalTo.a = brightness.w;
                }
                bool preserveNormalPeak = abs((normalFrom.a * rates.z) - (normalTo.a * rates.w)) <= 0.00001f;
                float4 color = InterpolatePhysicalRibbonColor(normalFrom, normalTo,
                    EvaluateRibbonEase(normalizedColor, (int)easings.y), (int)flags.z, preserveNormalPeak);
                if (!composedEndpoints)
                    color.a *= lerp(rates.z, rates.w, alphaProgress);
                if (rates.x > 0.0f || rates.y > 0.0f)
                {
                    float4 strobeFrom = TimelineRow(coordinate, bankOffset + 2);
                    float4 strobeTo = TimelineRow(coordinate, bankOffset + 3);
                    strobeFrom.a = flags.x;
                    strobeTo.a = flags.y;
                    bool preserveStrobePeak = abs((flags.x * brightness.x) - (flags.y * brightness.y)) <= 0.00001f;
                    float4 strobe = InterpolatePhysicalRibbonColor(strobeFrom, strobeTo,
                        EvaluateRibbonEase(normalizedColor, (int)easings.z), (int)flags.z, preserveStrobePeak);
                    strobe.a = lerp(flags.x * brightness.x, flags.y * brightness.y, alphaProgress);
                    float duration = times.y - times.x;
                    bool fadeEnabled = fmod(flags.w, 2.0f) >= 1.0f;
                    float cycles = EvaluateStrobeCycles(rates.x, rates.y, duration, normalizedAlpha, fadeEnabled);
                    float phase = frac(cycles);
                    if (fadeEnabled)
                    {
                        float fade = EvaluateRibbonEase(1.0f - abs((phase * 2.0f) - 1.0f), (int)easings.w);
                        color = InterpolateRibbonColor(color, strobe, fade, (int)flags.z);
                    }
                    else
                    {
                        hardStrobeColor = strobe;
                        hardStrobeMix = StrobeDutyCoverage(
                            cycles, abs(lerp(rates.x, rates.y, normalizedAlpha)) * pixelWidth);
                    }
                }
                return color;
            }

            // AlternatingStrobeRibbonsBlendColorsAtSharedStripEdges: only a
            // neighbor-bank lookup borrows another draw's color. The owned bank
            // needs no foreign-color flag or permission check.
            float4 PresentLightTimeline(
                float coordinate, float time, float pixelWidth, bool sampleNeighbors,
                out bool foreignColumn)
            {
                foreignColumn = false;
                float coverage;
                float4 hardStrobeColor;
                float hardStrobeMix;
                float4 color = EvaluateLightTimeline(
                    coordinate, time, pixelWidth, 0.0f, coverage,
                    hardStrobeColor, hardStrobeMix);
                float4 presented = hardStrobeMix >= 0.0f
                    ? PresentHardStrobe(color, hardStrobeColor, hardStrobeMix)
                    : DisplayLightStripColor(color);
                presented *= sqrt(coverage);
                // RandomizedMonstercatRibbonEdgesMatchSupersampling: a delayed
                // neighbor may cross several nodes during this owner's span.
                // Search its sorted records only at a shared strip edge.
                if (sampleNeighbors && presented.r + presented.g + presented.b <= 0.0f)
                {
                    float packedFlags = frac(TimelineRow(coordinate, 7).z);
                    bool hasNeighbors = fmod(floor(packedFlags * 64.0f), 2.0f) >= 1.0f;
                    if (hasNeighbors)
                    {
                        int lower = 0;
                        int upper = (int)TimelineRow(coordinate, 9.0f).x;
                        [loop]
                        while (lower < upper)
                        {
                            int middle = (lower + upper) >> 1;
                            float start = TimelineRow(coordinate, 14.0f + 9.0f * middle).x
                                + UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                            if (start <= time)
                                lower = middle + 1;
                            else
                                upper = middle;
                        }
                        float sidecarCoverage;
                        float4 sidecarStrobeColor;
                        float sidecarStrobeMix;
                        float4 sidecarColor = EvaluateLightTimeline(
                            coordinate, time, pixelWidth, 10.0f + 9.0f * max(lower - 1, 0),
                            sidecarCoverage,
                            sidecarStrobeColor, sidecarStrobeMix);
                        float4 sidecarPresented = sidecarStrobeMix >= 0.0f
                            ? PresentHardStrobe(sidecarColor, sidecarStrobeColor, sidecarStrobeMix)
                            : DisplayLightStripColor(sidecarColor);
                        sidecarPresented *= sqrt(sidecarCoverage);
                        if (sidecarPresented.r + sidecarPresented.g + sidecarPresented.b > 0.0f)
                        {
                            presented = sidecarPresented;
                            foreignColumn = true;
                        }
                    }
                }
                return presented;
            }

            // SeparatedStripCoverageIsLinear: use derivatives computed before
            // divergent ownership branches, without clamping the UV first.
            void StripCoverage(float uvY, float width, float pixelFootprint,
                out float index, out float neighbour, out float blend)
            {
                float stripPos = saturate(uvY) * width;
                index = min(floor(stripPos), width - 1.0f);
                float halfFootprint = pixelFootprint * 0.5f;
                float coverageNext = saturate((stripPos + halfFootprint - (index + 1.0f)) / pixelFootprint);
                float coveragePrev = saturate((index - (stripPos - halfFootprint)) / pixelFootprint);
                // NarrowProjectedStripBlendsWithTheTouchedNeighbor: a widened
                // footprint can touch both sides; sample the side with more
                // coverage instead of always selecting the next strip.
                blend = max(coverageNext, coveragePrev);
                neighbour = clamp(index + (coverageNext > coveragePrev ? 1.0f : -1.0f),
                    0.0f, width - 1.0f);
                if (neighbour == index)
                    blend = 0.0f;
            }

            // NarrowProjectedStripBlendsWithTheTouchedNeighbor: a subpixel lit
            // strip can touch background on both sides of one pixel. Integrate
            // its full strip interval instead of fading against just one side.
            float StripIntervalCoverage(float uvY, float width, float footprint, float index)
            {
                float stripPos = saturate(uvY) * width;
                float halfFootprint = footprint * 0.5f;
                return saturate((min(stripPos + halfFootprint, index + 1.0f)
                    - max(stripPos - halfFootprint, index)) / footprint);
            }

            bool RibbonStripEmits(float4 presented)
            {
                return presented.r + presented.g + presented.b > 0.0f;
            }

            float4 BlendPresentedStrips(float4 presented, float4 neighbourPresented, float blend)
            {
                // SrcColor blending squares the source. Average the displayed
                // linear energy before taking its square root to avoid dark seams.
                return sqrt(lerp(presented * presented, neighbourPresented * neighbourPresented, blend));
            }

            float RibbonAxisCoverage(float uv, float pixelWidth)
            {
                if (pixelWidth <= 1e-7f)
                    return 1.0f;
                return saturate(0.5f + (min(uv, 1.0f - uv) / pixelWidth));
            }

            float4 ApplyRibbonEdgeCoverage(float4 presented, float2 uv, float2 pixelWidth, bool includeTimeEdge)
            {
                float coverage = RibbonAxisCoverage(uv.y, pixelWidth.y);
                if (includeTimeEdge)
                    coverage *= RibbonAxisCoverage(uv.x, pixelWidth.x);
                return presented * sqrt(coverage);
            }

            float4 PresentStrobeOverlay(
                float4 color, float4 startStrobeColor, float4 endStrobeColor, float t, float progress,
                float progressPixelWidth, int colorLerpType)
            {
                float4 strobeColor = lerp(startStrobeColor, endStrobeColor, t);
                float duration = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeDuration);
                float startFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyA);
                float endFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyB);
                bool fadeEnabled = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFade) > 0.5f;
                float cycles = EvaluateStrobeCycles(startFrequency, endFrequency, duration, progress, fadeEnabled);
                float phase = frac(cycles);
                float trianglePhase = 1.0f - abs((phase * 2.0f) - 1.0f);
                float strobeMix;
                [branch]
                if (fadeEnabled)
                {
                    strobeMix = Cubic_InOut(trianglePhase);
                    return DisplayLightStripColor(InterpolateRibbonColor(color, strobeColor, strobeMix, colorLerpType));
                }
                else
                {
                    strobeMix = StrobeDutyCoverage(
                        cycles,
                        abs(lerp(startFrequency, endFrequency, progress)) * duration * progressPixelWidth);
                    return PresentHardStrobe(color, strobeColor, strobeMix);
                }
            }

            float4 EvaluateDistributedStrip(
                float lightCoordinate, float t, float progress, float progressPixelWidth,
                int colorLerpType, float useStrobeColors)
            {
                float4 color = lerp(
                    tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.125f, 0.0f, 0.0f)),
                    tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.375f, 0.0f, 0.0f)),
                    t);
                if (useStrobeColors > 0.5f)
                {
                    return PresentStrobeOverlay(
                        color,
                        tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.625f, 0.0f, 0.0f)),
                        tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.875f, 0.0f, 0.0f)),
                        t,
                        progress,
                        progressPixelWidth,
                        colorLerpType);
                }

                return DisplayLightStripColor(color);
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // SeparatedStripCoverageIsLinear: derivatives must precede
                // per-pixel branches so every pixel uses the same footprint.
                float2 laneUv = i.uv;
                float timelineOffset = i.uv.x * UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineDuration);
                float timelineOrigin = UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _UseRibbonPlane) > 0.5f)
                {
                    float2 ndc = i.vertex.xy / _ScreenParams.xy * 2.0f - 1.0f;
                    #if UNITY_UV_STARTS_AT_TOP
                        ndc.y = -ndc.y;
                    #endif
                    float3 pixel = float3(ndc, 1.0f);
                    float inverseDenominator = rcp(dot(i.laneDenominator, pixel));
                    timelineOffset = dot(i.laneTime, pixel) * inverseDenominator;
                    timelineOrigin = _SongBpmTime.y;
                    laneUv.y = dot(i.laneWidth, pixel) * inverseDenominator;
                }
                float2 uvPixelWidth = fwidth(laneUv);
                // Derive the footprint before adding the large absolute beat;
                // this keeps subpixel widths independent of the song position.
                float timelinePixelWidth = fwidth(timelineOffset);
                float timelineTime = timelineOffset + timelineOrigin;

                float4 startColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorA);
                float4 endColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorB);
                float progress = i.uv.x;
                float t = EvaluateRibbonEase(progress, UNITY_ACCESS_INSTANCED_PROP(Props, _EasingID));
                int colorLerpType = UNITY_ACCESS_INSTANCED_PROP(Props, _UseHSV);
                float useStrobeColors = UNITY_ACCESS_INSTANCED_PROP(Props, _UseStrobeColors);
                float index;
                float neighbour;
                float blend;
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightTimeline) > 0.5f)
                {
                    float width = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    // Use the same one-pixel footprint for color and background
                    // boundaries so lit neighbors do not widen the edge ramp.
                    float footprint = max(uvPixelWidth.y * width, 1e-4f);
                    StripCoverage(laneUv.y, width, footprint, index, neighbour, blend);
                    float time = timelineTime;
                    float timePixelWidth = timelinePixelWidth;
                    bool indexForeignColumn;
                    float4 presented = PresentLightTimeline(
                        (index + 0.5f) / width, time, timePixelWidth,
                        blend > 0.0f, indexForeignColumn);
                    [branch]
                    if (blend > 0.0f)
                    {
                        bool neighbourForeignColumn;
                        float4 neighbourPresented = PresentLightTimeline(
                            (neighbour + 0.5f) / width, time, timePixelWidth,
                            true, neighbourForeignColumn);
                        // RandomizedMonstercatRibbonEdgesMatchSupersampling:
                        // only the draw owning the center emits at a shared lit
                        // edge. Empty centers accept AA from an owned neighbor.
                        if (indexForeignColumn)
                        {
                            presented = 0.0f;
                        }
                        else if (!RibbonStripEmits(presented) && neighbourForeignColumn)
                            presented = 0.0f;
                        else
                        {
                            // Integrate isolated narrow strips across both sides
                            // without leaking their color into another lane.
                            bool indexEmits = RibbonStripEmits(presented);
                            bool neighbourEmits = RibbonStripEmits(neighbourPresented);
                            // NarrowProjectedStripBlendsWithTheTouchedNeighbor:
                            // a subpixel lit strip may touch both dark sides.
                            // Check the farther side only in this narrow case
                            // so an emitting opposite strip keeps its old blend.
                            bool isolatedStrip = indexEmits && !neighbourEmits
                                && index > 0.0f && index < width - 1.0f;
                            if (isolatedStrip)
                            {
                                float opposite = index + (index - neighbour);
                                if (StripIntervalCoverage(laneUv.y, width, footprint, opposite) > 0.0f)
                                {
                                    float4 oppositeTimes = TimelineRow((opposite + 0.5f) / width, 4.0f)
                                        + UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                                    isolatedStrip = !(oppositeTimes.y > oppositeTimes.x
                                        && time >= oppositeTimes.x && time <= oppositeTimes.y);
                                }
                            }
                            if (isolatedStrip)
                            {
                                presented *= sqrt(StripIntervalCoverage(laneUv.y, width, footprint, index));
                            }
                            else
                            {
                                presented = BlendPresentedStrips(presented, neighbourPresented, blend);
                            }
                        }
                    }
                    return ApplyRibbonEdgeCoverage(presented, laneUv, uvPixelWidth, false);
                }

                float useLightDistribution = UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightDistribution);
                [branch]
                if (useLightDistribution > 0.5f)
                {
                    float lightWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    StripCoverage(i.uv.y, lightWidth, max(uvPixelWidth.y * lightWidth, 1e-4f), index, neighbour, blend);
                    float progressPixelWidth = uvPixelWidth.x;
                    float4 presented = EvaluateDistributedStrip(
                        (index + 0.5f) / lightWidth, t, progress, progressPixelWidth,
                        colorLerpType, useStrobeColors);
                    [branch]
                    if (blend > 0.0f)
                    {
                        float4 neighbourPresented = EvaluateDistributedStrip(
                            (neighbour + 0.5f) / lightWidth, t, progress, progressPixelWidth,
                            colorLerpType, useStrobeColors);
                        presented = BlendPresentedStrips(presented, neighbourPresented, blend);
                    }

                    return ApplyRibbonEdgeCoverage(presented, i.uv, uvPixelWidth, true);
                }

                float4 color = InterpolateRibbonColor(startColor, endColor, t, colorLerpType);
                [branch]
                if (useStrobeColors > 0.5f)
                {
                    color = PresentStrobeOverlay(
                        color,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorA),
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorB),
                        t,
                        progress,
                        uvPixelWidth.x,
                        colorLerpType);
                }

                return ApplyRibbonEdgeCoverage(useStrobeColors > 0.5f
                    ? color : DisplayLightStripColor(color), i.uv, uvPixelWidth, true);
            }
            ENDHLSL
        }
    }
}
