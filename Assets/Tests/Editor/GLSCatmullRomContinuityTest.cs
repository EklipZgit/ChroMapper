using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    /// <summary>
    ///     Runs the frozen "Catmullington Romothey the Third" capture (ExpertPlusStandard.dat, SHA256
    ///     8971d8a2c0c6a10ddd398c35dc6342eebdcd5f1a4021649768d16bdf652a07fc) through production V3
    ///     deserialization and LightRotationGroupEffect/LightTranslationGroupEffect event generation.
    ///     Its Catmull-Rom nodes sit at uneven beats across all three knot profiles, which reproduced
    ///     the reported seam discontinuities: a tangent normalized per segment cannot keep one velocity
    ///     at a node met by unequal spans. These tests read the actual generated per-light state chains
    ///     and assert the same time-scaled velocity approaches each interior node from both sides.
    /// </summary>
    public class GLSCatmullRomContinuityTest : TestBase
    {
        private const int GroupId = 8;
        private const int LightCount = 8;
        private static readonly Axis[] Axes = { Axis.X, Axis.Y, Axis.Z };

        private BaseDifficulty originalMap;
        private float originalBpm;
        private string fixtureFile;
        private BaseDifficulty map;
        private GameObject effectRoot;
        private LightRotationGroupEffect rotationEffect;
        private LightTranslationGroupEffect translationEffect;
        private Dictionary<(Axis, int), LightRotationGroupContainer> rotationContainers;
        private Dictionary<(Axis, int), LightTranslationGroupContainer> translationContainers;

        [SetUp]
        public void LoadFixture()
        {
            var song = BeatSaberSongContainer.Instance;
            originalMap = song.Map;
            originalBpm = song.Info.BeatsPerMinute;
            song.Info.BeatsPerMinute = 100f;
            // Tests tagged CatmullRomExtendedCapture load the newer live capture; untagged tests keep
            // the frozen fixture. The tag must sit on the running test itself so TestContext exposes it.
            fixtureFile = TestContext.CurrentContext.Test.Properties.Get("CatmullRomExtendedCapture")
                    as string == "true"
                ? "ExpertPlusStandard-Extended.dat"
                : "ExpertPlusStandard.dat";
            map = BeatmapFactory.GetDifficultyFromJson(
                JSON.Parse(File.ReadAllText(PathUtils.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    "Catmullington",
                    fixtureFile))),
                "Catmull-Rom continuity",
                song.Info,
                song.MapDifficultyInfo);
            song.Map = map;

            effectRoot = new GameObject(nameof(GLSCatmullRomContinuityTest));
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc, Is.Not.Null);

            rotationEffect = effectRoot.AddComponent<LightRotationGroupEffect>();
            rotationEffect.Atsc = atsc;
            rotationEffect.ID = GroupId;
            rotationEffect.Count = LightCount;

            translationEffect = effectRoot.AddComponent<LightTranslationGroupEffect>();
            translationEffect.Atsc = atsc;
            translationEffect.ID = GroupId;
            translationEffect.Count = LightCount;
            // Identity mapping keeps generated endpoints at authored translation plus distribution offset.
            translationEffect.TranslationLimits = new[]
            {
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f)
            };
            translationEffect.DistributionLimits = new[]
            {
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f)
            };

            foreach (var axis in Axes)
            {
                for (var light = 0; light < LightCount; light++)
                {
                    rotationEffect.Register(light, axis, false, CreateChild($"rotation {axis} {light}"));
                    translationEffect.Register(light, axis, false, CreateChild($"translation {axis} {light}"));
                }
            }

            rotationEffect.Initialize();
            translationEffect.Initialize();
            foreach (var group in map.LightRotationEventBoxGroups)
            {
                rotationEffect.InsertData(group);
            }
            foreach (var group in map.LightTranslationEventBoxGroups)
            {
                translationEffect.InsertData(group);
            }
            rotationEffect.Refresh();
            translationEffect.Refresh();

            rotationContainers = (Dictionary<(Axis, int), LightRotationGroupContainer>)
                typeof(LightRotationGroupEffect)
                    .GetField("idToContainer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(rotationEffect);
            translationContainers = (Dictionary<(Axis, int), LightTranslationGroupContainer>)
                typeof(LightTranslationGroupEffect)
                    .GetField("idToContainer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(translationEffect);
        }

        protected override void BeforeCleanup()
        {
            Object.DestroyImmediate(effectRoot);
            BeatSaberSongContainer.Instance.Map = originalMap;
            BeatSaberSongContainer.Instance.Info.BeatsPerMinute = originalBpm;
        }

        private Transform CreateChild(string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(effectRoot.transform, false);
            return child;
        }

        private static List<TState> RealStates<TState, TData>(StateChunksContainer<TState, TData> container)
            where TState : EventGroupEventStateData<TData>
            where TData : BaseGLSEvent
        {
            var states = new List<TState>();
            foreach (var state in container.Collection)
            {
                if (state.StartTime != short.MinValue && state.StartTime != float.MaxValue)
                {
                    states.Add(state);
                }
            }

            states.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            return states;
        }

        private static void AssertStateTimes<TState, TData>(
            StateChunksContainer<TState, TData> container,
            float[] expected,
            string message)
            where TState : EventGroupEventStateData<TData>
            where TData : BaseGLSEvent
        {
            var states = RealStates(container);
            Assert.That(states.Count, Is.EqualTo(expected.Length), $"{message}: generated node count");
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(
                    states[i].StartTime,
                    Is.EqualTo(expected[i]).Within(1e-4f),
                    $"{message}: node {i} time");
            }
        }

        private float TranslationAt(Axis axis, int light, float beat)
        {
            translationEffect.UpdateTime(false, beat);
            return translationContainers[(axis, light)].Tween.Current;
        }

        private float RotationAt(Axis axis, int light, float beat)
        {
            rotationEffect.UpdateTime(false, beat);
            return rotationContainers[(axis, light)].Tween.Current;
        }

        private static readonly FieldInfo FloatTweenSplineM1 =
            typeof(FloatTween).GetField("splineM1", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FloatTweenSplineM2 =
            typeof(FloatTween).GetField("splineM2", BindingFlags.Instance | BindingFlags.NonPublic);

        // The C1 contract is that both segments touching the node carry the same velocity. Reading the
        // prepared tangents off each side's tween is exact; differencing ~300-degree Tween.Current
        // values instead drowns the comparison in float32 noise.
        private (float Incoming, float Outgoing) RotationNodeVelocities(Axis axis, int light, float node)
        {
            var tween = rotationContainers[(axis, light)].Tween;
            rotationEffect.UpdateTime(false, node - 1e-3f);
            Assert.That(tween.UseSpline, Is.True, $"rotation {axis} light {light} segment into {node} must spline");
            var incoming =
                (float)FloatTweenSplineM2.GetValue(tween) / (tween.EndTime - tween.StartTime);
            rotationEffect.UpdateTime(false, node + 1e-3f);
            Assert.That(tween.UseSpline, Is.True, $"rotation {axis} light {light} segment out of {node} must spline");
            var outgoing =
                (float)FloatTweenSplineM1.GetValue(tween) / (tween.EndTime - tween.StartTime);
            return (incoming, outgoing);
        }

        // Second-order one-sided derivatives so the measured slope sees the spline reaching the node,
        // not a symmetric difference that would already smear a kink away.
        internal static (float Incoming, float Outgoing) NodeVelocities(
            Func<float, float> sample,
            Func<float, float, float> delta,
            float time,
            float previousTime,
            float nextTime)
        {
            var h = Mathf.Min(0.01f, Mathf.Min(time - previousTime, nextTime - time) * 0.005f);
            var at = sample(time);
            var incoming = ((-4f * delta(at, sample(time - h))) + delta(at, sample(time - (2f * h))))
                / (2f * h);
            var outgoing = ((4f * delta(at, sample(time + h))) - delta(at, sample(time + (2f * h))))
                / (2f * h);
            return (incoming, outgoing);
        }

        internal static float Subtract(float from, float to) => to - from;

        [Test]
        public void GeneratedStateChainsMatchExpectedNodeTimes()
        {
            var transformG0Times = new[] { 1.5f, 6f, 9f, 11.5f, 13.5f, 15.5f };
            var rotationTimes = new[] { 1.5f, 6f, 12f, 14.5f, 18f, 20.5f, 22f, 23f };
            for (var light = 0; light < LightCount; light++)
            {
                AssertStateTimes(
                    translationContainers[(Axis.X, light)].EventContainer,
                    transformG0Times,
                    $"translation X light {light}");
                AssertStateTimes(
                    translationContainers[(Axis.Y, light)].EventContainer,
                    transformG0Times,
                    $"translation Y light {light}");
                AssertStateTimes(
                    rotationContainers[(Axis.X, light)].EventContainer,
                    rotationTimes,
                    $"rotation X light {light}");
                AssertStateTimes(
                    rotationContainers[(Axis.Y, light)].EventContainer,
                    rotationTimes,
                    $"rotation Y light {light}");
            }

            AssertStateTimes(
                translationContainers[(Axis.Z, 0)].EventContainer,
                new[] { 1.5f, 6f, 9f, 11.5f, 13.5f, 15.5f, 22f, 23f, 24f, 25f, 26f },
                "translation Z light 0");
            for (var light = 1; light < LightCount; light++)
            {
                AssertStateTimes(
                    translationContainers[(Axis.Z, light)].EventContainer,
                    new[] { 1.5f, 6f, 9f, 11.5f, 13.5f, 15.5f, 22f, 23f, 25f, 26f },
                    $"translation Z light {light}");
            }

            AssertStateTimes(
                rotationContainers[(Axis.Z, 0)].EventContainer,
                new[] { 1.5f, 6f, 12f, 14.5f, 18f, 20.5f, 22f, 23f, 24f, 25f, 26f },
                "rotation Z light 0");
            // The beat-24 box has no beat distribution so every light keeps it; only the w=10 wave at
            // beat 25 pushes later orders past the group-26 claim and clips them.
            for (var light = 1; light < LightCount; light++)
            {
                AssertStateTimes(
                    rotationContainers[(Axis.Z, light)].EventContainer,
                    new[] { 1.5f, 6f, 12f, 14.5f, 18f, 20.5f, 22f, 23f, 24f, 26f },
                    $"rotation Z light {light}");
            }
        }

        [Test]
        public void TranslationInteriorNodesShareOneVelocity()
        {
            var checkedNodes = 0;
            // X and Z must carry at least one genuinely moving interior node so an implementation that
            // forced every shared tangent to zero could not pass by flattening the whole fixture. Y's
            // interior nodes sit at repeats or local extrema where the shared velocity is genuinely zero.
            var axisMotion = new Dictionary<Axis, float>();
            foreach (var axis in Axes)
            {
                for (var light = 0; light < LightCount; light++)
                {
                    var container = translationContainers[(axis, light)].EventContainer;
                    var states = RealStates(container);
                    for (var i = 1; i < states.Count - 1; i++)
                    {
                        var node = states[i];
                        var previous = states[i - 1];
                        var next = states[i + 1];
                        var incomingSplines = !node.UsePrevious
                            && Easing.IsCatmullRom(node.Base.Easing);
                        var outgoingSplines = !next.UsePrevious
                            && Easing.IsCatmullRom(next.Base.Easing);
                        if (!incomingSplines || !outgoingSplines)
                        {
                            continue;
                        }

                        var (incoming, outgoing) = NodeVelocities(
                            time => TranslationAt(axis, light, time),
                            Subtract,
                            node.StartTime,
                            previous.StartTime,
                            next.StartTime);
                        var label = $"translation {axis} light {light} node {node.StartTime}";
                        Assert.That(
                            outgoing,
                            Is.EqualTo(incoming).Within(0.01f),
                            $"{label}: one node velocity must serve both segments " +
                            $"(incoming {incoming:R}, outgoing {outgoing:R})");
                        if (!axisMotion.ContainsKey(axis))
                        {
                            axisMotion[axis] = 0f;
                        }
                        axisMotion[axis] = Mathf.Max(axisMotion[axis], Mathf.Abs(outgoing));
                        checkedNodes++;
                    }
                }
            }

            Assert.That(checkedNodes, Is.GreaterThan(0));
            // X and Z must genuinely move somewhere so flattening every tangent to zero cannot pass.
            // Y's interior nodes are the authored .3,.3 repeat or local extrema, where a centripetal
            // profile's shared node velocity legitimately cancels to zero.
            Assert.That(axisMotion[Axis.X], Is.GreaterThan(0.01f), "translation X must move at an interior node");
            Assert.That(axisMotion[Axis.Z], Is.GreaterThan(0.01f), "translation Z must move at an interior node");
            Assert.That(
                axisMotion[Axis.Y],
                Is.LessThan(0.001f),
                "translation Y: repeats and local extrema have zero centripetal node velocity");
        }

        [Test]
        public void RotationInteriorNodesShareOneVelocity()
        {
            var checkedNodes = 0;
            foreach (var axis in Axes)
            {
                for (var light = 0; light < LightCount; light++)
                {
                    var states = RealStates(rotationContainers[(axis, light)].EventContainer);
                    for (var i = 1; i < states.Count - 1; i++)
                    {
                        var node = states[i];
                        var next = states[i + 1];
                        // Rotation splines through held nodes only when they still carry loop turns.
                        var incomingSplines = (!node.UsePrevious || node.Loop > 0)
                            && Easing.IsCatmullRom(node.Base.Easing);
                        var outgoingSplines = (!next.UsePrevious || next.Loop > 0)
                            && Easing.IsCatmullRom(next.Base.Easing);
                        if (!incomingSplines || !outgoingSplines)
                        {
                            continue;
                        }

                        var (incoming, outgoing) = RotationNodeVelocities(axis, light, node.StartTime);
                        Assert.That(
                            outgoing,
                            Is.EqualTo(incoming).Within(0.02f),
                            $"rotation {axis} light {light} node {node.StartTime}: one node velocity " +
                            $"must serve both segments (incoming {incoming:R}, outgoing {outgoing:R})");
                        checkedNodes++;
                    }
                }
            }

            Assert.That(checkedNodes, Is.GreaterThan(0));
        }

        [Test]
        public void TranslationXNodeAtBeatSixKeepsAuthoredVelocity()
        {
            // Authored X: 0.3 @ 1.5, -0.3 @ 6, -0.5 @ 9. The shared uniform velocity at the middle node is
            // (-0.5 - 0.3) / (9 - 1.5); per-segment normalization used to halve it differently each side.
            const float expected = (-0.5f - 0.300000012f) / (9f - 1.5f);
            for (var light = 0; light < LightCount; light++)
            {
                var (incoming, outgoing) = NodeVelocities(
                    time => TranslationAt(Axis.X, light, time),
                    Subtract,
                    6f,
                    1.5f,
                    9f);
                Assert.That(
                    outgoing,
                    Is.EqualTo(expected).Within(0.005f),
                    $"light {light} outgoing velocity at beat 6");
                Assert.That(
                    incoming,
                    Is.EqualTo(expected).Within(0.005f),
                    $"light {light} incoming velocity at beat 6");
            }
        }

        [Test]
        public void RepeatedTranslationYStaysFlatAndStopsBothSides()
        {
            // Y repeats 0.3 at beats 6 and 9 with centripetal knots: the zero chord zeroes the shared
            // velocity, so the middle stays put and both node ends arrive and leave flat.
            for (var light = 0; light < LightCount; light++)
            {
                Assert.That(
                    TranslationAt(Axis.Y, light, 7.5f),
                    Is.EqualTo(0.300000012f).Within(0.001f),
                    $"light {light} mid value");
                var atSix = NodeVelocities(
                    time => TranslationAt(Axis.Y, light, time),
                    Subtract,
                    6f,
                    1.5f,
                    9f);
                Assert.That(atSix.Outgoing, Is.EqualTo(0f).Within(0.01f), $"light {light} slope after beat 6");
                var atNine = NodeVelocities(
                    time => TranslationAt(Axis.Y, light, time),
                    Subtract,
                    9f,
                    6f,
                    11.5f);
                Assert.That(atNine.Incoming, Is.EqualTo(0f).Within(0.01f), $"light {light} slope into beat 9");
                Assert.That(atNine.Outgoing, Is.EqualTo(0f).Within(0.01f), $"light {light} slope after beat 9");
            }
        }

        [Test]
        public void TranslationZDistributedEndpointsMatchBoxContract()
        {
            // Group 22 Z: 0.3 + (-0.4) * InOutSin(order / 7) over all eight lights.
            Assert.That(
                TranslationAt(Axis.Z, 0, 22f),
                Is.EqualTo(0.300000012f).Within(1e-4f));
            Assert.That(
                TranslationAt(Axis.Z, 7, 22f),
                Is.EqualTo(-0.099999994f).Within(1e-4f));

            // Group 23 Z: -0.3 + 0.3 * InQuad(order / 7).
            Assert.That(
                TranslationAt(Axis.Z, 0, 23f),
                Is.EqualTo(-0.300000012f).Within(1e-4f));
            Assert.That(TranslationAt(Axis.Z, 7, 23f), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void RotationXKeepsSourceProfileVelocityAcrossProfileChange()
        {
            // Node 20.5 is authored uniform (e=103) inside a 103 sequence; the next node at 22 starts the
            // 105 groups. The shared tangent at 20.5 is owned by 20.5's profile: (180 - 195) / (22 - 18).
            var (incoming, outgoing) = RotationNodeVelocities(Axis.X, 0, 20.5f);
            Assert.That(outgoing, Is.EqualTo(-3.75f).Within(0.01f), "outgoing velocity at 20.5");
            Assert.That(incoming, Is.EqualTo(-3.75f).Within(0.01f), "incoming velocity at 20.5");
        }

        // A distributed sequence that is never clipped proves per-light staggering directly: the wave
        // X box spreads each relative beat by duration order while the step Y box spreads by order * w.
        [Test]
        public void StaggeredDistributionKeepsVelocityAtPerLightTimes()
        {
            InsertTranslationGroup(
                "{\"b\":40,\"g\":8,\"e\":[{" +
                "\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":4,\"d\":1,\"s\":0.3,\"t\":1,\"b\":1,\"i\":1,\"a\":0,\"r\":0,\"l\":[" +
                "{\"b\":0,\"p\":0,\"e\":103,\"t\":0}," +
                "{\"b\":0.5,\"p\":0,\"e\":104,\"t\":1}," +
                "{\"b\":2,\"p\":0,\"e\":105,\"t\":1.5}]}]}");
            InsertTranslationGroup(
                "{\"b\":41,\"g\":8,\"e\":[{" +
                "\"f\":{\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"c\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0.3,\"d\":2,\"s\":0.1,\"t\":2,\"b\":0,\"i\":1,\"a\":1,\"r\":0,\"l\":[" +
                "{\"b\":0,\"p\":0,\"e\":104,\"t\":0}," +
                "{\"b\":1,\"p\":0,\"e\":105,\"t\":-0.5}," +
                "{\"b\":3,\"p\":0,\"e\":103,\"t\":0.25}]}]}");

            // Wave X: beatStep = (4 - 2) / 7, value step 0.3 * InQuad(order / 7), affectFirst=1.
            const float waveStep = 2f / 7f;
            for (var light = 0; light < LightCount; light++)
            {
                var offset = 0.3f * Mathf.Pow(light / 7f, 2f);
                var baseTime = 40f + (light * waveStep);
                Assert.That(
                    TranslationAt(Axis.X, light, baseTime),
                    Is.EqualTo(offset).Within(1e-4f),
                    $"X light {light} first node");
                Assert.That(
                    TranslationAt(Axis.X, light, baseTime + 0.5f),
                    Is.EqualTo(1f + offset).Within(1e-4f),
                    $"X light {light} second node");
                Assert.That(
                    TranslationAt(Axis.X, light, baseTime + 2f),
                    Is.EqualTo(1.5f + offset).Within(1e-4f),
                    $"X light {light} last node");

                // Node 1 is owned by its 104 profile: sqrt-weighted velocity (sqrt(0.5) + 1/sqrt(0.5)*0.5)/2.
                var expectedOwned = ((Mathf.Sqrt(0.5f) * (1f - 0f))
                    + ((1f / Mathf.Sqrt(0.5f)) * (1.5f - 1f))) / 2f;
                var (incoming, outgoing) = NodeVelocities(
                    time => TranslationAt(Axis.X, light, time),
                    Subtract,
                    baseTime + 0.5f,
                    baseTime,
                    baseTime + 2f);
                Assert.That(
                    outgoing,
                    Is.EqualTo(expectedOwned).Within(0.01f),
                    $"X light {light} outgoing velocity at second node");
                Assert.That(
                    incoming,
                    Is.EqualTo(expectedOwned).Within(0.01f),
                    $"X light {light} incoming velocity at second node");
            }

            // Step Y: beatStep = 0.3 per order, value step 0.1 * 8 * InQuad(order / 8), affectFirst=0 so
            // only the first authored event carries no offset.
            for (var light = 0; light < LightCount; light++)
            {
                var offset = 0.8f * Mathf.Pow(light / 8f, 2f);
                var baseTime = 41f + (light * 0.3f);
                Assert.That(
                    TranslationAt(Axis.Y, light, baseTime),
                    Is.EqualTo(0f).Within(1e-4f),
                    $"Y light {light} first node ignores distribution (affectFirst=0)");
                Assert.That(
                    TranslationAt(Axis.Y, light, baseTime + 1f),
                    Is.EqualTo(-0.5f + offset).Within(1e-4f),
                    $"Y light {light} second node");
                Assert.That(
                    TranslationAt(Axis.Y, light, baseTime + 3f),
                    Is.EqualTo(0.25f + offset).Within(1e-4f),
                    $"Y light {light} last node");

                var (incoming, outgoing) = NodeVelocities(
                    time => TranslationAt(Axis.Y, light, time),
                    Subtract,
                    baseTime + 1f,
                    baseTime,
                    baseTime + 3f);
                Assert.That(
                    outgoing,
                    Is.EqualTo(incoming).Within(0.01f),
                    $"Y light {light} C1 at second node");
            }
        }

        // The extended capture adds group 29, a looped rotation Z node at beat 26, a beat-distributed
        // rotation X box, and changed distributions on top of the frozen fixture.
        [Test, NUnit.Framework.Property("CatmullRomExtendedCapture", "true")]
        public void TranslationInteriorNodesShareOneVelocityOnExtendedCapture()
        {
            Assert.That(fixtureFile, Is.EqualTo("ExpertPlusStandard-Extended.dat"));
            TranslationInteriorNodesShareOneVelocity();
        }

        [Test, NUnit.Framework.Property("CatmullRomExtendedCapture", "true")]
        public void RotationInteriorNodesShareOneVelocityOnExtendedCapture()
        {
            Assert.That(fixtureFile, Is.EqualTo("ExpertPlusStandard-Extended.dat"));
            RotationInteriorNodesShareOneVelocity();
        }

        // The same joins must survive the real UpdateTime paths: playing only walks the state cursor
        // forward, while paused scrubbing binary-searches in either direction. Everything is measured
        // on tween and transform outputs recorded during one monotonic pass, so each direction
        // exercises the actual state selection order.
        [TestCaseSource(nameof(PlaybackDirections))]
        public void PlaybackPathKeepsSplineContinuityAcrossDirections(bool playing, bool forward)
        {
            Assert.That(fixtureFile, Is.EqualTo("ExpertPlusStandard-Extended.dat"));
            var rotationSamples = new Dictionary<(Axis, int, float), RotationSample>();
            var translationSamples = new Dictionary<(Axis, int, float), TranslationSample>();
            var rotationJoins = new List<Join>();
            var translationJoins = new List<Join>();
            var beats = new SortedSet<float>();
            foreach (var axis in Axes)
            {
                for (var light = 0; light < LightCount; light++)
                {
                    var rotationStates = RealStates(rotationContainers[(axis, light)].EventContainer);
                    for (var i = 1; i < rotationStates.Count - 1; i++)
                    {
                        var node = rotationStates[i];
                        var next = rotationStates[i + 1];
                        var incomingSplines = (!node.UsePrevious || node.Loop > 0)
                            && Easing.IsCatmullRom(node.Base.Easing);
                        var outgoingSplines = (!next.UsePrevious || next.Loop > 0)
                            && Easing.IsCatmullRom(next.Base.Easing);
                        if (!incomingSplines || !outgoingSplines)
                        {
                            continue;
                        }

                        var join = Join.Between<LightRotationEventStateData, BaseLightRotationBase>(
                            axis,
                            light,
                            rotationStates[i - 1],
                            node,
                            next);
                        rotationJoins.Add(join);
                        foreach (var beat in join.Beats())
                        {
                            beats.Add(beat);
                        }
                    }

                    var translationStates = RealStates(translationContainers[(axis, light)].EventContainer);
                    for (var i = 1; i < translationStates.Count - 1; i++)
                    {
                        var node = translationStates[i];
                        var next = translationStates[i + 1];
                        var incomingSplines = !node.UsePrevious && Easing.IsCatmullRom(node.Base.Easing);
                        var outgoingSplines = !next.UsePrevious && Easing.IsCatmullRom(next.Base.Easing);
                        if (!incomingSplines || !outgoingSplines)
                        {
                            continue;
                        }

                        var join = Join.Between<LightTranslationEventStateData, BaseLightTranslationBase>(
                            axis,
                            light,
                            translationStates[i - 1],
                            node,
                            next);
                        translationJoins.Add(join);
                        foreach (var beat in join.Beats())
                        {
                            beats.Add(beat);
                        }
                    }
                }
            }

            Assert.That(rotationJoins, Is.Not.Empty, "extended capture must contain rotation spline joins");
            Assert.That(
                translationJoins,
                Is.Not.Empty,
                "extended capture must contain translation spline joins");

            var direction = $"{(playing ? "playing" : "paused")} {(forward ? "forward" : "reverse")}";
            var ordered = forward ? (IEnumerable<float>)beats : beats.Reverse();
            foreach (var beat in ordered)
            {
                rotationEffect.UpdateTime(playing, beat);
                translationEffect.UpdateTime(playing, beat);
                foreach (var axis in Axes)
                {
                    for (var light = 0; light < LightCount; light++)
                    {
                        var rotation = rotationContainers[(axis, light)];
                        var rotationTween = rotation.Tween;
                        rotationSamples[(axis, light, beat)] = new RotationSample
                        {
                            Current = rotationTween.Current,
                            LocalRotation = rotation.Transforms[0].localRotation,
                            State = rotation.EventContainer.CurrentState,
                            UseSpline = rotationTween.UseSpline,
                            P1 = rotationTween.StartValue,
                            P2 = rotationTween.EndValue,
                            M1 = (float)FloatTweenSplineM1.GetValue(rotationTween),
                            M2 = (float)FloatTweenSplineM2.GetValue(rotationTween),
                            Duration = rotationTween.EndTime - rotationTween.StartTime
                        };

                        var translation = translationContainers[(axis, light)];
                        translationSamples[(axis, light, beat)] = new TranslationSample
                        {
                            Current = translation.Tween.Current,
                            LocalPosition = translation.Transforms[0].localPosition,
                            State = translation.EventContainer.CurrentState,
                            UseSpline = translation.Tween.UseSpline
                        };
                    }
                }
            }

            foreach (var join in translationJoins)
            {
                var label = $"translation {join.Axis} {join.Light} node {join.Node} {direction}";
                var before = translationSamples[(join.Axis, join.Light, join.Node - join.H)];
                var at = translationSamples[(join.Axis, join.Light, join.Node)];
                var after = translationSamples[(join.Axis, join.Light, join.Node + join.H)];

                Assert.That(before.State, Is.SameAs(join.PreviousState), $"{label}: state before node");
                Assert.That(at.State, Is.SameAs(join.NodeState), $"{label}: state at node");
                Assert.That(after.State, Is.SameAs(join.NodeState), $"{label}: state after node");
                Assert.That(
                    translationSamples[(join.Axis, join.Light, join.MidBefore)].State,
                    Is.SameAs(join.PreviousState),
                    $"{label}: mid-segment state before node");
                Assert.That(
                    translationSamples[(join.Axis, join.Light, join.MidAfter)].State,
                    Is.SameAs(join.NodeState),
                    $"{label}: mid-segment state after node");
                Assert.That(before.UseSpline, Is.True, $"{label}: incoming segment must spline");
                Assert.That(after.UseSpline, Is.True, $"{label}: outgoing segment must spline");

                var (incoming, outgoing) = NodeVelocities(
                    t => translationSamples[(join.Axis, join.Light, t)].Current,
                    Subtract,
                    join.Node,
                    join.Previous,
                    join.Next);
                Assert.That(
                    outgoing,
                    Is.EqualTo(incoming).Within(0.01f),
                    $"{label}: one-sided velocities (in {incoming:R}, out {outgoing:R})");
                Assert.That(
                    Mathf.Abs(at.Current - before.Current),
                    Is.LessThanOrEqualTo((Mathf.Abs(incoming) * join.H * 1.5f) + 0.005f),
                    $"{label}: the incoming segment must arrive at the node value");
                Assert.That(
                    Mathf.Abs(after.Current - at.Current),
                    Is.LessThanOrEqualTo((Mathf.Abs(outgoing) * join.H * 1.5f) + 0.005f),
                    $"{label}: the outgoing segment must leave the node value");

                Assert.That(
                    TranslationComponent(before.LocalPosition, join.Axis),
                    Is.EqualTo(before.Current).Within(1e-4f),
                    $"{label}: transform position before node");
                Assert.That(
                    TranslationComponent(at.LocalPosition, join.Axis),
                    Is.EqualTo(at.Current).Within(1e-4f),
                    $"{label}: transform position at node");
                Assert.That(
                    TranslationComponent(after.LocalPosition, join.Axis),
                    Is.EqualTo(after.Current).Within(1e-4f),
                    $"{label}: transform position after node");
            }

            foreach (var join in rotationJoins)
            {
                var label = $"rotation {join.Axis} {join.Light} node {join.Node} {direction}";
                var before = rotationSamples[(join.Axis, join.Light, join.Node - join.H)];
                var at = rotationSamples[(join.Axis, join.Light, join.Node)];
                var after = rotationSamples[(join.Axis, join.Light, join.Node + join.H)];

                Assert.That(before.State, Is.SameAs(join.PreviousState), $"{label}: state before node");
                Assert.That(at.State, Is.SameAs(join.NodeState), $"{label}: state at node");
                Assert.That(after.State, Is.SameAs(join.NodeState), $"{label}: state after node");
                Assert.That(
                    rotationSamples[(join.Axis, join.Light, join.MidBefore)].State,
                    Is.SameAs(join.PreviousState),
                    $"{label}: mid-segment state before node");
                Assert.That(
                    rotationSamples[(join.Axis, join.Light, join.MidAfter)].State,
                    Is.SameAs(join.NodeState),
                    $"{label}: mid-segment state after node");
                Assert.That(before.UseSpline, Is.True, $"{label}: incoming segment must spline");
                Assert.That(after.UseSpline, Is.True, $"{label}: outgoing segment must spline");

                // C0: the evaluated endpoint of the incoming segment must equal the evaluated start
                // of the outgoing one, modulo the 360-degree wrap.
                var leftEnd = HermiteDouble(
                    before.P1, before.P2, before.M1, before.M2, 1.0);
                var rightStart = HermiteDouble(
                    after.P1, after.P2, after.M1, after.M2, 0.0);
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle((float)leftEnd, (float)rightStart)),
                    Is.LessThan(0.05f),
                    $"{label}: evaluated segment endpoints must agree at the node");

                // One-sided velocities from evaluating the prepared Hermite segments in double
                // precision; ~300-degree float Current values make direct differencing too noisy.
                // Second-order one-sided stencils keep the truncation error off several-hundred
                // degree swings without pushing the step into cancellation territory.
                const double d = 1e-4;
                var incomingVelocity =
                    ((3.0 * HermiteDouble(before.P1, before.P2, before.M1, before.M2, 1.0))
                        - (4.0 * HermiteDouble(before.P1, before.P2, before.M1, before.M2, 1.0 - d))
                        + HermiteDouble(before.P1, before.P2, before.M1, before.M2, 1.0 - (2.0 * d)))
                    / (2.0 * d * before.Duration);
                var outgoingVelocity =
                    ((-3.0 * HermiteDouble(after.P1, after.P2, after.M1, after.M2, 0.0))
                        + (4.0 * HermiteDouble(after.P1, after.P2, after.M1, after.M2, d))
                        - HermiteDouble(after.P1, after.P2, after.M1, after.M2, 2.0 * d))
                    / (2.0 * d * after.Duration);
                Assert.That(
                    outgoingVelocity,
                    Is.EqualTo(incomingVelocity).Within(0.05f),
                    $"{label}: evaluated velocities (in {incomingVelocity:R}, out {outgoingVelocity:R})");

                var axisVector = RotationAxisVector(join.Axis);
                Assert.That(
                    Quaternion.Angle(
                        before.LocalRotation,
                        Quaternion.AngleAxis(before.Current, axisVector)),
                    Is.LessThan(0.01f),
                    $"{label}: transform rotation before node");
                Assert.That(
                    Quaternion.Angle(
                        at.LocalRotation,
                        Quaternion.AngleAxis(at.Current, axisVector)),
                    Is.LessThan(0.01f),
                    $"{label}: transform rotation at node");
                Assert.That(
                    Quaternion.Angle(
                        after.LocalRotation,
                        Quaternion.AngleAxis(after.Current, axisVector)),
                    Is.LessThan(0.01f),
                    $"{label}: transform rotation after node");
            }
        }

        // Each case owns the tag so the extended fixture loads during its own SetUp; there is no
        // playing-reverse mode because the playing cursor only walks forward.
        private static IEnumerable<TestCaseData> PlaybackDirections()
        {
            yield return new TestCaseData(false, true)
                .SetProperty("CatmullRomExtendedCapture", "true");
            yield return new TestCaseData(false, false)
                .SetProperty("CatmullRomExtendedCapture", "true");
            yield return new TestCaseData(true, true)
                .SetProperty("CatmullRomExtendedCapture", "true");
        }

        private sealed class Join
        {
            public Axis Axis;
            public int Light;
            public float Previous;
            public float Node;
            public float Next;
            public float H;
            public float MidBefore;
            public float MidAfter;
            public object PreviousState;
            public object NodeState;

            public static Join Between<TState, TData>(
                Axis axis,
                int light,
                TState previous,
                TState node,
                TState next)
                where TState : EventGroupEventStateData<TData>
                where TData : BaseGLSEvent
            {
                var h = Mathf.Min(
                    0.01f,
                    Mathf.Min(node.StartTime - previous.StartTime, next.StartTime - node.StartTime)
                        * 0.005f);
                return new Join
                {
                    Axis = axis,
                    Light = light,
                    Previous = previous.StartTime,
                    Node = node.StartTime,
                    Next = next.StartTime,
                    H = h,
                    MidBefore = (previous.StartTime + node.StartTime) * 0.5f,
                    MidAfter = (node.StartTime + next.StartTime) * 0.5f,
                    PreviousState = previous,
                    NodeState = node
                };
            }

            public IEnumerable<float> Beats()
            {
                yield return Node - (2f * H);
                yield return Node - H;
                yield return Node;
                yield return Node + H;
                yield return Node + (2f * H);
                yield return MidBefore;
                yield return MidAfter;
            }
        }

        private struct RotationSample
        {
            public float Current;
            public Quaternion LocalRotation;
            public object State;
            public bool UseSpline;
            public double P1;
            public double P2;
            public double M1;
            public double M2;
            public double Duration;
        }

        private struct TranslationSample
        {
            public float Current;
            public Vector3 LocalPosition;
            public object State;
            public bool UseSpline;
        }

        private static Vector3 RotationAxisVector(Axis axis) => axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            _ => Vector3.forward
        };

        private static float TranslationComponent(Vector3 position, Axis axis) => axis switch
        {
            Axis.X => position.x,
            Axis.Y => position.y,
            _ => position.z
        };

        // The same cubic Hermite the tween runs, evaluated in double so ~300-degree rotation values
        // do not lose the velocity comparison to float32 noise.
        private static double HermiteDouble(double p1, double p2, double m1, double m2, double t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return (((2.0 * t3) - (3.0 * t2) + 1.0) * p1)
                + ((t3 - (2.0 * t2) + t) * m1)
                + (((-2.0 * t3) + (3.0 * t2)) * p2)
                + ((t3 - t2) * m2);
        }

        private void InsertTranslationGroup(string json)
        {
            var group = V3LightTranslationEventBoxGroup.GetFromJson(JSON.Parse(json));
            group.NormalizeLoadedEventConflicts();
            group.SetMap(map);
            group.RecomputeSongBpmTime();
            translationEffect.InsertData(group);
            translationEffect.Refresh();
        }
    }
}
