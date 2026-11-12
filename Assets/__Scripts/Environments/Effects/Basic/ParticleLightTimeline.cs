using Beatmap.Base;
using UnityEngine;

public class ParticleLightTimeline : BasicMovementEffect<ParticleLightStateData>
{
    private const float FadeSpeed = 2f;
    public ParticleSystemEffect Visual;

    public override void Initialize()
    {
        Visual.Initialize();
        Visual.ColorSchemeProvider.OnColorSchemeChanged -= HandleColorSchemeChanged;
        Visual.ColorSchemeProvider.OnColorSchemeChanged += HandleColorSchemeChanged;
        base.Initialize();
    }

    private void OnDestroy()
    {
        if (Visual != null && Visual.ColorSchemeProvider != null)
            Visual.ColorSchemeProvider.OnColorSchemeChanged -= HandleColorSchemeChanged;
    }

    private void HandleColorSchemeChanged()
    {
        foreach (var state in container.Collection)
        {
            if (!IsStartSentinel(state))
                state.SnapshotValid = false;
        }

        InvalidateFrom(0f);
        Refresh();
    }

    protected override ParticleLightStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(ParticleLightStateData previous, ParticleLightStateData current)
    {
        if (previous == null)
        {
            current.IsInitial = true;
            return;
        }

        current.IsInitial = previous.IsInitial;
        current.IsFlashing = previous.IsFlashing;
        current.SteadyColor = previous.SteadyColor;
        current.OffColor = previous.IsInitial
            ? Visual.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor.WithAlpha(0f)
            : previous.OffColor;
        current.BeforeFlashColor = previous.BeforeFlashColor;
        current.HighlightColor = previous.HighlightColor;
        current.AfterHighlightColor = previous.AfterHighlightColor;
        current.FlashFrame = previous.FlashFrame;
        var value = current.Base.Value;
        var scheme = Visual.ColorSchemeProvider.ColorScheme;
        switch (value)
        {
            case 0:
                current.SteadyColor = current.OffColor;
                current.IsInitial = false;
                current.IsFlashing = false;
                break;
            case 1 or 5:
                current.SteadyColor = value == 1 ? scheme.EnvironmentLeftColor : scheme.EnvironmentRightColor;
                current.OffColor = current.SteadyColor.WithAlpha(0f);
                current.IsInitial = false;
                current.IsFlashing = false;
                break;
            case 2 or 6 or 3 or 7 or -1:
                current.FlashFrame = TimeHelper.GetPreviewRenderIndex(Atsc.GetSecondsFromBeat(current.StartTime));
                current.BeforeFlashColor = EvaluateColor(previous, current.FlashFrame);
                current.HighlightColor = value is 2 or 3
                    ? scheme.EnvironmentLeftBoostColor
                    : scheme.EnvironmentRightBoostColor;
                current.OffColor = current.HighlightColor.WithAlpha(0f);
                current.AfterHighlightColor = value switch
                {
                    2 => scheme.EnvironmentLeftColor,
                    6 => scheme.EnvironmentRightColor,
                    _ => current.OffColor
                };
                current.IsInitial = false;
                current.IsFlashing = true;
                break;
        }
    }

    protected override void ApplyVisual(
        float beat,
        float seconds,
        ParticleLightStateData current,
        ParticleLightStateData next)
    {
        var frame = TimeHelper.GetPreviewRenderIndex(Atsc.GetSecondsFromBeat(beat));
        Visual.SetColor(EvaluateColor(current, frame));
    }

    private Color EvaluateColor(ParticleLightStateData state, int frame)
    {
        if (state.IsInitial)
        {
            var initial = Visual.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor;
            return Visual.LightOnStart ? initial : initial.WithAlpha(0f);
        }

        if (!state.IsFlashing)
            return state.SteadyColor;

        // Native callbacks run after Update. Flash values first reach particles on the following render frame.
        if (frame <= state.FlashFrame)
            return state.BeforeFlashColor;

        var elapsedFrames = frame - state.FlashFrame - 1;
        var factor = 1f - (FadeSpeed / TimeHelper.PreviewCallbackRate);
        var highlight = Mathf.Pow(factor, elapsedFrames);
        if (highlight * factor < 0.0001f)
            return state.AfterHighlightColor;

        return Color.Lerp(state.AfterHighlightColor, state.HighlightColor, highlight);
    }
}

public class ParticleLightStateData : BasicMovementStateData
{
    public bool IsInitial;
    public bool IsFlashing;
    public Color SteadyColor;
    public Color OffColor;
    public Color BeforeFlashColor;
    public Color HighlightColor;
    public Color AfterHighlightColor;
    public int FlashFrame;

    public ParticleLightStateData(BaseEvent data) : base(data)
    {
    }
}
