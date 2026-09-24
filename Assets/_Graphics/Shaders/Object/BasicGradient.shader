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
            #include "../ShaderLibrary/Common/Bloom.hlsl"

            sampler2D _LightDistributionTex;

            // Define instanced properties
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Keep the ribbon's HSV conversion self-contained because the shared shader includes only easing functions.
            float3 RGBToHSV(float3 color)
            {
                const float epsilon = 1e-10f;
                const float4 constants = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
                float4 p = lerp(float4(color.bg, constants.wz), float4(color.gb, constants.xy), step(color.b, color.g));
                float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
                float chroma = q.x - min(q.w, q.y);
                return float3(abs(q.z + ((q.w - q.y) / ((6.0f * chroma) + epsilon))), chroma / (q.x + epsilon), q.x);
            }

            // Convert the hue, saturation, and value interpolated above back to the display color.
            float3 HSVToRGB(float3 color)
            {
                float3 rgb = abs((frac(color.xxx + float3(0.0f, 2.0f / 3.0f, 1.0f / 3.0f)) * 6.0f) - 3.0f);
                return color.z * lerp(1.0f, saturate(rgb - 1.0f), color.y);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float EvaluateRibbonEase(float t, int id)
            {
                // a small price to pay for salvation
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
                // BasicGradientDispatchMatchesEasingShaderId keeps Back 22-24 and Elastic 25-27 aligned with Easing.ByName.
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

            // Correct the color space of ribbons to match lights
            float3 RibbonTextureToLinear(float3 color)
            {
#ifdef UNITY_COLORSPACE_GAMMA
                return color;
#else
                return float3(
                    GammaToLinearSpaceExact(color.r),
                    GammaToLinearSpaceExact(color.g),
                    GammaToLinearSpaceExact(color.b));
#endif
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

            // The nine-row table contains the actual LightColorTween endpoints and clocks, prepared once per ribbon refresh.
            float4 TimelineRow(float coordinate, float row)
            {
                return tex2Dlod(_LightDistributionTex, float4(coordinate, (row + 0.5f) / 9.0f, 0.0f, 0.0f));
            }

            float EvaluateStrobePhase(float startFrequency, float endFrequency, float duration, float progress, bool fade)
            {
                float cycles = duration * ((startFrequency * progress)
                    + (0.5f * (endFrequency - startFrequency) * progress * progress));
                if (fade && startFrequency <= 0.0f && endFrequency > 0.0f)
                    cycles -= (startFrequency + endFrequency) * duration * 0.5f;
                return frac(cycles);
            }

            float4 EvaluateLightTimeline(float coordinate, float time)
            {
                float4 times = TimelineRow(coordinate, 4);
                if (times.y <= times.x || time < times.x || time > times.y)
                    return 0.0f;
                float4 rates = TimelineRow(coordinate, 5);
                float4 brightness = TimelineRow(coordinate, 6);
                float4 flags = TimelineRow(coordinate, 7);
                float4 easings = TimelineRow(coordinate, 8);
                float4 normalFrom = TimelineRow(coordinate, 0);
                float4 normalTo = TimelineRow(coordinate, 1);
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
                    float4 strobeFrom = TimelineRow(coordinate, 2);
                    float4 strobeTo = TimelineRow(coordinate, 3);
                    strobeFrom.a = flags.x;
                    strobeTo.a = flags.y;
                    bool preserveStrobePeak = abs((flags.x * brightness.x) - (flags.y * brightness.y)) <= 0.00001f;
                    float4 strobe = InterpolatePhysicalRibbonColor(strobeFrom, strobeTo,
                        EvaluateRibbonEase(normalizedColor, (int)easings.z), (int)flags.z, preserveStrobePeak);
                    strobe.a = lerp(flags.x * brightness.x, flags.y * brightness.y, alphaProgress);
                    float duration = times.y - times.x;
                    bool fadeEnabled = fmod(flags.w, 2.0f) >= 1.0f;
                    float phase = EvaluateStrobePhase(rates.x, rates.y, duration, normalizedAlpha, fadeEnabled);
                    float fade = fadeEnabled
                        ? EvaluateRibbonEase(1.0f - abs((phase * 2.0f) - 1.0f), (int)easings.w)
                        : step(0.5f, phase);
                    color = InterpolateRibbonColor(color, strobe, fade, (int)flags.z);
                }
                // PR 666's parametric lights consume material colors directly, so texture-backed timelines retain that same authored color space.
                return color;
            }

            // StripBoundaryAntiAliasingBlendsPixelsStraddlingStripEdges: floor() snapping staircases strip edges inside
            // a single quad where MSAA cannot reach. The pixel-footprint coverage decides how much of the neighbour
            // strip the fragment should contain; interior fragments keep blend = 0 and stay exact.
            void StripCoverage(float uvY, float width, out float index, out float neighbour, out float blend)
            {
                float stripPos = saturate(uvY) * width;
                index = min(floor(stripPos), width - 1.0f);
                float pixelFootprint = max(fwidth(stripPos), 1e-4f);
                float halfFootprint = pixelFootprint * 0.5f;
                float coverageNext = saturate((stripPos + halfFootprint - (index + 1.0f)) / pixelFootprint);
                float coveragePrev = saturate((index - (stripPos - halfFootprint)) / pixelFootprint);
                blend = max(coverageNext, coveragePrev);
                neighbour = clamp(index + (coverageNext > 0.0f ? 1.0f : -1.0f), 0.0f, width - 1.0f);
            }

            // StripBoundaryAntiAliasingBlendsLitToInactive / StripBoundaryAntiAliasingBlendsInactiveToLit:
            // an inactive strip has zero source color and leaves the existing framebuffer untouched.
            bool RibbonStripEmits(float4 presented)
            {
                return presented.r + presented.g + presented.b > 0.0f;
            }

            // StripBoundaryAntiAliasingBlendsLitToInactive / StripBoundaryAntiAliasingBlendsInactiveToLit:
            // SrcColor blending squares the source against a black background. Scale an emitting strip
            // by the square root of its pixel coverage so the final contribution follows coverage.
            // Keep the existing displayed-color interpolation when both adjacent strips emit.
            float4 BlendPresentedStrips(float4 presented, float4 neighbourPresented, float blend)
            {
                bool emits = RibbonStripEmits(presented);
                bool neighbourEmits = RibbonStripEmits(neighbourPresented);
                if (emits && neighbourEmits)
                    return lerp(presented, neighbourPresented, blend);
                if (emits)
                    return presented * sqrt(1.0f - blend);
                return neighbourPresented * sqrt(blend);
            }

            // RibbonOuterEdgeAntiAliasingScalesPartialPixels: the outer mesh boundary has no adjacent
            // strip to evaluate. Attenuate covered fragments near UV 0/1 by their in-mesh fraction;
            // the square root compensates for this shader's SrcColor blend on dark backgrounds.
            float4 ApplyRibbonEdgeCoverage(float4 presented, float uvY)
            {
                float pixelWidth = max(fwidth(uvY), 1e-4f);
                float edgeDistance = min(uvY, 1.0f - uvY);
                if (edgeDistance >= pixelWidth * 0.5f)
                    return presented;
                float coverage = saturate(0.5f + (edgeDistance / pixelWidth));
                return presented * sqrt(coverage);
            }

            // Strobe overlay shared by the scalar and distributed paths; identical to the pre-refactor frag block.
            float4 ApplyStrobeOverlay(
                float4 color, float4 startStrobeColor, float4 endStrobeColor, float t, float progress, int colorLerpType)
            {
                float4 strobeColor = lerp(startStrobeColor, endStrobeColor, t);
                float duration = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeDuration);
                float startFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyA);
                float endFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyB);
                bool fadeEnabled = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFade) > 0.5f;
                float phase = EvaluateStrobePhase(startFrequency, endFrequency, duration, progress, fadeEnabled);
                float trianglePhase = 1.0f - abs((phase * 2.0f) - 1.0f);
                float strobeMix;
                [branch]
                if (fadeEnabled)
                {
                    strobeMix = Cubic_InOut(trianglePhase);
                }
                else
                {
                    strobeMix = step(0.5f, phase);
                }

                return InterpolateRibbonColor(color, strobeColor, strobeMix, colorLerpType);
            }

            // Full endpoint-distribution composition for one strip. tex2Dlod keeps the mip-less texture's samples
            // exact inside the boundary fragment's divergent second evaluation.
            float4 EvaluateDistributedStrip(
                float lightCoordinate, float t, float progress, int colorLerpType, float useStrobeColors)
            {
                float4 color = lerp(
                    tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.125f, 0.0f, 0.0f)),
                    tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.375f, 0.0f, 0.0f)),
                    t);
                if (useStrobeColors > 0.5f)
                {
                    color = ApplyStrobeOverlay(
                        color,
                        tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.625f, 0.0f, 0.0f)),
                        tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.875f, 0.0f, 0.0f)),
                        t,
                        progress,
                        colorLerpType);
                }

                return color;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

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
                    StripCoverage(i.uv.y, width, index, neighbour, blend);
                    float time = progress * UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineDuration);
                    // StripBoundaryAntiAliasingBlendsLitToInactive: evaluate each timeline separately,
                    // then apply lit/off coverage so a dark light still exposes the background.
                    float4 presented = DisplayLightStripColor(
                        EvaluateLightTimeline((index + 0.5f) / width, time));
                    [branch]
                    if (blend > 0.0f)
                    {
                        float4 neighbourPresented = DisplayLightStripColor(
                            EvaluateLightTimeline((neighbour + 0.5f) / width, time));
                        presented = BlendPresentedStrips(presented, neighbourPresented, blend);
                    }

                    // RibbonOuterEdgeAntiAliasingScalesPartialPixels also resolves the timeline's
                    // first and last strips against the background at the mesh silhouette.
                    return ApplyRibbonEdgeCoverage(presented, i.uv.y);
                }

                float useLightDistribution = UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightDistribution);
                [branch]
                if (useLightDistribution > 0.5f)
                {
                    float lightWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    StripCoverage(i.uv.y, lightWidth, index, neighbour, blend);
                    // StripBoundaryAntiAliasingBlendsInactiveToLit: blend evaluated display colors and
                    // apply coverage when either distributed endpoint leaves a strip dark.
                    float4 presented = DisplayLightStripColor(EvaluateDistributedStrip(
                        (index + 0.5f) / lightWidth, t, progress, colorLerpType, useStrobeColors));
                    [branch]
                    if (blend > 0.0f)
                    {
                        float4 neighbourPresented = DisplayLightStripColor(EvaluateDistributedStrip(
                            (neighbour + 0.5f) / lightWidth, t, progress, colorLerpType, useStrobeColors));
                        presented = BlendPresentedStrips(presented, neighbourPresented, blend);
                    }

                    // RibbonOuterEdgeAntiAliasingScalesPartialPixels also resolves distributed
                    // endpoint strips against the background at the mesh silhouette.
                    return ApplyRibbonEdgeCoverage(presented, i.uv.y);
                }

                float4 color = InterpolateRibbonColor(startColor, endColor, t, colorLerpType);
                [branch]
                if (useStrobeColors > 0.5f)
                {
                    color = ApplyStrobeOverlay(
                        color,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorA),
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorB),
                        t,
                        progress,
                        colorLerpType);
                }

                // RibbonOuterEdgeAntiAliasingScalesPartialPixels resolves scalar ribbon silhouettes
                // while preserving their authored display color inside the mesh.
                return ApplyRibbonEdgeCoverage(DisplayLightStripColor(color), i.uv.y);
            }
            ENDHLSL
        }
    }
}
