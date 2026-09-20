using Beatmap.Enums;
using UnityEngine;

public class
    LightColorStaticGroupEffect : LightColorGroupEffect
{
    public Color StaticColor;

    protected override void HandleBoostChange(bool boost)
    {
        // no op because boost dont exist here
    }

    protected override void UpdateObject(LightColorGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var tween = container.Tween;

        tween.StartTimeAlpha = tween.StartTimeColor = state.StartTime;
        var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        tween.StartAlpha = startState.Brightness;
        tween.StartColor = startState.Base.CustomColor
            ?? (startState.Base.Color == (int)LightColor.White
                ? ColorSchemeProvider.ColorScheme.EnvironmentWhiteColor
                : StaticColor);
        tween.StartStrobeFrequency = StrobeFrequencyFor(startState.Base);
        tween.StartStrobeBrightness = startState.Base.StrobeBrightness;
        tween.StartStrobeColor = startState.Base.StrobeColor ?? tween.StartColor;

        tween.EndTimeAlpha = tween.EndTimeColor = state.EndTime;
        var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        tween.EndAlpha = endState.Brightness;
        tween.EndColor = endState.Base.CustomColor
            ?? (endState.Base.Color == (int)LightColor.White
                ? ColorSchemeProvider.ColorScheme.EnvironmentWhiteColor
                : StaticColor);

        if (endState.Base.Easing == (int)EaseType.None)
        {
            tween.EndStrobeFrequency = StrobeFrequencyFor(startState.Base);
            tween.EndStrobeBrightness = startState.Base.StrobeBrightness;
            tween.EndStrobeColor = tween.StartStrobeColor;
            tween.StrobeFade = startState.Base.StrobeFade == 1;
        }
        else
        {
            tween.EndStrobeFrequency = StrobeFrequencyFor(endState.Base);
            tween.EndStrobeBrightness = endState.Base.StrobeBrightness;
            tween.EndStrobeColor = endState.Base.StrobeColor ?? tween.EndColor;
            // shouldn't we fade between no strobe fade and strobe fade...? What does the game even do?
            tween.StrobeFade = endState.Base.StrobeFade == 1;
        }

        tween.Easing = Easing.FromID(endState.Base.Easing);
    }
}
