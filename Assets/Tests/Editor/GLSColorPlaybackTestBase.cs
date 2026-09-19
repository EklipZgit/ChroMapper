using System;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    /// <summary>
    /// Shared fixture for GLS color playback/ribbon tests: a real LightColorGroupEffect over the
    /// supplied 2026-09-13 GLS Wave Testing map (Info.dat: 100 BPM, WeaveEnvironment, eight physical
    /// lights on group 1) with recording lights instead of renderers, plus the ribbon pixel-sampling
    /// helpers every subject file needs. Real InsertData/UpdateTime is the oracle; only the final
    /// renderer is replaced with a recording light.
    /// </summary>
    public abstract class GLSColorPlaybackTestBase : TestBase
    {
        internal const int LightCount = 8;
        internal const string MapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":0,""sf"":1,""customData"":{""color"":[1,0.4,0.913]}}]}]},
            {""b"":11,""g"":1,""e"":[
                {""f"":{""f"":2,""p"":0,""t"":2,""c"":8},""w"":0,""d"":1,""r"":0.03,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":5,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.942]}},
                    {""b"":6,""c"":0,""s"":0.6,""i"":1,""f"":0,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.942]}},
                    {""b"":18.75,""c"":0,""s"":0.5,""i"":1,""f"":1,""sb"":0.8,""sf"":1,""customData"":{""color"":[0,0.3,0],""strobeColorDistributions"":[""sv,0.7,lin""],""colorDistributions"":[""v,5,l,l""],""strobeColor"":[0.2,0.3,0.3],""strobeInterval"":5}},
                    {""b"":28,""c"":0,""s"":0.8,""i"":1,""f"":1,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.941],""colorDistributions"":[""hs,-0.6,lin"",""hs,0.6,ioc""],""strobeColorDistributions"":[""hs,0.2,lin"",""v,2,lin""],""strobeInterval"":1}}]},
                {""f"":{""f"":1,""p"":1},""w"":0.4,""d"":2,""r"":0.01,""t"":1,""b"":0,""i"":1,""e"":[{""b"":11,""c"":0,""s"":0.2,""i"":1,""f"":1,""sb"":0.9,""sf"":1,""customData"":{""color"":[1,0.4,0.913],""strobeColorDistributions"":[""h,0.3,lin""],""colorDistributions"":[""v,6,lin,l""],""strobeColorEasing"":4,""strobeInterval"":2,""strobeEasing"":19}}]}]},
            {""b"":46,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":1,""customData"":{""color"":[0,0,1],""strobeColor"":[0.969,0,0],""colorEasing"":11}}]}]}]}";

        // OEM cross-group interruption is keyed to the later group's element start at beat 15, while its first
        // all-light color node occurs at beat 17; prior filtered nodes at beats 15, 17, and 20 must all be dropped.
        internal const string InterruptedMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,1,1]}}]}]},
            {""b"":10,""g"":1,""e"":[{""f"":{""f"":2,""p"":0,""t"":2,""c"":8},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0]}},
                {""b"":5,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,1,0]}},
                {""b"":7,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,1,0]}},
                {""b"":10,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,1]}}]}]},
            {""b"":15,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,1]}}]}]}]}";

        // User-reported brightness-wash map: the s=20 shifted node sits ~1 beat before the all-light
        // blue node, where the lasers already render washed-out blue while the ribbon kept showing
        // the raw color-distributed colors. Box-level strobeColorDistributions plus sf=0/colorEasing=29 events exercise
        // the same transition data as the live map.
        internal const string ReportedWaveMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":11,""g"":1,""e"":[
                {""f"":{""f"":2,""p"":0,""t"":2,""r"":0,""c"":8,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0.03,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":2,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.942],""strobeEasing"":4}},
                    {""b"":6,""c"":0,""s"":0.6,""i"":1,""f"":0,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.942]}},
                    {""b"":18.75,""c"":0,""s"":0.5,""i"":1,""f"":1,""sb"":0.8,""sf"":1,""customData"":{""color"":[0,0.3,0],""strobeColorDistributions"":[""sv,0.7,lin""],""colorDistributions"":[""v,5,l,l""],""strobeColor"":[0.2,0.3,0.3],""strobeInterval"":5}},
                    {""b"":28,""c"":0,""s"":20,""i"":1,""f"":1,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.941],""colorDistributions"":[""hs,-0.6,lin"",""hs,0.6,ioc""],""strobeColorDistributions"":[""hs,0.2,lin"",""v,2,lin""],""strobeInterval"":1}}],
                 ""customData"":{""strobeColorDistributions"":[""h,-1,lin""]}},
                {""f"":{""f"":1,""p"":1,""t"":0,""r"":0,""c"":0,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0.4,""d"":2,""r"":0.01,""t"":1,""b"":0,""i"":1,""e"":[
                    {""b"":11,""c"":0,""s"":0.2,""i"":1,""f"":1,""sb"":0.9,""sf"":0,""customData"":{""color"":[1,0.4,0.913],""strobeColorDistributions"":[""h,0.3,lin""],""colorDistributions"":[""v,6,lin,l""],""strobeInterval"":2,""colorEasing"":29,""strobeColorEasing"":4}},
                    {""b"":13,""c"":0,""s"":1,""i"":1,""f"":2,""sb"":1,""sf"":1,""customData"":{""strobeColor"":[1,0.973,0],""color"":[0.953,1,0]}}]}]},
            {""b"":46,""g"":1,""e"":[{""f"":{""f"":1,""p"":1,""t"":0,""r"":0,""c"":0,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":1,""customData"":{""color"":[0,0,1],""strobeColor"":[0.969,0,0]}}]}]}]}";

        // Exact beat-100 report: complementary hard strobes share both timestamps across two serialized boxes.
        internal const string AlternatingChunksMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[{""b"":100,""g"":1,""e"":[
            {""f"":{""f"":2,""p"":0,""t"":2,""r"":0,""c"":4,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":1,""sf"":0},
                {""b"":3.909,""c"":0,""s"":0.5,""i"":2,""f"":2,""sb"":1,""sf"":1}]},
            {""f"":{""f"":1,""p"":1,""t"":0,""r"":0,""c"":0,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":1,""sb"":0,""sf"":0},
                {""b"":3.909,""c"":0,""s"":0.5,""i"":2,""f"":2,""sb"":1,""sf"":1}]}]}]}";

        private BaseDifficulty originalMap;
        private float originalBpm;
        protected BaseDifficulty map;
        private GameObject playbackRoot;
        protected LightColorGroupEffect playback;
        protected LightColorGroupContainer[] containers;
        private readonly RecordingLight[] lights = new RecordingLight[LightCount];
        protected EventAppearanceSO appearance;

        [SetUp]
        public void PreparePlayback()
        {
            var song = BeatSaberSongContainer.Instance;
            originalMap = song.Map;
            originalBpm = song.Info.BeatsPerMinute;
            song.Info.BeatsPerMinute = 100f;
            appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            LoadPlayback(MapJson);
        }

        // Interruption and bounds regressions reload a different synthetic map through the same real
        // eight-light effect without duplicating recording-light registration or container capture.
        protected void LoadPlayback(string json)
        {
            var song = BeatSaberSongContainer.Instance;
            if (playback != null)
            {
                // PR 666 owns the active palette through ColorSchemeProvider, so dispose the fixture-owned asset there.
                Object.DestroyImmediate(playback.ColorSchemeProvider.ColorScheme);
                Object.DestroyImmediate(playbackRoot);
            }

            map = BeatmapFactory.GetDifficultyFromJson(JSON.Parse(json), "GLS playback", song.Info, song.MapDifficultyInfo);
            song.Map = map;
            playbackRoot = new GameObject("GLS deterministic playback");
            playbackRoot.SetActive(false);
            playback = playbackRoot.AddComponent<LightColorGroupEffect>();
            playback.ColorBoostEffect = playbackRoot.AddComponent<ColorBoostEffect>();
            // Mirror PR 666's environment wiring so deterministic GLS playback exercises the provider-backed path.
            playback.ColorSchemeProvider = playbackRoot.AddComponent<ColorSchemeProvider>();
            playback.ColorSchemeProvider.ColorScheme = ScriptableObject.CreateInstance<ColorSchemeSO>();
            playback.ColorBoostEffect.ColorSchemeProvider = playback.ColorSchemeProvider;
            playback.Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            playback.ID = 1;
            playback.Count = LightCount;
            for (var light = 0; light < LightCount; light++)
            {
                lights[light] = playbackRoot.AddComponent<RecordingLight>();
                lights[light].ID = light;
                playback.Register(lights[light]);
            }
            playback.Initialize();
            foreach (var group in map.LightColorEventBoxGroups)
                playback.InsertData(group);
            // UpdateTime only reconfigures the tween when the current state changes; production pairs
            // every Initialize with Refresh so the sentinel head segment's tween exists before the
            // first scrub inside it.
            playback.Refresh();
            containers = (LightColorGroupContainer[])typeof(LightColorGroupEffect)
                .GetField("idToContainer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(playback);
        }

        // Restore the scene's map/BPM even on failure; tests never edit the user's WIP files or use native audio playback.
        protected override void BeforeCleanup()
        {
            if (playback != null)
                Object.DestroyImmediate(playback.ColorSchemeProvider.ColorScheme);
            Object.DestroyImmediate(playbackRoot);
            Object.DestroyImmediate(appearance);
            BeatSaberSongContainer.Instance.Map = originalMap;
            BeatSaberSongContainer.Instance.Info.BeatsPerMinute = originalBpm;
        }

        // A fixed non-black environment color isolates alternating brightness while retaining the supplied node/filter data verbatim.
        protected void LoadAlternatingChunks()
        {
            LoadPlayback(AlternatingChunksMapJson);
            appearance.RedColor = Color.white;
            playback.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor = Color.white;
            // The load-time Refresh bakes scheme colors into each tween; re-resolve them after overriding the environment color.
            playback.Refresh();
        }

        // These helpers query the actual generated state/tween rather than replaying filter or strobe rules in the test.
        protected BaseLightColorBase Node(int group, int box = 0, int node = 0) =>
            map.LightColorEventBoxGroups[group].Boxes[box].Events[node];

        protected float SongTime(float beat) => (float)map.JsonTimeToSongBpmTime(beat);

        // State membership directly proves whether EventGroupEffect unpacked a node for this physical light.
        protected bool HasState(int light, BaseLightColorBase node)
        {
            foreach (var state in containers[light].EventContainer.Collection)
            {
                if (ReferenceEquals(state.Base, node)) return true;
            }

            return false;
        }

        // Inspecting an endpoint must not move the playback cursor and cause UpdateTime to skip rebuilding its tween.
        protected LightColorEventStateData StateAt(int light, float beat) =>
            containers[light].EventContainer.GetStateAt(SongTime(beat)).state;

        protected Color ColorAt(int light, float beat)
        {
            playback.UpdateTime(false, SongTime(beat));
            return lights[light].Color;
        }

        protected static void AssertColor(Color actual, Color expected, float tolerance, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), message);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), message);
        }

        // Ribbon strips write alpha 0 while the parametric light shader saturates its alpha, so
        // strip/light parity is asserted on rgb only.
        protected static void AssertColorRgb(Color actual, Color expected, float tolerance, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), message);
        }

        // HundredBrightnessRedToGreenRibbonMatchesGameYellowMidpoint verifies that peak compensation
        // remains on the same hue ray and never recreates the pre-bloom brightness trough.
        private static void AssertPeakCompensatedRibbon(Color actual, Color preBloomReference, string message)
        {
            Assert.That(
                actual.maxColorComponent,
                Is.GreaterThanOrEqualTo(preBloomReference.maxColorComponent - 0.02f),
                message);
            Color.RGBToHSV(actual, out var actualHue, out _, out _);
            Color.RGBToHSV(preBloomReference, out var expectedHue, out _, out _);
            var hueDistance = Mathf.Abs(Mathf.DeltaAngle(actualHue * 360f, expectedHue * 360f)) / 360f;
            Assert.That(hueDistance, Is.LessThanOrEqualTo(0.08f), message);
        }

        // All raster cases compare the actual shader against the parametric light shader's output
        // for the same production light sample and its ownership mask. The laser reference renders
        // PR 666's premultiplied, white-boosted color exactly like the preview lights, so it cannot share a ribbon-side
        // color-space or interpolation bug the way a same-shader reference texture did.
        protected void AssertRibbonPixels(
            int group,
            int box,
            int node,
            float beat,
            int onlyLight,
            Func<int, bool> expectsPeakCompensation = null,
            Func<int, Color> expectedPreviewAtLight = null,
            Func<int, bool> expectedSourceOwnership = null)
        {
            var source = Node(group, box, node);
            var ribbonObject = new GameObject("GLS per-light ribbon");
            Material material = null;
            Material lightMaterial = null;
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, source, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf, $"Source {source.JsonTime} must have a ribbon to its lights' next nodes.");
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                lightMaterial = GLSColorTransitionCacheTest.CreateLightSampleMaterial();
                var duration = ribbon.transform.localScale.x / (EditorScaleController.EditorScale * (4f / 3f));
                var progress = (SongTime(beat) - source.SongBpmTime) / duration;
                for (var light = 0; light < LightCount; light++)
                {
                    if (onlyLight >= 0 && light != onlyLight)
                        continue;
                    var state = StateAt(light, beat);
                    var sourceOwnsLight = ReferenceEquals(state.Base, source);
                    var live = ColorAt(light, beat);
                    // WashedOutColorDistributionRibbonMatchesPreviewLight freezes preview output and
                    // ownership independently of both production consumers before testing the strip.
                    if (expectedPreviewAtLight != null)
                    {
                        AssertColor(
                            live,
                            expectedPreviewAtLight(light),
                            0.006f,
                            $"light={light} beat={beat}: live preview must match the frozen authored expectation.");
                    }
                    if (expectedSourceOwnership != null)
                    {
                        Assert.That(
                            sourceOwnsLight,
                            Is.EqualTo(expectedSourceOwnership(light)),
                            $"source={source.JsonTime} light={light} beat={beat}: ownership must match the authored filter.");
                    }
                    // PR 666 white boost can illuminate opaque black; an unowned strip is transparent absence, not a black preview light.
                    var expected = sourceOwnsLight
                        ? expectedPreviewAtLight?.Invoke(light) ?? live
                        : Color.clear;
                    var lane = (LightCount - light - 0.5f) / LightCount;
                    var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                    // SharedRibbonAlphaCurveIsTunableAndRollbackSafe keeps the authoritative live light unchanged while the expected raster receives the ribbon-only opacity policy.
                    lightMaterial.SetColor(
                        "_Color",
                        GLSColorTransitionCacheTest.ApplyExpectedRibbonOpacity(expected));
                    var expectedPixel = GLSColorTransitionCacheTest.RenderGradientPixel(lightMaterial, 0.5f, 0.5f);
                    // The render target stores the strip's linear composite; on screen it passes the
                    // pipeline's linear->sRGB present, while the light shader emits the intended display
                    // value directly. Compare the strip's presented bytes (pixel.gamma)
                    // against the light's emitted bytes.
                    var presented = pixel.gamma;
                    var timelineTexture = properties.GetTexture(Shader.PropertyToID("_LightDistributionTex")) as Texture2D;
                    // Separate prepared-state mismatches from shader interpolation errors without changing either rendering path.
                    if (Mathf.Abs(presented.r - expectedPixel.r) > 0.02f
                        || Mathf.Abs(presented.g - expectedPixel.g) > 0.02f
                        || Mathf.Abs(presented.b - expectedPixel.b) > 0.02f)
                    {
                        var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
                        if (timeline.TryGetOutgoing(source, light, out var preparedState))
                        {
                            var prepared = new LightColorTween();
                            timeline.ConfigureTween(prepared, preparedState, appearance, _ => false);
                            prepared.UpdateTime(SongTime(beat));
                            Debug.Log($"[GLSRibbonMismatch] beat={beat} light={light} uv={progress} " +
                                $"live={expected} prepared={prepared.Color} start={prepared.StartColor} end={prepared.EndColor} " +
                                $"strobeStart={prepared.StartStrobeColor} strobeEnd={prepared.EndStrobeColor} " +
                                $"easeRow={timelineTexture.GetPixel(LightCount - light - 1, 8)} " +
                                $"texStart={timelineTexture.GetPixel(LightCount - light - 1, 0)} texEnd={timelineTexture.GetPixel(LightCount - light - 1, 1)} " +
                                $"times={timelineTexture.GetPixel(LightCount - light - 1, 4)} rates={timelineTexture.GetPixel(LightCount - light - 1, 5)} " +
                                $"levels={timelineTexture.GetPixel(LightCount - light - 1, 6)} flags={timelineTexture.GetPixel(LightCount - light - 1, 7)} " +
                                $"duration={material.GetFloat("_LightTimelineDuration")}");
                        }
                    }
                    var message = $"source={source.JsonTime} light={light} beat={beat}: ribbon pixels must match the same light's preview state.";
                    // HundredBrightnessRedToGreenRibbonMatchesGameYellowMidpoint: expected visual
                    // compensation comes from the authored test case, never from the ribbon payload
                    // under test, so a malformed producer cannot make its own assertion pass.
                    if (expectsPeakCompensation != null && expectsPeakCompensation(light))
                        AssertPeakCompensatedRibbon(presented, expectedPixel, message);
                    else
                        AssertColorRgb(presented, expectedPixel, 0.02f, message);
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(lightMaterial);
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // Incoming ribbons key off the segment before the target; each owned strip must equal that light's preview sample.
        protected void AssertIncomingRibbonPixels(int group, int box, int node, float beat, Func<int, bool> stripOwned)
        {
            var target = Node(group, box, node);
            var ribbonObject = new GameObject("GLS incoming ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(ribbon, target, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf,
                    $"Target {target.JsonTime} must have an incoming ribbon for lights lit before their first node.");
                AssertIncomingStripPixels(renderer, ribbon, beat, stripOwned);
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // FirstNodeHeadExtendsOuterIncomingRibbonToMapStart shares the same per-strip comparison with the container-owned incoming renderer.
        protected void AssertIncomingStripPixels(
            MeshRenderer renderer, LightGradientController ribbon, float beat, Func<int, bool> stripOwned)
        {
            Material material = null;
            Material lightMaterial = null;
            try
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                lightMaterial = GLSColorTransitionCacheTest.CreateLightSampleMaterial();
                var progress = (SongTime(beat) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                for (var light = 0; light < LightCount; light++)
                {
                    // PR 666 white boost can illuminate opaque black; masked lanes must remain transparent rather than entering the light shader as black with alpha one.
                    var expected = stripOwned(light) ? ColorAt(light, beat) : Color.clear;
                    var lane = (LightCount - light - 0.5f) / LightCount;
                    var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane).gamma;
                    // SharedRibbonAlphaCurveIsTunableAndRollbackSafe keeps incoming-strip parity on the common ribbon-only opacity policy.
                    lightMaterial.SetColor(
                        "_Color",
                        GLSColorTransitionCacheTest.ApplyExpectedRibbonOpacity(expected));
                    var expectedPixel = GLSColorTransitionCacheTest.RenderGradientPixel(lightMaterial, 0.5f, 0.5f);
                    if (Mathf.Abs(pixel.r - expectedPixel.r) > 0.02f
                        || Mathf.Abs(pixel.g - expectedPixel.g) > 0.02f
                        || Mathf.Abs(pixel.b - expectedPixel.b) > 0.02f)
                    {
                        var texture = properties.GetTexture(Shader.PropertyToID("_LightDistributionTex")) as Texture2D;
                        Debug.Log($"[IncomingRibbonMismatch] light={light} beat={beat} progress={progress} " +
                            $"live={expected} pixel={pixel} expectedPixel={expectedPixel} " +
                            $"timelineStart={ribbon.ColorTimelineStart} duration={ribbon.ColorTimelineDuration} " +
                            $"texStart={texture.GetPixel(LightCount - light - 1, 0)} texEnd={texture.GetPixel(LightCount - light - 1, 1)} " +
                            $"times={texture.GetPixel(LightCount - light - 1, 4)} rates={texture.GetPixel(LightCount - light - 1, 5)} " +
                            $"levels={texture.GetPixel(LightCount - light - 1, 6)} flags={texture.GetPixel(LightCount - light - 1, 7)} " +
                            $"ease={texture.GetPixel(LightCount - light - 1, 8)}");
                    }
                    AssertColorRgb(pixel, expectedPixel, 0.02f,
                        $"light={light} beat={beat}: the head strip must match the preview light.");
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(lightMaterial);
            }
        }

        // Record the production light output without a renderer or audio clock dependency.
        private class RecordingLight : LightController
        {
            protected override bool Initialize() => true;
            public override void SetColor(Color color) => Color = color;
        }
    }
}
