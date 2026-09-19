using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    // Per-light ribbon strip parity on the wave fixture: each physical strip must render the same
    // color the production light shows at that beat, including distributed timing, independent easing
    // tracks, HSV endpoints, and instant-destination holds.
    public class GLSRibbonParityTest : GLSColorPlaybackTestBase
    {
        // WashedOutColorDistributionRibbonMatchesPreviewLight freezes independently reviewed preview
        // output for the reported map. Neither the live tween nor generated ribbon payload supplies
        // expected colors at assertion time, so a shared preparation regression fails both subjects.
        private static readonly Dictionary<float, Color[]> ReportedWaveExpectedPreview = new()
        {
            [44f] = new[]
            {
                new Color(0.122f, 0.196f, 0.799f, 4.719f),
                new Color(0.093f, 0.090f, 0.907f, 1.000f),
                new Color(0.169f, 0.280f, 0.883f, 4.721f),
                new Color(0.096f, 0.094f, 0.904f, 1.000f),
                new Color(0.035f, 0.403f, 0.810f, 4.723f),
                new Color(0.097f, 0.099f, 0.900f, 1.000f),
                new Color(0.302f, 0.257f, 0.714f, 4.725f),
                new Color(0.099f, 0.104f, 0.896f, 1.001f),
            },
            [45f] = new[]
            {
                new Color(0.137f, 0.002f, 0.990f, 1.032f),
                new Color(0.046f, 0.045f, 0.954f, 1.000f),
                new Color(0.001f, 0.134f, 1.088f, 1.032f),
                new Color(0.047f, 0.047f, 0.952f, 1.000f),
                new Color(0.056f, 0.327f, 0.858f, 1.032f),
                new Color(0.048f, 0.050f, 0.950f, 1.000f),
                new Color(0.419f, 0.098f, 0.857f, 1.032f),
                new Color(0.050f, 0.052f, 0.948f, 1.000f),
            },
            [45.5f] = new[]
            {
                new Color(0.069f, 0.000f, 0.996f, 1.000f),
                new Color(0.023f, 0.023f, 0.977f, 1.000f),
                new Color(0.000f, 0.067f, 1.045f, 1.000f),
                new Color(0.023f, 0.024f, 0.976f, 1.000f),
                new Color(0.028f, 0.164f, 0.929f, 1.000f),
                new Color(0.024f, 0.025f, 0.975f, 1.000f),
                new Color(0.212f, 0.049f, 0.929f, 1.000f),
                new Color(0.025f, 0.026f, 0.974f, 1.000f),
            },
        };

        // The first source fans out to B/C, and C/final B each keep only their four owned strips until D.
        [TestCase(0, 0, 0, 5f)]
        [TestCase(0, 0, 0, 20f)]
        [TestCase(1, 0, 0, 14.35f)]
        [TestCase(1, 0, 2, 33.1f)]
        [TestCase(1, 0, 3, 42.1f)]
        [TestCase(1, 1, 0, 23.35f)]
        [TestCase(1, 1, 0, 30.15f)]
        [TestCase(1, 1, 0, 45.999f)]
        public void RibbonPixelsMatchEachOwnedPreviewLight(int group, int box, int node, float beat) =>
            AssertRibbonPixels(group, box, node, beat, -1);

        // Probe odd strips directly so a wrong even-light mask cannot hide distribution/easing failures later in the loop.
        [TestCase(0, 0, 7, 23.6f)]
        [TestCase(1, 1, 1, 23.35f)]
        [TestCase(1, 1, 7, 24.2f)]
        [TestCase(1, 1, 3, 31.6f)]
        public void DistributedTimingAndIndependentEasingsMatchPreview(int group, int box, int light, float beat) =>
            AssertRibbonPixels(group, box, 0, beat, light);

        // Instant destinations ignore their stored transition-only metadata and still show the preceding held/strobing strip.
        [Test]
        public void InstantDestinationKeepsHeldRibbonWithoutApplyingStoredEasing()
        {
            Node(2).Easing = (int)Beatmap.Enums.EaseType.None;
            Node(2).CustomLerpType = BasicEventColorLerpType.TrueHSV;
            ColorAt(1, 40f);
            Assert.IsNull(containers[1].Tween.ColorEasing,
                "A no-transition node must not animate the preceding color using a retained custom colorEasing.");
            Assert.AreEqual(Beatmap.Shared.BasicEventColorLerpType.RGB, containers[1].Tween.ColorLerpType,
                "An instant destination must not apply its HSV conversion to held HDR colors.");
            AssertRibbonPixels(1, 1, 0, 40f, 1);
        }

        // Color-distribution-generated HDR values must take the identical HSV conversion path in preview and ribbon pixels.
        [Test]
        public void HdrHsvRibbonMatchesPreviewLight()
        {
            Node(2).CustomLerpType = BasicEventColorLerpType.TrueHSV;
            AssertRibbonPixels(1, 1, 0, 30.15f, 1);
        }

        // HiddenGlsLightTransitionsHideBothRibbonDirections proves turning off the Graphics option suppresses every
        // GLS color-tween ribbon, including the separate incoming renderer used at cross-group boundaries.
        [Test]
        public void HiddenGlsLightTransitionsHideBothRibbonDirections()
        {
            var outgoingObject = new GameObject("disabled outgoing GLS ribbon");
            var incomingObject = new GameObject("disabled incoming GLS ribbon");
            try
            {
                var outgoing = GLSColorTransitionCacheTest.CreateRibbonController(outgoingObject, out _);
                var incoming = GLSColorTransitionCacheTest.CreateRibbonController(incomingObject, out _);
                Settings.Instance.VisualizeGLSLightTransitions = false;

                GLSEventCommon.UpdateColorTransitionRibbon(
                    outgoing, Node(1, 0, 3), appearance, _ => false, LightCount);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    incoming, Node(1, 0, 0), appearance, _ => false, LightCount);

                Assert.That(outgoingObject.activeSelf, Is.False, "The disable setting must hide outgoing GLS ribbons.");
                Assert.That(incomingObject.activeSelf, Is.False, "The disable setting must hide incoming GLS ribbons.");
            }
            finally
            {
                Settings.Instance.VisualizeGLSLightTransitions = true;
                Object.DestroyImmediate(outgoingObject);
                Object.DestroyImmediate(incomingObject);
            }
        }

        // EveryColorEasingMatchesRibbonOutput prevents a shader formula or timeline-channel regression from changing the normal color shown by the ribbon.
        [Test]
        public void EveryColorEasingMatchesRibbonOutput() =>
            AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack.Color);

        // EveryStrobeColorEasingMatchesRibbonOutput covers the independently authored color of the bright strobe phase for every supported curve.
        [Test]
        public void EveryStrobeColorEasingMatchesRibbonOutput() =>
            AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack.StrobeColor);

        // EveryStrobeEasingMatchesRibbonOutput covers the pulse fade itself, which must not accidentally use either of the two color easing tracks.
        [Test]
        public void EveryStrobeEasingMatchesRibbonOutput() =>
            AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack.Strobe);

        // ReportedWaveMapJson regression: near the all-light blue destination the lasers render
        // washed-out blue while the ribbon kept showing the raw color-distributed sRGB colors because the
        // timeline texture bypassed sRGB->linear conversion. Every strip must equal the parametric
        // light shader's output for the same live tween color. The beat-39 node owns the even lanes
        // [39,46]; the beat-24 node owns the odd lanes [24,46].
        [TestCase(0, 0, 3, 44f)]
        [TestCase(0, 0, 3, 45f)]
        [TestCase(0, 0, 3, 45.5f)]
        [TestCase(0, 1, 1, 44f)]
        [TestCase(0, 1, 1, 45f)]
        [TestCase(0, 1, 1, 45.5f)]
        public void WashedOutColorDistributionRibbonMatchesPreviewLight(int group, int box, int node, float beat)
        {
            LoadPlayback(ReportedWaveMapJson);
            // HundredBrightnessRedToGreenRibbonMatchesGameYellowMidpoint: the second box owns odd
            // lights and authors equal-output RGB endpoints, independently declaring which strips
            // must preserve peak intensity instead of consulting the generated ribbon texture.
            System.Func<int, bool> expectsPeakCompensation = box == 1
                ? light => light % 2 == 1
                : _ => false;
            var expectedPreview = ReportedWaveExpectedPreview[beat];
            System.Func<int, bool> expectedOwnership = box == 0
                ? light => light % 2 == 0
                : light => light % 2 == 1;
            AssertRibbonPixels(
                group,
                box,
                node,
                beat,
                -1,
                expectsPeakCompensation,
                light => expectedPreview[light],
                expectedOwnership);
        }

        [Test]
        public void DumpStripVsLightValues()
        {
            LoadPlayback(ReportedWaveMapJson);
            var lightMaterial = GLSColorTransitionCacheTest.CreateLightSampleMaterial();
            var sb = new StringBuilder();
            foreach (var beat in new[] { 43f, 44f, 45f })
            {
                sb.AppendLine($"=== beat {beat} ===");
                for (var light = 0; light < LightCount; light++)
                {
                    var live = ColorAt(light, beat);
                    lightMaterial.SetColor("_Color", live);
                    var laser = GLSColorTransitionCacheTest.RenderGradientPixel(lightMaterial, 0.5f, 0.5f);
                    var state = StateAt(light, beat);
                    sb.AppendLine($"light={light} live={live} laser={laser} owner={state?.Base?.JsonTime} grp={state?.Base?.EventBoxGroupData?.JsonTime} boxIdx={(state?.Base?.EventBoxData as BaseLightColorEventBox)?.BeatDistribution}");
                }

                for (var box = 0; box < map.LightColorEventBoxGroups[0].Boxes.Count; box++)
                {
                    for (var node = 0; node < map.LightColorEventBoxGroups[0].Boxes[box].Events.Length; node++)
                    {
                        var source = Node(0, box, node);
                        DumpRibbon(sb, $"out b{box}n{node}@{source.JsonTime}", source,
                            (r, s) => GLSEventCommon.UpdateColorTransitionRibbon(r, s, appearance, _ => false, LightCount), beat);
                        DumpRibbon(sb, $"in b{box}n{node}@{source.JsonTime}", source,
                            (r, s) => GLSEventCommon.UpdateIncomingColorTransitionRibbon(r, s, appearance, _ => false, LightCount), beat);
                    }
                }

                // The outer lane previews aggregate every same-time box into one ribbon per group node.
                foreach (var group in map.LightColorEventBoxGroups)
                {
                    var representative = group.Boxes.SelectMany(b => b.Events).OrderBy(e => e.RelativeJsonTime).FirstOrDefault();
                    if (representative == null) continue;
                    DumpRibbon(sb, $"aggOut g{group.JsonTime}", representative,
                        (r, s) => GLSEventCommon.UpdateColorTransitionRibbon(r, s, appearance, _ => false, LightCount, aggregateSameTimeBoxes: true), beat);
                    DumpRibbon(sb, $"aggIn g{group.JsonTime}", representative,
                        (r, s) => GLSEventCommon.UpdateIncomingColorTransitionRibbon(r, s, appearance, _ => false, LightCount, aggregateSameTimeBoxes: true), beat);
                }
            }
            Debug.Log(sb.ToString());
            Object.DestroyImmediate(lightMaterial);
        }

        // The Weave-map screenshots caught the band mid-strobe: s:20 wraps the strobe phase twenty
        // times per beat, so integer/half-beat parity checks always land on phase zero. Sample the
        // full phase cycle so a strip/tween strobe-clock offset cannot hide between them.
        [Test]
        public void StripMatchesLightAcrossTheWholeStrobePhaseCycle()
        {
            LoadPlayback(ReportedWaveMapJson);
            var lightMaterial = GLSColorTransitionCacheTest.CreateLightSampleMaterial();
            var ribbonObject = new GameObject("strip");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    ribbon, Node(0, 0, 3), appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                try
                {
                    for (var step = 0; step <= 20; step++)
                    {
                        var beat = 44f + (step * 0.025f);
                        for (var light = 0; light < LightCount; light += 2)
                        {
                            var progress = (SongTime(beat) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                            var lane = (LightCount - light - 0.5f) / LightCount;
                            var strip = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                            var live = ColorAt(light, beat);
                            // SharedRibbonAlphaCurveIsTunableAndRollbackSafe compares every strobe phase after the ribbon-only opacity transform.
                            lightMaterial.SetColor(
                                "_Color",
                                GLSColorTransitionCacheTest.ApplyExpectedRibbonOpacity(live));
                            var laser = GLSColorTransitionCacheTest.RenderGradientPixel(lightMaterial, 0.5f, 0.5f);
                            AssertColorRgb(strip.gamma, laser, 0.03f,
                                $"beat={beat} light={light} strip must equal the light at every strobe phase");
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(material);
                }
            }
            finally
            {
                Object.DestroyImmediate(lightMaterial);
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // The exhaustive easing regressions compare the real ribbon shader with LightColorTween, the same runtime evaluator used by preview lights.
        private static void AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack track)
        {
            var texture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true);
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient")) { enableInstancing = false };
            var lightMaterial = GLSColorTransitionCacheTest.CreateLightSampleMaterial();
            try
            {
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetTexture("_LightDistributionTex", texture);
                foreach (var easingEntry in Easing.IDToInternalName.OrderBy(entry => entry.Key))
                {
                    var easing = Easing.FromID(easingEntry.Key);
                    var shaderId = Easing.EasingShaderId(easingEntry.Key);
                    var progress = track == RibbonEasingTrack.Strobe ? 0.2f : 0.37f;
                    var tween = CreateEasingProbeTween(track, easing);
                    texture.SetPixels(CreateEasingProbeRows(tween, track, shaderId));
                    texture.Apply(false, false);
                    tween.UpdateTime(progress);
                    var ribbonPixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, 0.5f).gamma;
                    // SharedRibbonAlphaCurveIsTunableAndRollbackSafe keeps exhaustive easing parity on the shared ribbon-opacity policy.
                    lightMaterial.SetColor(
                        "_Color",
                        GLSColorTransitionCacheTest.ApplyExpectedRibbonOpacity(tween.Color));
                    var lightPixel = GLSColorTransitionCacheTest.RenderGradientPixel(lightMaterial, 0.5f, 0.5f);
                    AssertColorRgb(
                        ribbonPixel,
                        lightPixel,
                        0.02f,
                        $"{track} easing {easingEntry.Value} ({easingEntry.Key}) must produce the preview light's color.");
                }
            }
            finally
            {
                Object.DestroyImmediate(lightMaterial);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        // The probe holds every unrelated track linear so each regression fails only for the selected ribbon easing channel.
        private static LightColorTween CreateEasingProbeTween(
            RibbonEasingTrack track,
            System.Func<float, float> easing)
        {
            // A normal-color probe disables strobing, a strobe-color probe lands in its hard on-phase, and a fade probe uses one cycle for an uncluttered pulse sample.
            var strobeFrequency = 1f;
            if (track == RibbonEasingTrack.Color)
            {
                strobeFrequency = 0f;
            }
            else if (track == RibbonEasingTrack.StrobeColor)
            {
                strobeFrequency = 2f;
            }

            var tween = new LightColorTween
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                StartColor = new Color(0.12f, 0.38f, 0.77f, 1f),
                EndColor = new Color(0.91f, 0.22f, 0.08f, 1f),
                StartAlpha = 1f,
                EndAlpha = 1f,
                StartStrobeColor = new Color(0.84f, 0.05f, 0.61f, 1f),
                EndStrobeColor = new Color(0.03f, 0.92f, 0.31f, 1f),
                StartStrobeBrightness = 1f,
                EndStrobeBrightness = 1f,
                StartStrobeFrequency = strobeFrequency,
                EndStrobeFrequency = strobeFrequency,
                StrobeFade = track == RibbonEasingTrack.Strobe,
                ColorLerpType = BasicEventColorLerpType.RGB,
                Easing = Easing.Linear,
                ColorEasing = track == RibbonEasingTrack.Color ? easing : Easing.Linear,
                StrobeColorEasing = track == RibbonEasingTrack.StrobeColor ? easing : Easing.Linear,
                StrobeEasing = track == RibbonEasingTrack.Strobe ? easing : Easing.Cubic.InOut,
                ComposeAlphaAtColorEndpoints = true
            };
            return tween;
        }

        // The nine rows mirror GLSColorTransitionPreview.UpdateTimeline so this test exercises the shader's actual per-track channel contract.
        private static Color[] CreateEasingProbeRows(
            LightColorTween tween,
            RibbonEasingTrack track,
            int shaderId)
        {
            var linearShaderId = Easing.EasingShaderId((int)Beatmap.Enums.EaseType.Linear);
            var rows = new Color[9];
            rows[0] = tween.StartColor;
            rows[1] = tween.EndColor;
            rows[2] = tween.StartStrobeColor;
            rows[3] = tween.EndStrobeColor;
            rows[4] = new Color(tween.StartTimeAlpha, tween.EndTimeAlpha, tween.StartTimeColor, tween.EndTimeColor);
            rows[5] = new Color(tween.StartStrobeFrequency, tween.EndStrobeFrequency, tween.StartAlpha, tween.EndAlpha);
            rows[6] = new Color(tween.StartStrobeBrightness, tween.EndStrobeBrightness, tween.StartAlpha, tween.EndAlpha);
            rows[7] = new Color(tween.StartStrobeColor.a, tween.EndStrobeColor.a, 0f, tween.StrobeFade ? 3f : 2f);
            rows[8] = new Color(
                linearShaderId,
                track == RibbonEasingTrack.Color ? shaderId : linearShaderId,
                track == RibbonEasingTrack.StrobeColor ? shaderId : linearShaderId,
                track == RibbonEasingTrack.Strobe ? shaderId : Easing.EasingShaderId("easeInOutCubic"));
            return rows;
        }

        // Named tracks keep the three independent ribbon channels explicit in failure output.
        private enum RibbonEasingTrack
        {
            Color,
            StrobeColor,
            Strobe
        }

        // The real outer lane can stack ghost/primary ribbon quads differently than any manual
        // composite: spawn the actual prefab containers for both g:1 box-groups, enumerate every
        // active ribbon (primary + pooled ghosts, outgoing + incoming), and count how many strips
        // light each physical light at the reported beat. Two lit strips per light is the double-draw.
        [Test]
        public void RealOuterLaneRibbonsDoNotStackPerLight()
        {
            LoadPlayback(ReportedWaveMapJson);
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            Assert.IsNotNull(collection, "The playmode scene must contain the outer GLS group collection.");
            var containers = new List<GLSGroupContainer>();
            var ribbons = new List<(string label, LightGradientController ribbon)>();
            try
            {
                foreach (var group in map.LightColorEventBoxGroups)
                {
                    var container = (GLSGroupContainer)collection.CreateContainer();
                    containers.Add(container);
                    container.ObjectData = group;
                    container.GlsLightCount = LightCount;
                    container.ConfigurePreviewNodes(_ => false);
                }

                var ghostField = typeof(GLSGroupContainer).GetField(
                    "previewGhosts", BindingFlags.Instance | BindingFlags.NonPublic);
                var bodies = new List<(string label, GLSGroupContainer container)>();
                foreach (var container in containers)
                {
                    bodies.Add(($"body g{container.EventBoxGroupData.JsonTime}", container));
                    foreach (var ghost in (List<GLSGroupContainer>)ghostField.GetValue(container))
                    {
                        bodies.Add(($"ghost g{container.EventBoxGroupData.JsonTime}@{ghost.PreviewEventData?.JsonTime}", ghost));
                    }
                }

                foreach (var (label, container) in bodies)
                {
                    foreach (var ribbon in container.GetComponentsInChildren<LightGradientController>(true))
                    {
                        var direction = ribbon.IsIncomingColorTransition ? "in" : "out";
                        ribbons.Add((
                            $"{label} {direction} span=[{ribbon.ColorTimelineStart},{ribbon.ColorTimelineStart + ribbon.ColorTimelineDuration}]",
                            ribbon));
                    }
                }

                var sb = new StringBuilder();
                const float beat = 44f;
                var time = SongTime(beat);
                var coverage = new List<(string label, LightGradientController ribbon)>[LightCount];
                for (var light = 0; light < LightCount; light++)
                {
                    coverage[light] = new List<(string label, LightGradientController ribbon)>();
                }

                foreach (var (label, ribbon) in ribbons)
                {
                    sb.Append($"{label}: active={ribbon.gameObject.activeSelf}");
                    if (!ribbon.gameObject.activeSelf)
                    {
                        sb.AppendLine();
                        continue;
                    }

                    var renderer = (MeshRenderer)typeof(LightGradientController)
                        .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    var material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                    try
                    {
                        var progress = (time - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                        for (var light = 0; light < LightCount; light++)
                        {
                            var lane = (LightCount - light - 0.5f) / LightCount;
                            var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                            if (pixel.r + pixel.g + pixel.b > 0.02f)
                            {
                                coverage[light].Add((label, ribbon));
                            }
                            sb.Append($" L{light}({pixel.r:0.00},{pixel.g:0.00},{pixel.b:0.00})");
                        }
                        sb.AppendLine();
                    }
                    finally
                    {
                        Object.DestroyImmediate(material);
                    }
                }

                for (var light = 0; light < LightCount; light++)
                {
                    sb.AppendLine($"light={light} lit-by=[{string.Join("; ", coverage[light].Select(c => c.label))}]");
                    Assert.That(coverage[light].Count, Is.LessThanOrEqualTo(1),
                        $"beat={beat} light={light}: {coverage[light].Count} outer ribbons render the same light");
                }
                Debug.Log(sb.ToString());
            }
            finally
            {
                foreach (var container in containers)
                {
                    Object.DestroyImmediate(container.gameObject);
                }
            }
        }

        [Test]
        public void DumpStripImage()
        {
            LoadPlayback(ReportedWaveMapJson);
            var objects = new List<GameObject>();
            try
            {
                // Composite every strip that covers the [39,46] lane region like the scene does:
                // each box's outgoing strip plus the blue destination node's incoming strip.
                var sources = new List<(string label, BaseLightColorBase node, bool incoming)>
                {
                    ("out b0n3", Node(0, 0, 3), false),
                    ("out b1n1", Node(0, 1, 1), false),
                };
                var blueNode = map.LightColorEventBoxGroups
                    .SelectMany(g => g.Boxes.SelectMany(b => b.Events))
                    .FirstOrDefault(e => Mathf.Approximately((float)e.JsonTime, 46f));
                if (blueNode != null)
                {
                    sources.Add(("in b46", blueNode, true));
                }

                var materials = new List<(string label, Material material, float start, float duration)>();
                foreach (var (label, node, incoming) in sources)
                {
                    var ribbonObject = new GameObject($"strip {label}");
                    objects.Add(ribbonObject);
                    var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                    if (incoming)
                    {
                        GLSEventCommon.UpdateIncomingColorTransitionRibbon(ribbon, node, appearance, _ => false, LightCount);
                    }
                    else
                    {
                        GLSEventCommon.UpdateColorTransitionRibbon(ribbon, node, appearance, _ => false, LightCount);
                    }

                    if (!ribbonObject.activeSelf)
                    {
                        Debug.Log($"{label}: hidden");
                        continue;
                    }

                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    materials.Add((label, GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties),
                        ribbon.ColorTimelineStart, ribbon.ColorTimelineDuration));
                    Debug.Log($"{label}: span=[{ribbon.ColorTimelineStart},{ribbon.ColorTimelineStart + ribbon.ColorTimelineDuration}]");
                }

                const int columns = 140;
                const int lanes = 8;
                const float windowStart = 39f;
                const float windowEnd = 46f;
                var rt = new RenderTexture(columns, lanes, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                var read = new Texture2D(columns, lanes, TextureFormat.RGBA32, false, true);
                try
                {
                    rt.Create();
                    RenderTexture.active = rt;
                    GL.Clear(true, true, Color.black);
                    GL.PushMatrix();
                    GL.LoadOrtho();
                    foreach (var (label, material, start, duration) in materials)
                    {
                        var u0 = (SongTime(windowStart) - start) / duration;
                        var u1 = (SongTime(windowEnd) - start) / duration;
                        var mesh = new Mesh
                        {
                            vertices = new[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, 1), new Vector3(0, 1) },
                            triangles = new[] { 0, 1, 2, 0, 2, 3 },
                            uv = new[]
                            {
                                new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1)
                            }
                        };
                        material.SetPass(0);
                        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                        Object.DestroyImmediate(mesh);
                    }
                    GL.PopMatrix();
                    read.ReadPixels(new Rect(0, 0, columns, lanes), 0, 0, false);
                    read.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = null;
                }

                var sb = new StringBuilder();
                for (var x = 0; x < columns; x += 4)
                {
                    var beat = windowStart + ((x / (float)(columns - 1)) * (windowEnd - windowStart));
                    sb.Append($"t={beat:0.00}: ");
                    for (var y = lanes - 1; y >= 0; y--)
                    {
                        var p = read.GetPixel(x, y).gamma;
                        sb.Append($"L{lanes - 1 - y}[{p.r:0.00},{p.g:0.00},{p.b:0.00}] ");
                    }
                    sb.AppendLine();
                }
                Debug.Log(sb.ToString());
                Object.DestroyImmediate(read);
                rt.Release();
                Object.DestroyImmediate(rt);
                foreach (var (_, material, _, _) in materials)
                {
                    Object.DestroyImmediate(material);
                }
            }
            finally
            {
                foreach (var o in objects)
                {
                    Object.DestroyImmediate(o);
                }
            }
        }

        private void DumpRibbon(
            StringBuilder sb, string label, BaseLightColorBase source,
            System.Action<LightGradientController, BaseLightColorBase> update, float beat)
        {
            var ribbonObject = new GameObject($"strip {label}");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                update(ribbon, source);
                if (!ribbonObject.activeSelf)
                {
                    sb.AppendLine($"strip {label}: hidden");
                    return;
                }

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                var start = ribbon.ColorTimelineStart;
                var duration = ribbon.ColorTimelineDuration;
                var progress = (SongTime(beat) - start) / duration;
                sb.AppendLine($"strip {label} span=[{start},{start + duration}] progress={progress:0.###}");
                for (var light = 0; light < LightCount; light++)
                {
                    var lane = (LightCount - light - 0.5f) / LightCount;
                    var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                    sb.AppendLine($"  light={light} lane={lane:0.###} stored={pixel} presented={pixel.gamma}");
                }

                Object.DestroyImmediate(material);
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }
    }
}
