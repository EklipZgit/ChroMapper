using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// Strobe-channel color-distribution composition on the real LightColorGroupEffect pipeline:
    /// an explicit customData strobeColor is shifted only by strobeColorDistributions, an omitted
    /// strobeColor with any authored strobeColorDistributions (box or event) starts from the raw
    /// normal base (custom color or OEM) and applies only the strobe instructions, and an omitted
    /// strobeColor with NO strobeColorDistributions at all inherits the fully normal-distributed
    /// color. Box- and event-scoped instructions must produce identical outcomes, and saturation
    /// offsets must clamp once after each channel's box+event chain so cancelling offsets on a .5
    /// base keep .5 on both light channels.
    /// </summary>
    public class GLSColorDistributionStrobeInheritanceTest : GLSColorPlaybackTestBase
    {
        // The authored node sits at beat 0 of a single all-light box, so SongTime offsets stay
        // relative to its segment start and f=2 puts beat +0.125 on the normal half-cycle and
        // +0.25 on the strobe half-cycle.
        private const float EventBeat = 0f;

        // Distinct hue offsets keep normal, strobe, and composed endpoints on different hue rays
        // so a misrouted instruction can never produce the expected color by coincidence.
        private const float NormalHueOffset = 0.25f;
        private const float StrobeHueOffset = 0.5f;

        private static readonly Color CustomColor = new(0.9f, 1f, 0.5f); // h=0.2, s=0.5, v=1
        private static readonly Color OemColor = new(0.2f, 0.5f, 1f); // event c:1 resolves through Blue
        private static readonly Color ExplicitStrobeColor = new(1f, 0.75f, 0.5f); // s=0.5

        public enum HueDistributions
        {
            NormalOnly,
            StrobeOnly,
            NormalAndStrobe
        }

        // StrobeChannelIgnoresNormalDistributionsOnceStrobeDistributionsExist crosses the
        // four authored override combinations (customData color and strobeColor present/absent, the
        // absent color exercising the OEM scheme fallback) with all three distribution combinations at
        // both scopes, so box- and event-scoped instructions assert identical outcomes.
        private static IEnumerable<TestCaseData> StrobeInheritanceCases()
        {
            foreach (var customColor in new[] { false, true })
            foreach (var strobeColor in new[] { false, true })
            foreach (var distributions in new[]
                     {
                         HueDistributions.NormalOnly,
                         HueDistributions.StrobeOnly,
                         HueDistributions.NormalAndStrobe
                     })
            foreach (var boxScope in new[] { false, true })
            {
                yield return new TestCaseData(customColor, strobeColor, distributions, boxScope)
                    .SetName(
                        "StrobeInheritance_"
                        + (customColor ? "customColor" : "oemColor")
                        + (strobeColor ? "_explicitStrobe" : "_inheritedStrobe")
                        + $"_{distributions}_{(boxScope ? "box" : "event")}");
            }
        }

        // Without an explicit strobeColor the strobe phase inherits the normal-distributed color only
        // when NO strobeColorDistributions exist at either scope; the moment any strobe instruction is
        // authored the strobe channel starts from the raw normal base and applies only its own
        // instructions, so normal distributions must never leak into it.
        [TestCaseSource(nameof(StrobeInheritanceCases))]
        public void StrobeChannelIgnoresNormalDistributionsOnceStrobeDistributionsExist(
            bool customColor,
            bool explicitStrobeColor,
            HueDistributions distributions,
            bool boxScope)
        {
            var normal = distributions == HueDistributions.StrobeOnly
                ? null
                : new[] { $"h,{NormalHueOffset},L" };
            var strobe = distributions == HueDistributions.NormalOnly
                ? null
                : new[] { $"h,{StrobeHueOffset},L" };

            LoadPlayback(BuildMapJson(
                customColor,
                explicitStrobeColor,
                boxScope ? normal : null,
                boxScope ? strobe : null,
                boxScope ? null : normal,
                boxScope ? null : strobe));
            PinOemFallbackColor();

            var baseColor = customColor ? CustomColor : OemColor;
            var expectedNormal = OffsetHsv(
                baseColor,
                distributions == HueDistributions.StrobeOnly ? 0f : NormalHueOffset,
                0f);
            // An authored strobeColor or any strobeColorDistributions own the strobe channel outright
            // (explicit color wins, else the raw normal base); only a node with neither falls back to
            // the fully normal-distributed endpoint.
            var hasStrobeDistributions = distributions != HueDistributions.NormalOnly;
            var strobeBase = explicitStrobeColor
                ? ExplicitStrobeColor
                : hasStrobeDistributions ? baseColor : expectedNormal;
            var expectedStrobe = OffsetHsv(
                strobeBase,
                hasStrobeDistributions ? StrobeHueOffset : 0f,
                0f);

            AssertDistributedEndpoints(
                Node(0),
                expectedNormal,
                expectedStrobe,
                baseColor,
                explicitStrobeColor ? ExplicitStrobeColor : baseColor);
        }

        // Cancelling saturation offsets must net to the authored .5 on the normal channel: a
        // per-instruction clamp pins the intermediate 1.5 or -0.5 to 1 or 0 and loses the
        // cancellation. The strobe channel asserts the same .5 through normal-chain inheritance
        // because neither an explicit strobeColor nor any strobeColorDistributions are authored.
        [TestCase("s,1,L", "s,-1,L")]
        [TestCase("s,-1,L", "s,1,L")]
        public void CancelledBoxAndEventSaturationOffsetsClampOnceOnBothChannels(
            string boxInstruction,
            string eventInstruction)
        {
            LoadPlayback(BuildMapJson(
                customColor: true,
                explicitStrobeColor: false,
                boxColorDistributions: new[] { boxInstruction },
                boxStrobeColorDistributions: null,
                eventColorDistributions: new[] { eventInstruction },
                eventStrobeColorDistributions: null));

            AssertDistributedEndpoints(Node(0), CustomColor, CustomColor, CustomColor, CustomColor);
        }

        // CrossScopeSaturationOffsetsClampIndependentlyWhenStrobeDistributionsExist covers normal and
        // strobe saturation offsets authored at DIFFERENT scopes (box normal + event strobe, and the
        // inverse scope split). Once any strobeColorDistributions exist the strobe channel ignores the
        // normal chain entirely, so each endpoint is base + its own clamped offset rather than a
        // shared four-list chain that would cancel back to .5.
        [TestCase(1f, -1f, false)]
        [TestCase(-1f, 1f, false)]
        [TestCase(-1f, 1f, true)]
        [TestCase(1f, -1f, true)]
        public void CrossScopeSaturationOffsetsClampIndependentlyWhenStrobeDistributionsExist(
            float normalOffset,
            float strobeOffset,
            bool normalOnEventScope)
        {
            var normalInstruction = FormattableString.Invariant($"s,{normalOffset},L");
            var strobeInstruction = FormattableString.Invariant($"s,{strobeOffset},L");
            LoadPlayback(BuildMapJson(
                customColor: true,
                explicitStrobeColor: false,
                boxColorDistributions: normalOnEventScope ? null : new[] { normalInstruction },
                boxStrobeColorDistributions: normalOnEventScope ? new[] { strobeInstruction } : null,
                eventColorDistributions: normalOnEventScope ? new[] { normalInstruction } : null,
                eventStrobeColorDistributions: normalOnEventScope ? null : new[] { strobeInstruction }));

            // Independent oracles: the normal channel clamps its own offset alone, and the strobe
            // channel clamps its own offset over the raw base — never the other channel's chain.
            var expectedNormal = OffsetHsv(CustomColor, 0f, normalOffset);
            var expectedStrobe = OffsetHsv(CustomColor, 0f, strobeOffset);
            AssertDistributedEndpoints(
                Node(0), expectedNormal, expectedStrobe, CustomColor, CustomColor);
        }

        // FadedStrobePhaseRendersShiftedOemColorAtStrobeBrightness reproduces the supplied
        // two-node segment: an OEM c:1 node carrying inert a/b customData keys plus an event-scope
        // hue distribution, then a customColor node — both f=1 sb=0.2 sf=1 i=0 with no strobeColor
        // and no event-scope strobeColorDistributions, serialized on group 2. The playback harness
        // feeds every parsed group to its single recording effect rather than standing up a real
        // per-group manager, so g:2 exercises the same code path as g:1 here. One deliberate
        // divergence from the supplied map: the supplied events author NO strobeColorDistributions
        // anywhere; this fixture adds an equal box-scope h+0.3 strobe instruction to exercise the
        // channel-replacement rule (any parsed strobeColorDistributions makes the strobe channel
        // start from the raw base and ignore normal distributions). The sf=1 fade trough at +0.5
        // beat must then display the same h+0.3-shifted RGB as the primary — the wrong displayed
        // color would show a double-shifted hue here (normal + strobe offsets chained) — while the
        // phase-0 frame stays fully lit.
        [Test]
        public void FadedStrobePhaseRendersShiftedOemColorAtStrobeBrightness()
        {
            LoadPlayback("{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[{\"b\":0,\"g\":2,\"e\":[{"
                + "\"f\":{\"f\":1,\"p\":1},\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,"
                + "\"customData\":{\"strobeColorDistributions\":[\"h,0.3,L,l\"]},\"e\":["
                + "{\"b\":13,\"c\":1,\"s\":1,\"i\":0,\"f\":1,\"sb\":0.2,\"sf\":1,\"customData\":{\"a\":1,\"b\":1,\"colorDistributions\":[\"h,0.3,L,l\"]}},"
                + "{\"b\":15,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":0.2,\"sf\":1,\"customData\":{\"color\":[0,1,0.197]}}]}]}]}");
            PinOemFallbackColor();

            // Independent oracle: the hue distribution shifts the OEM base on the normal channel,
            // and the box-scope strobe distribution shifts the RAW OEM base by the same amount on
            // the strobe channel — one offset each, never a composed double shift.
            var expectedOemShifted = OffsetHsv(OemColor, 0.3f, 0f);
            var expectedCustom = new Color(0f, 1f, 0.197f);
            // The box-scope strobe distribution applies to both events, so the customColor node's
            // strobe trough is its authored color shifted by h+0.3 as well.
            var expectedCustomStrobe = OffsetHsv(expectedCustom, 0.3f, 0f);

            // Capture every live sample first so the state/tween reads below cannot be disturbed by
            // UpdateTime re-entering through a later ColorAt call.
            var lastStrobeTrough = ColorAt(LightCount - 1, 13.5f);
            var lastNormalPhase = ColorAt(LightCount - 1, 14f);
            var customStrobeTrough = ColorAt(LightCount - 1, 15.5f);
            var customNormalPhase = ColorAt(LightCount - 1, 16f);
            var firstStrobeTrough = ColorAt(0, 13.5f);
            var firstNormalPhase = ColorAt(0, 14f);

            playback.UpdateTime(false, SongTime(13.5f));
            var lastState = StateAt(LightCount - 1, 13.5f);
            var firstState = StateAt(0, 13.5f);
            var tween = containers[LightCount - 1].Tween;
            Debug.Log(
                $"[GLSStrobeRepro] progress last={lastState.AffectedLightProgress} first={firstState.AffectedLightProgress} "
                + $"| tween start={tween.StartColor} end={tween.EndColor} startStrobe={tween.StartStrobeColor} "
                + $"endStrobe={tween.EndStrobeColor} f={tween.StartStrobeFrequency} sb={tween.StartStrobeBrightness} sf={tween.StrobeFade} "
                + $"| rendered last@13.5={lastStrobeTrough} last@14={lastNormalPhase} first@13.5={firstStrobeTrough} first@14={firstNormalPhase} "
                + $"custom@15.5={customStrobeTrough} custom@16={customNormalPhase}");

            Assert.That(lastState.AffectedLightProgress, Is.EqualTo(1f).Within(0.001f),
                "the all-light filter must put the last physical light at progress 1");
            Assert.That(firstState.AffectedLightProgress, Is.EqualTo(0f).Within(0.001f),
                "the all-light filter must put the first physical light at progress 0");
            Assert.That(tween.StartStrobeFrequency, Is.GreaterThan(0f),
                "the authored f=1 must keep the strobe tween live instead of collapsing");
            AssertColor(tween.StartColor, expectedOemShifted, 0.001f,
                "OEM node normal endpoint carries the event hue distribution");
            AssertColor(tween.StartStrobeColor, expectedOemShifted, 0.001f,
                "the strobe endpoint is raw OEM + box strobe hue shift — the same RGB once, not a composed double shift");

            AssertColorRgb(lastStrobeTrough, expectedOemShifted, 0.01f,
                "the sf=1 fade trough must display the hue-shifted OEM RGB, not a double-shifted hue");
            Assert.That(lastStrobeTrough.a, Is.EqualTo(0.2f).Within(0.02f),
                "the fade trough dims the last light to the authored sb=0.2");
            AssertColorRgb(lastNormalPhase, expectedOemShifted, 0.01f,
                "the normal phase keeps the hue-shifted OEM RGB");
            Assert.That(lastNormalPhase.a, Is.EqualTo(1f).Within(0.02f),
                "the normal phase stays fully lit");
            AssertColorRgb(firstStrobeTrough, OemColor, 0.01f,
                "progress-0 first light receives no hue offset even at the strobe trough");
            Assert.That(firstStrobeTrough.a, Is.EqualTo(0.2f).Within(0.02f));
            Assert.That(firstNormalPhase.a, Is.EqualTo(1f).Within(0.02f));

            AssertColorRgb(customStrobeTrough, expectedCustomStrobe, 0.01f,
                "the customColor node's strobe trough renders the box strobe shift over its raw authored color");
            Assert.That(customStrobeTrough.a, Is.EqualTo(0.2f).Within(0.02f));
            AssertColorRgb(customNormalPhase, expectedCustom, 0.01f,
                "the customColor node's normal phase renders its authored color");
            Assert.That(customNormalPhase.a, Is.EqualTo(1f).Within(0.02f));
        }

        // CancelledStrobeSaturationOffsetsClampOnceOnExplicitStrobeColor proves the deferred clamp is
        // not a normal-channel-only fix: the same split-offset chain authored on
        // strobeColorDistributions must net .5 on a .5-saturation strobeColor while the normal
        // channel stays untouched by strobe instructions.
        [TestCase("s,1,L", "s,-1,L")]
        [TestCase("s,-1,L", "s,1,L")]
        public void CancelledStrobeSaturationOffsetsClampOnceOnExplicitStrobeColor(
            string boxInstruction,
            string eventInstruction)
        {
            LoadPlayback(BuildMapJson(
                customColor: true,
                explicitStrobeColor: true,
                boxColorDistributions: null,
                boxStrobeColorDistributions: new[] { boxInstruction },
                eventColorDistributions: null,
                eventStrobeColorDistributions: new[] { eventInstruction }));

            AssertDistributedEndpoints(
                Node(0), CustomColor, ExplicitStrobeColor, CustomColor, ExplicitStrobeColor);
        }

        // The OEM fallback resolves through the active color scheme on the playback path and through
        // EventAppearanceSO on the preview/timeline path; pinning both to one constant keeps a single
        // independent oracle. Refresh re-resolves the baked tween colors like LoadAlternatingChunks.
        private void PinOemFallbackColor()
        {
            playback.ColorSchemeProvider.ColorScheme.EnvironmentRightColor = OemColor;
            appearance.BlueColor = OemColor;
            playback.Refresh();
        }

        // The authored all-light division filter puts the last physical light at affected progress 1
        // and the first at 0. Playback tween endpoints, the rendered recording light at both strobe
        // phases, the ribbon preview endpoint cache, and the shared timeline tween must all agree.
        private void AssertDistributedEndpoints(
            BaseLightColorBase source,
            Color expectedNormalLast,
            Color expectedStrobeLast,
            Color expectedNormalFirst,
            Color expectedStrobeFirst)
        {
            playback.UpdateTime(false, SongTime(EventBeat + 0.5f));
            var lastTween = containers[LightCount - 1].Tween;
            var firstTween = containers[0].Tween;
            Assert.That(
                lastTween.StartStrobeFrequency,
                Is.GreaterThan(0f),
                "the authored f=2 must keep the strobe tween live instead of collapsing to the normal color");
            AssertColor(lastTween.StartColor, expectedNormalLast, 0.001f, "playback last-light normal endpoint");
            AssertColor(lastTween.StartStrobeColor, expectedStrobeLast, 0.001f, "playback last-light strobe endpoint");
            AssertColor(firstTween.StartColor, expectedNormalFirst, 0.001f, "playback first-light normal endpoint");
            AssertColor(firstTween.StartStrobeColor, expectedStrobeFirst, 0.001f, "playback first-light strobe endpoint");

            AssertColor(
                ColorAt(LightCount - 1, EventBeat + 0.125f),
                expectedNormalLast,
                0.005f,
                "rendered last-light normal phase");
            AssertColor(
                ColorAt(LightCount - 1, EventBeat + 0.25f),
                expectedStrobeLast,
                0.005f,
                "rendered last-light strobe phase");

            var mainColors = new Color[LightCount];
            var strobeColors = new Color[LightCount];
            Assert.That(
                GLSEventCommon.PopulateColorTransitionEndpoint(
                    source, LightCount, false, appearance, mainColors, strobeColors),
                Is.True,
                "distributed nodes must light the per-light endpoint cache");
            AssertColor(mainColors[LightCount - 1], expectedNormalLast, 0.001f, "preview last-light normal");
            AssertColor(strobeColors[LightCount - 1], expectedStrobeLast, 0.001f, "preview last-light strobe");
            AssertColor(mainColors[0], expectedNormalFirst, 0.001f, "preview first-light normal");
            AssertColor(strobeColors[0], expectedStrobeFirst, 0.001f, "preview first-light strobe");

            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.TryGetOutgoing(source, LightCount - 1, out var timelineState), Is.True);
            var timelineTween = new LightColorTween();
            timeline.ConfigureTween(timelineTween, timelineState, appearance, _ => false);
            AssertColor(timelineTween.StartColor, expectedNormalLast, 0.001f, "timeline last-light normal");
            AssertColor(timelineTween.StartStrobeColor, expectedStrobeLast, 0.001f, "timeline last-light strobe");
        }

        // Real V3 parsing: one all-light division-filtered box whose single b=0 node carries the
        // authored customData; f=2/sb=1/sf=0 keeps a live hard-strobe tween so the strobe endpoint
        // can never collapse into the normal color.
        private static string BuildMapJson(
            bool customColor,
            bool explicitStrobeColor,
            string[] boxColorDistributions,
            string[] boxStrobeColorDistributions,
            string[] eventColorDistributions,
            string[] eventStrobeColorDistributions)
        {
            var boxCustomData = new List<string>();
            AddDistributionsJson(boxCustomData, "colorDistributions", boxColorDistributions);
            AddDistributionsJson(boxCustomData, "strobeColorDistributions", boxStrobeColorDistributions);
            var eventCustomData = new List<string>();
            if (customColor)
            {
                eventCustomData.Add("\"color\":[0.9,1,0.5]");
            }
            if (explicitStrobeColor)
            {
                eventCustomData.Add("\"strobeColor\":[1,0.75,0.5]");
            }
            AddDistributionsJson(eventCustomData, "colorDistributions", eventColorDistributions);
            AddDistributionsJson(eventCustomData, "strobeColorDistributions", eventStrobeColorDistributions);
            return "{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[{\"b\":0,\"g\":1,\"e\":[{\"f\":{\"f\":1,\"p\":1},\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,\"customData\":{"
                + string.Join(",", boxCustomData)
                + "},\"e\":[{\"b\":0,\"c\":1,\"s\":1,\"i\":1,\"f\":2,\"sb\":1,\"sf\":0,\"customData\":{"
                + string.Join(",", eventCustomData)
                + "}}]}]}]}";
        }

        private static void AddDistributionsJson(List<string> customData, string key, string[] instructions)
        {
            if (instructions == null || instructions.Length == 0)
            {
                return;
            }

            customData.Add($"\"{key}\":[{string.Join(",", instructions.Select(value => $"\"{value}\""))}]");
        }

        // Independent HSV oracle: hue wraps and saturation clamps once after every authored offset is
        // applied, evaluated through Unity's color math without calling GLSColorDistribution.
        private static Color OffsetHsv(Color source, float hueOffset, float saturationOffset)
        {
            Color.RGBToHSV(source, out var hue, out var saturation, out var value);
            var result = Color.HSVToRGB(
                Mathf.Repeat(hue + hueOffset, 1f),
                Mathf.Clamp01(saturation + saturationOffset),
                value,
                true);
            result.a = source.a;
            return result;
        }
    }
}
