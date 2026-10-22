namespace Beatmap.Enums
{
    public enum EaseType
    {
        None = -1,
        Linear,
        InQuadratic,
        OutQuadratic,
        InOutQuadratic,
        InSinusoidal,
        OutSinusoidal,
        InOutSinusoidal,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuartic,
        OutQuartic,
        InOutQuartic,
        InQuintic,
        OutQuintic,
        InOutQuintic,
        InExponential,
        OutExponential,
        InOutExponential,
        InCircular,
        OutCircular,
        InOutCircular,
        InBack,
        OutBack,
        InOutBack,
        InElastic,
        OutElastic,
        InOutElastic,
        InBounce,
        OutBounce,
        InOutBounce,
        BeatSaberInOutBack = 100,
        BeatSaberInOutElastic,
        BeatSaberInOutBounce,
        // Experimental ChroMapper-only four-point spline parameterizations. Game-side support is
        // unavailable until matching BeatSaberChromaGLS plugin support exists, so maps saved with
        // these ids have no guaranteed game playback. They are interpolation modes over neighboring
        // GLS event states, not polynomial easing curves, so they have no entry in Easing.ByID.
        CatmullRom = 103,
        CatmullRomCentripetal,
        CatmullRomChordal
    }
}
