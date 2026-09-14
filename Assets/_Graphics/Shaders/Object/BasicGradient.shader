Shader "ChroMapper/Object/Basic Gradient"
{
    Properties
    {
        _ColorA("Color A", Color) = (1.0, 0.0, 0.0, 1.0)
        _ColorB("Color B", Color) = (1.0, 0.0, 0.0, 1.0)
        // StrobingTransitionRibbonUsesDestinationEasedPhaseColor supplies the eased strobe gradient selected by each fragment's transition phase.
        _StrobeColorA("Strobe Color A", Color) = (1.0, 0.0, 0.0, 1.0)
        _StrobeColorB("Strobe Color B", Color) = (1.0, 0.0, 0.0, 1.0)
        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips carries four endpoint rows without adding another renderer.
        [NoScaleOffset] _LightDistributionTex("Light Distribution", 2D) = "black" {}
        _LightDistributionWidth("Light Distribution Width", Float) = 0
        _UseLightDistribution("Use Light Distribution", Float) = 0
        // Collider wave strips carry per-light time ranges and independent easing tracks from the preview tween.
        _UseLightTimeline("Use Light Timeline", Float) = 0
        _LightTimelineDuration("Light Timeline Duration", Float) = 1
        // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint keeps phase inputs scalar and inactive for ordinary ribbons.
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
            #include "../ShaderLibrary/Core/Tonemapping.hlsl"

            sampler2D _LightDistributionTex;

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorB)
                // StrobingTransitionRibbonUsesDestinationEasedPhaseColor keeps the strobe state per renderer so pooled ribbons can share one material.
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColorB)
                // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint keeps each pooled ribbon's phase metadata in its existing instancing buffer.
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeDuration)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFade)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFrequencyA)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFrequencyB)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseStrobeColors)
                // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips keeps the per-light table opt-in for every Basic Event ribbon.
                UNITY_DEFINE_INSTANCED_PROP(float, _UseLightDistribution)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightDistributionWidth)
                // DistributedTimingAndIndependentEasingsMatchPreview needs each strip's own clock, not one global interpolation fraction.
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

            // Collider wave color, alpha, strobe color and pulse fade use the same easing dispatch as ordinary gradients.
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
                // GLSColorEasingInputTest.BasicGradientDispatchesBeatSaberInOutVariants: the authored BeatSaber
                // InOut ids follow ByName order so ribbons preview the game's curves.
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

            // Both timeline and legacy paths share final brightness and display conversion so pixel parity tests compare like for like.
            float4 DisplayRibbonColor(float4 color)
            {
                float mult = max(color.a, 1);
                color.r *= mult;
                color.g *= mult;
                color.b *= mult;
                color.rgb *= clamp(color.a, 0, 1);
                color.a = 0;

                color = ApplyAcesTonemapping(color);
                return color;
            }

            // The nine-row table contains the actual LightColorTween endpoints and clocks, prepared once per ribbon refresh.
            float4 TimelineRow(float coordinate, float row)
            {
                return tex2D(_LightDistributionTex, float2(coordinate, (row + 0.5f) / 9.0f));
            }

            // StartingBlackStrobeDoesNotSnapBrightAtBeat93 aligns only a faded zero-to-active ramp with the destination's phase zero.
            float EvaluateStrobePhase(float startFrequency, float endFrequency, float duration, float progress, bool fade)
            {
                float cycles = duration * ((startFrequency * progress)
                    + (0.5f * (endFrequency - startFrequency) * progress * progress));
                if (fade && startFrequency <= 0.0f && endFrequency > 0.0f)
                    cycles -= (startFrequency + endFrequency) * duration * 0.5f;
                return frac(cycles);
            }

            // RibbonPixelsMatchEachOwnedPreviewLight evaluates only the active interval for this physical strip, including distributed delays.
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
                float4 color = InterpolateRibbonColor(normalFrom, normalTo,
                    EvaluateRibbonEase(normalizedColor, (int)easings.y), (int)flags.z);
                if (!composedEndpoints)
                    color.a *= lerp(rates.z, rates.w, alphaProgress);
                if (rates.x > 0.0f || rates.y > 0.0f)
                {
                    float4 strobeFrom = TimelineRow(coordinate, 2);
                    float4 strobeTo = TimelineRow(coordinate, 3);
                    strobeFrom.a = flags.x;
                    strobeTo.a = flags.y;
                    float4 strobe = InterpolateRibbonColor(strobeFrom, strobeTo,
                        EvaluateRibbonEase(normalizedColor, (int)easings.z), (int)flags.z);
                    strobe.a = lerp(flags.x * brightness.x, flags.y * brightness.y, alphaProgress);
                    float duration = times.y - times.x;
                    bool fadeEnabled = fmod(flags.w, 2.0f) >= 1.0f;
                    // Use the preview's endpoint-aligned fade-in clock for each distributed strip.
                    float phase = EvaluateStrobePhase(rates.x, rates.y, duration, normalizedAlpha, fadeEnabled);
                    float fade = fadeEnabled
                        ? EvaluateRibbonEase(1.0f - abs((phase * 2.0f) - 1.0f), (int)easings.w)
                        : step(0.5f, phase);
                    color = lerp(color, strobe, fade);
                }
                return color;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Grab GPU Instanced parameters
                float4 startColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorA);
                float4 endColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorB);
                // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint retains raw transition time for phase integration while color interpolation eases its own copy.
                float progress = i.uv.x;
                float t = EvaluateRibbonEase(progress, UNITY_ACCESS_INSTANCED_PROP(Props, _EasingID));
                int colorLerpType = UNITY_ACCESS_INSTANCED_PROP(Props, _UseHSV);
                float4 color = InterpolateRibbonColor(startColor, endColor, t, colorLerpType);
                // DistributedTimingAndIndependentEasingsMatchPreview uses a per-light clock without adding fragment-time map searches.
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightTimeline) > 0.5f)
                {
                    float width = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    float coordinate = (min(floor(saturate(i.uv.y) * width), width - 1.0f) + 0.5f) / width;
                    float time = progress * UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineDuration);
                    return DisplayRibbonColor(EvaluateLightTimeline(coordinate, time));
                }

                // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips gives every equal-width light strip its own endpoint gradient without sampling on ordinary ribbons.
                float useLightDistribution = UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightDistribution);
                float lightCoordinate = 0.0f;
                [branch]
                if (useLightDistribution > 0.5f)
                {
                    float lightWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    float lightIndex = floor(saturate(i.uv.y) * lightWidth);
                    lightIndex = min(lightIndex, lightWidth - 1.0f);
                    lightCoordinate = (lightIndex + 0.5f) / lightWidth;
                    float4 distributedStart = tex2D(
                        _LightDistributionTex,
                        float2(lightCoordinate, 0.125f));
                    float4 distributedEnd = tex2D(
                        _LightDistributionTex,
                        float2(lightCoordinate, 0.375f));
                    color = lerp(distributedStart, distributedEnd, t);
                }

                // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips evaluates LightColorTween's phase once per fragment instead of dedicating half of the ribbon to the strobe endpoint.
                float useStrobeColors = UNITY_ACCESS_INSTANCED_PROP(Props, _UseStrobeColors);
                [branch]
                if (useStrobeColors > 0.5f)
                {
                    float4 startStrobeColor;
                    float4 endStrobeColor;
                    [branch]
                    if (useLightDistribution > 0.5f)
                    {
                        startStrobeColor = tex2D(
                            _LightDistributionTex,
                            float2(lightCoordinate, 0.625f));
                        endStrobeColor = tex2D(
                            _LightDistributionTex,
                            float2(lightCoordinate, 0.875f));
                    }
                    else
                    {
                        startStrobeColor = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorA);
                        endStrobeColor = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorB);
                    }

                    float4 strobeColor = lerp(startStrobeColor, endStrobeColor, t);
                    float duration = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeDuration);
                    float startFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyA);
                    float endFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyB);
                    // The scalar fallback shares phase anchoring with physical-light ribbons instead of keeping a second formula.
                    bool fadeEnabled = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFade) > 0.5f;
                    float phase = EvaluateStrobePhase(startFrequency, endFrequency, duration, progress, fadeEnabled);
                    float trianglePhase = 1.0f - abs((phase * 2.0f) - 1.0f);
                    float strobeMix;
                    // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint preserves cubic fades while hard strobes reproduce the same phase gate across the entire ribbon.
                    [branch]
                    if (fadeEnabled)
                    {
                        strobeMix = Cubic_InOut(trianglePhase);
                    }
                    else
                    {
                        strobeMix = step(0.5f, phase);
                    }

                    color = lerp(color, strobeColor, strobeMix);
                }

                // Keep Basic Event display conversion identical to the per-light timeline path.
                return DisplayRibbonColor(color);
            }
            ENDHLSL
        }
    }
}