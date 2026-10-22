using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class LightColorStaticGroupEffect : LightColorGroupEffect
{
    public Color StaticColor;

    protected override void HandleBoostChange(bool boost)
    {
        // no op because boost dont exist here
    }

    protected override void UpdateObject(LightColorGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var start = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        var end = (LightColorEventStateData)(state.Next.UsePrevious ? start : state.Next);
        var darkHead = IsPreFirstEventSentinel(state);
        var previous = default(SplineControl);
        var next = default(SplineControl);
        if (!darkHead && !state.Next.UsePrevious && UsesCatmullRom(end))
        {
            PrepareSplineControls(container, state, end, out previous, out next);
        }

        ConfigureTween(
            container.Tween,
            state,
            ResolveStaticColor(start),
            ResolveStaticColor(end),
            ResolveStaticStrobeColor(start),
            ResolveStaticStrobeColor(end),
            previous,
            next,
            BeatSaberSongContainer.Instance.Map,
            darkHead);
    }

    protected override SplineControl ResolveSplineControl(LightColorEventStateData state, float time) =>
        state == null
            ? default
            : new SplineControl
            {
                State = state,
                Time = time,
                Color = ResolveStaticColor(state),
                StrobeColor = ResolveStaticStrobeColor(state),
            };

    private Color ResolveBaseColor(LightColorEventStateData state) =>
        state.Base.CustomColor
        ?? (state.Base.Color == (int)LightColor.White
            ? ColorSchemeProvider.ColorScheme.EnvironmentWhiteColor
            : StaticColor);

    private Color ResolveStaticColor(LightColorEventStateData state) =>
        GLSColorDistribution.ApplyNormal(
            ResolveBaseColor(state),
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);

    private Color ResolveStaticStrobeColor(LightColorEventStateData state) =>
        GLSColorDistribution.ApplyStrobe(
            ResolveBaseColor(state),
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);
}
