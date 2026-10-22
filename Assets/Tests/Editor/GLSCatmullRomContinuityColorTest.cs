using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;

namespace Tests.Editor
{
    /// <summary>
    ///     Color-channel continuity regressions for time-scaled Catmull-Rom tangents through
    ///     LightColorGroupEffect and the shared GLSColorTimeline preparation used by ribbons. The frozen
    ///     map has no Catmull-Rom color content, so these use small production-loaded groups through the
    ///     same spline helper. Nodes arrive at uneven beats so the spans meeting at a node differ in
    ///     duration.
    /// </summary>
    public class GLSCatmullRomContinuityColorTest : GLSColorPlaybackTestBase
    {
        // Gray ramp [0, .2, .6, 0] at beats 0, 1, 2.5, 5. The destination nodes at 2.5 and 5 carry the
        // interval easing so both segments into and out of 2.5 run splines on every channel.
        private static string GrayRampMapJson(int easing) =>
            "{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[" +
            "{\"b\":0,\"g\":1,\"e\":[{\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
            "\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,\"e\":[" +
            "{\"b\":0,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[0,0,0]}}," +
            "{\"b\":1,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[0.2,0.2,0.2]}}," +
            "{\"b\":2.5,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0.6,0.6,0.6],\"easing\":" + easing + "}}," +
            "{\"b\":5,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"easing\":" + easing + "}}]}]}]}";

        // Brightness track [.2, .9, .4, .7] on white nodes; only the alpha channel moves.
        private static string BrightnessMapJson(int easing) =>
            "{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[" +
            "{\"b\":0,\"g\":1,\"e\":[{\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
            "\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,\"e\":[" +
            "{\"b\":0,\"c\":0,\"s\":0.2,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[1,1,1]}}," +
            "{\"b\":1,\"c\":0,\"s\":0.9,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[1,1,1]}}," +
            "{\"b\":2.5,\"c\":0,\"s\":0.4,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[1,1,1],\"easing\":" + easing + "}}," +
            "{\"b\":5,\"c\":0,\"s\":0.7,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[1,1,1],\"easing\":" + easing + "}}]}]}]}";

        // Frequency-1 hard strobe; strobe colors, strobe brightness, and normal brightness all share
        // [.2, .9, .4, .7] so the on-phase output checks the strobe color and strobe alpha splines while
        // the prepared tangent assertions cover every splined channel.
        private static string StrobeMapJson(int easing) =>
            "{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[" +
            "{\"b\":0,\"g\":1,\"e\":[{\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
            "\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,\"e\":[" +
            "{\"b\":0,\"c\":0,\"s\":0.2,\"i\":1,\"f\":1,\"sb\":0.2,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"strobeColor\":[0.2,0.2,0.2]}}," +
            "{\"b\":1,\"c\":0,\"s\":0.9,\"i\":1,\"f\":1,\"sb\":0.9,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"strobeColor\":[0.9,0.9,0.9]}}," +
            "{\"b\":2.5,\"c\":0,\"s\":0.4,\"i\":1,\"f\":1,\"sb\":0.4,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"strobeColor\":[0.4,0.4,0.4],\"easing\":" + easing + "}}," +
            "{\"b\":5,\"c\":0,\"s\":0.7,\"i\":1,\"f\":1,\"sb\":0.7,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"strobeColor\":[0.7,0.7,0.7],\"easing\":" + easing + "}}]}]}]}";

        // Chained holds at beats 2 and 3 resolve back to the beat-1 value, but the segment into beat 4
        // must take its left timestamp from the real predecessor (held node at 2), not the resolved
        // value source at beat 1.
        private static string HeldChainMapJson =>
            "{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[" +
            "{\"b\":0,\"g\":1,\"e\":[{\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
            "\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,\"e\":[" +
            "{\"b\":0,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[0,0,0]}}," +
            "{\"b\":1,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[0.2,0.2,0.2]}}," +
            "{\"b\":2,\"c\":0,\"s\":1,\"i\":2,\"f\":0,\"sb\":0,\"sf\":0}," +
            "{\"b\":3,\"c\":0,\"s\":1,\"i\":2,\"f\":0,\"sb\":0,\"sf\":0}," +
            "{\"b\":4,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0.6,0.6,0.6],\"easing\":103}}," +
            "{\"b\":5,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"easing\":103}}]}]}]}";

        // Node 1 owns its 105 color easing; node 2.5 arrives on 104. The shared tangent at 1 must use
        // the node's own chordal profile even though the segment destination is centripetal.
        private static string MixedProfileMapJson =>
            "{\"version\":\"3.3.0\",\"lightColorEventBoxGroups\":[" +
            "{\"b\":0,\"g\":1,\"e\":[{\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
            "\"w\":0,\"d\":1,\"r\":0,\"t\":1,\"b\":0,\"i\":0,\"e\":[" +
            "{\"b\":0,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[0,0,0]}}," +
            "{\"b\":1,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0.2,0.2,0.2],\"colorEasing\":105}}," +
            "{\"b\":2.5,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0.6,0.6,0.6],\"colorEasing\":104}}," +
            "{\"b\":5,\"c\":0,\"s\":1,\"i\":1,\"f\":0,\"sb\":0,\"sf\":0," +
            "\"customData\":{\"color\":[0,0,0],\"colorEasing\":104}}]}]}]}";

        [TestCase(GLSCatmullRomIds.Uniform, 0.454375f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 0.442426f)]
        [TestCase(GLSCatmullRomIds.Chordal, 0.435625f)]
        public void UnequalSpacingNormalColorSharesNodeVelocity(int easing, float expectedGray)
        {
            LoadPlayback(GrayRampMapJson(easing));
            Assert.That(
                ColorAt(0, 1.75f).r,
                Is.EqualTo(expectedGray).Within(1e-3f),
                "mid-segment color");

            var (incoming, outgoing) = GLSCatmullRomContinuityTest.NodeVelocities(
                beat => ColorAt(0, beat).r,
                GLSCatmullRomContinuityTest.Subtract,
                2.5f,
                1f,
                5f);
            Assert.That(outgoing, Is.EqualTo(incoming).Within(0.01f), "C1 at beat 2.5");
        }

        [TestCase(GLSCatmullRomIds.Uniform, 0.674375f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 0.65f)]
        [TestCase(GLSCatmullRomIds.Chordal, 0.625625f)]
        public void UnequalSpacingBrightnessAlphaSharesNodeVelocity(int easing, float expectedAlpha)
        {
            LoadPlayback(BrightnessMapJson(easing));
            Assert.That(
                ColorAt(0, 1.75f).a,
                Is.EqualTo(expectedAlpha).Within(1e-3f),
                "mid-segment alpha");

            var (incoming, outgoing) = GLSCatmullRomContinuityTest.NodeVelocities(
                beat => ColorAt(0, beat).a,
                GLSCatmullRomContinuityTest.Subtract,
                2.5f,
                1f,
                5f);
            Assert.That(outgoing, Is.EqualTo(incoming).Within(0.01f), "alpha C1 at beat 2.5");
        }

        // The final strobe output jumps between pulses by design, so continuity here is proven on the
        // prepared per-channel tangents: each node must hand the adjoining tweens the same velocity,
        // which is the stored Hermite m divided by that segment's real duration.
        [TestCase(GLSCatmullRomIds.Uniform)]
        [TestCase(GLSCatmullRomIds.Centripetal)]
        [TestCase(GLSCatmullRomIds.Chordal)]
        public void UnequalSpacingStrobeChannelsShareNodeVelocity(int easing)
        {
            LoadPlayback(StrobeMapJson(easing));
            // Beat 1.75 sits in the on-phase half of the frequency-1 pulse; the midpoint checks the
            // spline still lands on the evaluated output, not just the coefficients.
            var expected = easing == GLSCatmullRomIds.Centripetal
                ? 0.65f
                : easing == GLSCatmullRomIds.Chordal
                    ? 0.625625f
                    : 0.674375f;
            var color = ColorAt(0, 1.75f);
            Assert.That(color.r, Is.EqualTo(expected).Within(1e-3f), "on-phase strobe color");
            Assert.That(color.a, Is.EqualTo(expected).Within(1e-3f), "on-phase strobe alpha");

            var left = ConfiguredTween(Node(0, 0, 1));
            var right = ConfiguredTween(Node(0, 0, 2));
            Assert.That(left.UseAlphaSpline && right.UseAlphaSpline, Is.True);
            Assert.That(left.UseColorSpline && right.UseColorSpline, Is.True);
            Assert.That(left.UseStrobeColorSpline && right.UseStrobeColorSpline, Is.True);
            var leftDuration = left.EndTimeColor - left.StartTimeColor;
            var rightDuration = right.EndTimeColor - right.StartTimeColor;
            Assert.That(leftDuration, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(rightDuration, Is.EqualTo(2.5f).Within(1e-4f));
            Assert.That(
                right.AlphaSplineM1 / rightDuration,
                Is.EqualTo(left.AlphaSplineM2 / leftDuration).Within(1e-4f),
                "alpha velocity at beat 2.5");
            Assert.That(
                right.StrobeAlphaSplineM1 / rightDuration,
                Is.EqualTo(left.StrobeAlphaSplineM2 / leftDuration).Within(1e-4f),
                "strobe alpha velocity at beat 2.5");
            for (var i = 0; i < 4; i++)
            {
                Assert.That(
                    right.ColorSplineM1[i] / rightDuration,
                    Is.EqualTo(left.ColorSplineM2[i] / leftDuration).Within(1e-4f),
                    $"color velocity component {i} at beat 2.5");
                Assert.That(
                    right.StrobeColorSplineM1[i] / rightDuration,
                    Is.EqualTo(left.StrobeColorSplineM2[i] / leftDuration).Within(1e-4f),
                    $"strobe color velocity component {i} at beat 2.5");
            }
        }

        private LightColorTween ConfiguredTween(BaseLightColorBase source)
        {
            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.TryGetOutgoing(source, 0, out var state), Is.True);
            var tween = new LightColorTween();
            timeline.ConfigureTween(tween, state, 0, appearance, _ => false);
            return tween;
        }

        [TestCase(GLSCatmullRomIds.Uniform)]
        [TestCase(GLSCatmullRomIds.Centripetal)]
        [TestCase(GLSCatmullRomIds.Chordal)]
        public void UnequalSpacingRibbonMatchesLivePreview(int easing)
        {
            LoadPlayback(GrayRampMapJson(easing));
            AssertRibbonPixels(0, 0, 1, 1.25f, 0);
            AssertRibbonPixels(0, 0, 1, 1.75f, 0);
            AssertRibbonPixels(0, 0, 1, 2.25f, 0);
        }

        [Test]
        public void HeldChainKeepsActualNeighborTimestamp()
        {
            LoadPlayback(HeldChainMapJson);
            // Controls for the held-owned segment are .2@(real predecessor time 2), .2@3, .6@4, 0@5.
            // Uniform tangents .2/-.1 give .4375 at 3.5; resolving the value source's timestamp (beat 1)
            // instead of the actual held predecessor's would give .429167.
            Assert.That(ColorAt(0, 3.5f).r, Is.EqualTo(0.4375f).Within(1e-3f));
        }

        [Test]
        public void MixedProfilesKeepSharedVelocityAtOwnedNode()
        {
            LoadPlayback(MixedProfileMapJson);
            // Node 1's own 105 profile owns its tangent: chordal velocity .24 -> m1 .36; the centripetal
            // destination at 2.5 ends the segment with m2 = 0.
            Assert.That(ColorAt(0, 1.75f).r, Is.EqualTo(0.445f).Within(1e-3f));

            var atOne = GLSCatmullRomContinuityTest.NodeVelocities(
                beat => ColorAt(0, beat).r,
                GLSCatmullRomContinuityTest.Subtract,
                1f,
                0f,
                2.5f);
            Assert.That(atOne.Outgoing, Is.EqualTo(atOne.Incoming).Within(0.01f), "C1 at beat 1");
            Assert.That(atOne.Outgoing, Is.EqualTo(0.24f).Within(0.01f), "node-owned velocity at beat 1");

            var atTwoHalf = GLSCatmullRomContinuityTest.NodeVelocities(
                beat => ColorAt(0, beat).r,
                GLSCatmullRomContinuityTest.Subtract,
                2.5f,
                1f,
                5f);
            Assert.That(atTwoHalf.Outgoing, Is.EqualTo(atTwoHalf.Incoming).Within(0.01f), "C1 at beat 2.5");
        }
    }
}
