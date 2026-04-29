using System.Globalization;
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
            if (trackDef.Kind == BasicEventKind.IntValue)
            {
                if (e.EventData.IsLaneRotationEvent())
                {
                    var rotation = e.EventData.Rotation;
                    e.UpdateTextDisplay(true, $"{rotation}°");
                }
                else if (trackDef.Kind == BasicEventKind.IntValue)
                {
                    float speed = e.EventData.Value;
                    if (e.EventData.CustomSpeed != null) speed = (float)e.EventData.CustomSpeed;

                    e.UpdateTextDisplay(true, speed.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
                e.UpdateTextDisplay(false);

            if (trackDef.Kind != BasicEventKind.Lights)
            {
                e.UseBlockModel = true;
                if (e.EventData.Type == (int)EventTypeValue.ColorBoost)
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

                    e.ChangeFadeSize(0.5f, false);
                }
                else if (trackDef.Kind == BasicEventKind.None)
                {
                    e.ChangeColorA(RingEventsColor, false);
                    e.ChangeColorB(RingEventsColor, false);
                }
                else
                {
                    e.ChangeColorA(OtherColor, false);
                    e.ChangeColorB(OtherColor, false);
                }

                e.UpdateGradientRendering();
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
                    e.ChangeColorA(color.Multiply(1.2f), false);
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

            e.ChangeFadeSize(1f, false);

            // At this point, next Event must be a light event.
            Color? nextColor = null;
            var nextEvent = e.EventData.Next;
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
            }

            if (Settings.Instance.VisualizeChromaGradients)
                e.UpdateGradientRendering(color, nextColor, e.EventData?.CustomEasing ?? "easeLinear");

            e.UpdateMaterials();
        }
    }
}
