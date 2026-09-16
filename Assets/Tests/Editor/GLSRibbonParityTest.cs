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
            Node(2).CustomLerpType = "HSV";
            ColorAt(1, 40f);
            Assert.IsNull(containers[1].Tween.ColorEasing,
                "A no-transition node must not animate the preceding color using a retained custom colorEasing.");
            Assert.AreEqual(Beatmap.Shared.BasicEventColorLerpType.RGB, containers[1].Tween.ColorLerpType,
                "An instant destination must not apply its HSV conversion to held HDR colors.");
            AssertRibbonPixels(1, 1, 0, 40f, 1);
        }

        // Shift-generated HDR values must take the identical HSV conversion path in preview and ribbon pixels.
        [Test]
        public void HdrHsvRibbonMatchesPreviewLight()
        {
            Node(2).CustomLerpType = "HSV";
            AssertRibbonPixels(1, 1, 0, 30.15f, 1);
        }

        // ReportedWaveMapJson regression: near the all-light blue destination the lasers render
        // washed-out blue while the ribbon kept showing the raw shifted sRGB colors because the
        // timeline texture bypassed sRGB->linear conversion. Every strip must equal the parametric
        // light shader's output for the same live tween color. The beat-39 node owns the even lanes
        // [39,46]; the beat-24 node owns the odd lanes [24,46].
        [TestCase(0, 0, 3, 44f)]
        [TestCase(0, 0, 3, 45f)]
        [TestCase(0, 0, 3, 45.5f)]
        [TestCase(0, 1, 1, 44f)]
        [TestCase(0, 1, 1, 45f)]
        [TestCase(0, 1, 1, 45.5f)]
        public void WashedOutShiftRibbonMatchesPreviewLight(int group, int box, int node, float beat)
        {
            LoadPlayback(ReportedWaveMapJson);
            AssertRibbonPixels(group, box, node, beat, -1);
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
                            lightMaterial.SetColor("_Color", live);
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
