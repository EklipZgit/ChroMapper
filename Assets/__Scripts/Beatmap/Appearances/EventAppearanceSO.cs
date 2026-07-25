using System;
using System.Globalization;
using System.Text;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Event Appearance SO", fileName = "EventAppearanceSO")]
    public class EventAppearanceSO : ScriptableObject
    {
        [Space(5)] [Header("Default Colors")] public Color RedColor;
        public Color BlueColor;
        public Color WhiteColor = new(0.7264151f, 0.7264151f, 0.7264151f);
        public Color RedBoostColor;
        public Color BlueBoostColor;
        public Color WhiteBoostColor = new(0.7264151f, 0.7264151f, 0.7264151f);
        public Color OffColor;

        [Header("Other Event Colors")] public Color RingEventsColor;

        [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
        public Color OtherColor;

        public void SetAppearance(
            EventContainer e,
            bool final = true,
            bool boost = false)
        {
            var color = Color.white;
            var trackDef = e.TracksDefinition.GetBasicOrDefault(e.EventData.Type);
            e.UpdateAlpha(final ? 1.0f : 0.6f, false);
            e.UpdateScale(final ? 0.75f : 0.6f);
            e.UpdateOffset(e.AlternateShader ? -0.5f : 0f);
            // Component metadata distinguishes laser-speed IntValue tracks from unrelated integer events.
            var isLaserSpeed = trackDef.Components.HasFlag(BasicEventComponent.LightRotation);
            if (trackDef.Kind == BasicEventKind.IntValue)
                e.UpdateTextDisplay(true, GetIntValueText(e.EventData, isLaserSpeed));
            else
                e.UpdateTextDisplay(false);

            // Ring behavior comes from environment component metadata because event-type numbers can be repurposed or mixed.
            var isRingRotation = trackDef.Components.HasFlag(BasicEventComponent.RingRotation);
            // SmoothStepRingZoom only applies to The Second's legacy ring right now.
            var isSmoothStepRingZoom = trackDef.Components.HasFlag(BasicEventComponent.SmoothStepRingZoom);
            var isRingZoom = trackDef.Components.HasFlag(BasicEventComponent.RingZoom) || isSmoothStepRingZoom;
            var isRingEvent = isRingRotation || isRingZoom;
            if (isRingEvent)
            {
                e.UseBlockModel = true;
                Color ringColor;
                if (e.IsOverlapping)
                {
                    ringColor = isRingRotation
                        ? Color.Lerp(RingEventsColor, Color.red, 0.5f)
                        : Color.Lerp(Color.black, Color.red, 0.6f);
                }
                else if (isRingZoom && e.EventData.CustomStep < 0)
                {
                    // Negative ring zoom (towards the player), color darker than normal.
                    ringColor = new Color(0.25f, 0.25f, 0.25f);
                }
                else if (isRingRotation)
                {
                    // Basic Event ring rotation direction is explicit custom data, so use it to distinguish CW/CCW/random.
                    ringColor = GetRotationDirectionColor(e.EventData.CustomDirection, RingEventsColor);
                }
                else
                {
                    ringColor = RingEventsColor;
                }

                e.ChangeColorA(ringColor, false);
                e.ChangeColorB(ringColor, false);
                e.ChangeFadeSize(0.75f, false);
                e.UpdateGradientRendering();
                var ringText = GetNonLightText(e.EventData, trackDef, isSmoothStepRingZoom);
                e.UpdateTextDisplay(ringText.Length > 0, ringText);
                e.UpdateMaterials();
                return;
            }

            if (trackDef.Kind != BasicEventKind.Lights)
            {
                e.UseBlockModel = true;
                if (e.EventData.Type == (int)EventTypeValue.ColorBoostEventType)
                {
                    if (e.EventData.Value == 1)
                    {
                        e.ChangeColorA(RedBoostColor, false);
                        e.ChangeColorB(BlueBoostColor, false);
                    }
                    else
                    {
                        e.ChangeColorA(RedColor, false);
                        e.ChangeColorB(BlueColor, false);
                    }

                    e.ChangeFadeSize(0.25f, false);
                }
                else if (trackDef.Kind == BasicEventKind.None)
                {
                    e.ChangeColorA(RingEventsColor, false);
                    e.ChangeColorB(RingEventsColor, false);
                }
                else if (isLaserSpeed)
                {
                    // Laser rotation direction uses the same light/dark/random visual language as ring rotation.
                    var laserColor = GetRotationDirectionColor(e.EventData.CustomDirection, OtherColor);
                    e.ChangeColorA(laserColor, false);
                    e.ChangeColorB(laserColor, false);
                }
                else
                {
                    e.ChangeColorA(OtherColor, false);
                    e.ChangeColorB(OtherColor, false);
                }

                if (trackDef.Kind == BasicEventKind.IntValue && e.EventData.CustomLockRotation == true)
                    e.UpdateGradientRendering(OtherColor, OtherColor, allowNonLight: true);
                else
                    e.UpdateGradientRendering();

                if (trackDef.Kind != BasicEventKind.IntValue)
                {
                    var text = GetNonLightText(e.EventData, trackDef);
                    e.UpdateTextDisplay(text.Length > 0, text);
                }
                e.UpdateMaterials();
                return;
            }

            if (e.EventData.Value >= ColourManager.RgbintOffset)
            {
                color = ColourManager.ColourFromInt(e.EventData.Value);
                e.UpdateAlpha(final ? 0.9f : 0.6f, false);
            }
            else if (e.EventData.IsOff)
                color = OffColor;
            else if (e.EventData.IsBlue)
                color = boost ? BlueBoostColor : BlueColor;
            else if (e.EventData.IsRed)
                color = boost ? RedBoostColor : RedColor;
            else if (e.EventData.IsWhite) color = boost ? WhiteBoostColor : WhiteColor;

            if (Settings.Instance.EmulateChromaLite
                && e.EventData.CustomColor != null
                && !e.EventData.IsOff
                && !e.EventData.IsWhite) // White overrides Chroma
                color = e.EventData.CustomColor.Value;

            // Display floatValue only where used
            if (trackDef.Kind == BasicEventKind.Lights
                && e.EventData.Value != 0)
            {
                if (Settings.Instance.DisplayFloatValueText)
                {
                    if (!Mathf.Approximately(e.EventData.FloatValue, 1f))
                    {
                        var text = e.EventData.IsTransition
                            ? $"T{Mathf.RoundToInt(e.EventData.FloatValue * 100)}"
                            : $"{Mathf.RoundToInt(e.EventData.FloatValue * 100)}";
                        e.UpdateTextDisplay(true, text);
                    }
                    else if (e.EventData.IsTransition)
                        e.UpdateTextDisplay(true, "T");
                    else
                        e.UpdateTextDisplay(false);
                }

                // for clarity sake, we don't want this to be the same as off color
                var clampedOffColor = Color.Lerp(OffColor, color, 0.25f);
                color = Color.Lerp(clampedOffColor, color, e.EventData.FloatValue);
            }

            e.UseBlockModel = false;
            e.ChangeColorA(color, false);
            e.ChangeColorB(OffColor, false);
            switch (e.EventData.Value)
            {
                case (int)LightValue.Off:
                    e.ChangeColorB(OffColor, false);
                    e.ChangeColorA(OffColor, false);
                    break;
                case (int)LightValue.BlueOn:
                case (int)LightValue.RedOn:
                case (int)LightValue.WhiteOn:
                    e.ChangeColorB(color, false);
                    break;
                case (int)LightValue.BlueFlash:
                case (int)LightValue.RedFlash:
                case (int)LightValue.WhiteFlash:
                    e.ChangeColorA(OffColor, false);
                    e.ChangeColorB(color, false);
                    break;
                case (int)LightValue.BlueFade:
                case (int)LightValue.RedFade:
                case (int)LightValue.WhiteFade:
                    break;
                case (int)LightValue.BlueTransition:
                case (int)LightValue.RedTransition:
                case (int)LightValue.WhiteTransition:
                    e.ChangeColorB(color, false);
                    break;
            }

            e.ChangeFadeSize(0.75f, false);

            // At this point, next Event must be a light event.
            Color? nextColor = null;
            var nextEvent = e.EventData.Next;
            var easing = "easeLinear";
            string easingLabel = null;
            if (!e.EventData.IsFade && !e.EventData.IsFlash && nextEvent != null && nextEvent.IsTransition)
            {
                if (nextEvent.IsBlue)
                    nextColor = boost ? BlueBoostColor : BlueColor;
                else if (nextEvent.IsRed)
                    nextColor = boost ? RedBoostColor : RedColor;
                else if (nextEvent.IsWhite) nextColor = boost ? WhiteBoostColor : WhiteColor;

                if (Settings.Instance.EmulateChromaLite
                    && nextEvent.CustomColor != null
                    && !nextEvent.IsWhite) // White overrides Chroma
                    nextColor = nextEvent.CustomColor.Value;

                // for clarity sake, we don't want this to be the same as off color
                var clampedOffColor = Color.Lerp(OffColor, nextColor.Value, 0.25f);
                nextColor = Color.Lerp(clampedOffColor, nextColor.Value, nextEvent.FloatValue);

                easing = e.EventData?.CustomEasing ?? "easeLinear";
                easingLabel = easing == "easeLinear" ? null : AbbreviateEasing(easing);
            }

            if (e.EventData.CustomLightGradient != null)
            {
                easing = e.EventData.CustomLightGradient.EasingType;
                easingLabel = easing == "easeLinear" ? null : AbbreviateEasing(easing);
            }

            // Display lerp type when it's not the default (RGB)
            var lerpTypeLabel = e.EventData.CustomLerpType == "HSV" ? "HSV" : null;

            var lightText = GetLightText(e.EventData, GetLightValueText(e.EventData), easingLabel, lerpTypeLabel);
            e.UpdateTextDisplay(lightText.Length > 0, lightText);

            if (Settings.Instance.VisualizeChromaGradients)
            {
                e.UpdateGradientRendering(color, nextColor, easing);
            }

            e.UpdateMaterials();
        }

        public void SetAppearance(
            RotationEventContainer e,
            bool final = true)
        {
            e.UpdateScale(final ? 0.75f : 0.6f);
            e.UpdateTextDisplay(true, $"{e.EventData.Rotation}°");
        }

        private static string GetIntValueText(BaseEvent data, bool isLaserSpeed)
        {
            var speed = data.CustomSpeed ?? data.Value;
            // Basic Event laser speed displays at most one decimal place.
            var text = new StringBuilder(speed.ToString("0.#", CultureInfo.InvariantCulture));

            if (data.CustomDirection.HasValue)
                text.Append(" ").Append(DirectionText(data.CustomDirection.Value));

            if (isLaserSpeed && data.Value == 0 && data.CustomSpeed.HasValue
                && !Mathf.Approximately(data.CustomSpeed.Value, 0f))
                text.AppendLine().Append("NR");

            // Show the serialized laser rotation lock as a dedicated line beneath the speed/direction value.
            if (isLaserSpeed && data.CustomLockRotation == true)
                text.AppendLine().Append("L");

            return text.ToString();
        }

        private static string GetLightText(BaseEvent data, string existingText, string easingLabel, string lerpTypeLabel = null)
        {
            var result = existingText;
            if (!string.IsNullOrEmpty(easingLabel))
            {
                result = string.IsNullOrEmpty(result) ? easingLabel : result + "\n" + easingLabel;
            }
            if (!string.IsNullOrEmpty(lerpTypeLabel))
            {
                result = string.IsNullOrEmpty(result) ? lerpTypeLabel : result + "\n" + lerpTypeLabel;
            }
            return result;
        }

        private static string GetLightValueText(BaseEvent data)
        {
            if (!Settings.Instance.DisplayFloatValueText || data.Value == 0) return string.Empty;
            if (!Mathf.Approximately(data.FloatValue, 1f))
                return data.IsTransition
                    ? $"T{Mathf.RoundToInt(data.FloatValue * 100)}"
                    : $"{Mathf.RoundToInt(data.FloatValue * 100)}";
            return data.IsTransition ? "T" : string.Empty;
        }

        private static string GetNonLightText(
            BaseEvent data,
            TrackDefinitionBasic trackDef,
            bool isSmoothStepRingZoom = false)
        {
            // Prefer rotation text for mixed ring tracks because it includes the shared ring parameters.
            if (trackDef.Components.HasFlag(BasicEventComponent.RingRotation)) return GetRingRotationText(data);
            if (trackDef.Components.HasFlag(BasicEventComponent.RingZoom) || isSmoothStepRingZoom)
                return GetRingZoomText(data, isSmoothStepRingZoom);
            return string.Empty;
        }

        private static string GetRingRotationText(BaseEvent data)
        {
            var lines = new StringBuilder();
            if (data.CustomRingRotation.HasValue)
            {
                var rotationLine = FormatFloat(data.CustomRingRotation.Value);
                if (data.CustomDirection.HasValue)
                {
                    rotationLine += " " + DirectionText(data.CustomDirection.Value);
                }
                lines.AppendLine(rotationLine);
            }
            if (data.CustomStep.HasValue) lines.AppendLine($"Z{FormatFloat(data.CustomStep.Value)}");
            if (data.CustomProp.HasValue) lines.AppendLine($"P{FormatFloat(data.CustomProp.Value)}");
            if (data.CustomSpeed.HasValue) lines.AppendLine($"S{FormatFloat(data.CustomSpeed.Value)}");
            return lines.ToString().TrimEnd('\r', '\n');
        }

        private static string GetRingZoomText(BaseEvent data, bool isSmoothStepRingZoom)
        {
            // SmoothStepRingZoom only applies to The Second's ring and uses i as its integer fallback.
            if (isSmoothStepRingZoom)
                return $"Z{FormatFloat(data.CustomStep ?? data.Value)}";

            var lines = new StringBuilder();
            if (data.CustomStep.HasValue) lines.AppendLine($"Z{FormatFloat(data.CustomStep.Value)}");
            if (data.CustomSpeed.HasValue) lines.Append($"S{FormatFloat(data.CustomSpeed.Value)}");
            return lines.ToString().TrimEnd('\r', '\n');
        }

        private static string DirectionText(int direction) => direction == 1 ? "CW" : "CCW";

        // Basic Event rotation components share direction colors while retaining their own neutral track color.
        private static Color GetRotationDirectionColor(int? direction, Color neutralColor) => direction switch
        {
            1 => new Color(0.90f, 0.90f, 0.90f),
            0 => new Color(0.25f, 0.25f, 0.25f),
            _ => neutralColor
        };

        private static string AbbreviateEasing(string easing) => easing switch
        {
            "easeLinear" => "Lin",
            "easeInQuad" => "InQ",
            "easeOutQuad" => "OutQ",
            "easeInOutQuad" => "IOQ",
            "easeInCubic" => "InC",
            "easeOutCubic" => "OutC",
            "easeInOutCubic" => "IOC",
            "easeInQuart" => "InQt",
            "easeOutQuart" => "OutQt",
            "easeInOutQuart" => "IOQt",
            "easeInQuint" => "InQn",
            "easeOutQuint" => "OutQn",
            "easeInOutQuint" => "IOQn",
            "easeInSine" => "InS",
            "easeOutSine" => "OutS",
            "easeInOutSine" => "IOS",
            "easeInExpo" => "InE",
            "easeOutExpo" => "OutE",
            "easeInOutExpo" => "IOE",
            "easeInCirc" => "InCr",
            "easeOutCirc" => "OutCr",
            "easeInOutCirc" => "IOCr",
            "easeInBack" => "InB",
            "easeOutBack" => "OutB",
            "easeInOutBack" => "IOB",
            "easeInElastic" => "InEl",
            "easeOutElastic" => "OutEl",
            "easeInOutElastic" => "IOEl",
            "easeInBounce" => "InBo",
            "easeOutBounce" => "OutBo",
            "easeInOutBounce" => "IOBo",
            "easeStep" => "Step",
            _ => easing.StartsWith("ease", StringComparison.Ordinal) ? easing[4..] : easing
        };

        private static string FormatFloat(float value)
        {
            var magnitude = Mathf.Abs(value);
            var format = magnitude > 100f ? "0.##" : magnitude > 10f ? "0.#" : "0.##";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
