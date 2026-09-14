using System;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Reconstruct the supplied 2026-09-13 GLS Wave Testing map, not the older six-group Wave fixture.
    // Info.dat specifies 100 BPM and WeaveEnvironment; its group 1 has eight physical lights.
    public class GLSColliderWaveTest : TestBase
    {
        internal const int LightCount = 8;
        internal const string MapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":0,""sf"":1,""customData"":{""color"":[1,0.4,0.913]}}]}]},
            {""b"":11,""g"":1,""e"":[
                {""f"":{""f"":2,""p"":0,""t"":2,""c"":8},""w"":0,""d"":1,""r"":0.03,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":5,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.942]}},
                    {""b"":6,""c"":0,""s"":0.6,""i"":1,""f"":0,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.942]}},
                    {""b"":18.75,""c"":0,""s"":0.5,""i"":1,""f"":1,""sb"":0.8,""sf"":1,""customData"":{""color"":[0,0.3,0],""strobeShifts"":[""sv,0.7,lin""],""shifts"":[""v,5,l,l""],""strobeColor"":[0.2,0.3,0.3],""strobeInterval"":5}},
                    {""b"":28,""c"":0,""s"":0.8,""i"":1,""f"":1,""sb"":1,""sf"":1,""customData"":{""color"":[0.179,1,0],""strobeColor"":[0.969,0,0.941],""shifts"":[""hs,-0.6,lin"",""hs,0.6,ioc""],""strobeShifts"":[""hs,0.2,lin"",""v,2,lin""],""strobeInterval"":1}}]},
                {""f"":{""f"":1,""p"":1},""w"":0.4,""d"":2,""r"":0.01,""t"":1,""b"":0,""i"":1,""e"":[{""b"":11,""c"":0,""s"":0.2,""i"":1,""f"":1,""sb"":0.9,""sf"":1,""customData"":{""color"":[1,0.4,0.913],""strobeShifts"":[""h,0.3,lin""],""shifts"":[""v,6,lin,l""],""strobeColorEasing"":4,""strobeInterval"":2,""strobeEasing"":19}}]}]},
            {""b"":46,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":1,""customData"":{""color"":[0,0,1],""strobeColor"":[0.969,0,0],""colorEasing"":11}}]}]}]}";

        // OEM cross-group interruption is keyed to the later group's element start at beat 15, while its first
        // all-light color node occurs at beat 17; prior filtered nodes at beats 15, 17, and 20 must all be dropped.
        private const string InterruptedMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,1,1]}}]}]},
            {""b"":10,""g"":1,""e"":[{""f"":{""f"":2,""p"":0,""t"":2,""c"":8},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0]}},
                {""b"":5,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,1,0]}},
                {""b"":7,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,1,0]}},
                {""b"":10,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,1]}}]}]},
            {""b"":15,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,1]}}]}]}]}";

        // Exact beat-88/93 report: the destination starts an sf=1 strobe with a black (sb=0) alternate phase.
        private const string BlackStrobeStartMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":88,""g"":1,""e"":[{""f"":{""f"":1,""p"":1,""t"":0,""r"":0,""c"":0,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":0.5,""i"":1,""f"":0,""sb"":0,""sf"":1,""customData"":{""color"":[1,0.4,0.913],""shifts"":[""h,-0.2,lin,l""],""colorEasing"":30}},
                {""b"":5,""c"":0,""s"":0.5,""i"":1,""f"":1,""sb"":0,""sf"":1,""customData"":{""color"":[0.678,0.4,1],""shifts"":[""h,0.1,lin,l""],""strobeShifts"":[""s,-0.2,iobk""],""strobeColor"":[0.969,0.384,0.71],""colorEasing"":22}}]}]}]}";

        // Exact beat-100 report: complementary hard strobes share both timestamps across two serialized boxes.
        private const string AlternatingChunksMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[{""b"":100,""g"":1,""e"":[
            {""f"":{""f"":2,""p"":0,""t"":2,""r"":0,""c"":4,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":1,""sf"":0},
                {""b"":3.909,""c"":0,""s"":0.5,""i"":2,""f"":2,""sb"":1,""sf"":1}]},
            {""f"":{""f"":1,""p"":1,""t"":0,""r"":0,""c"":0,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":1,""sb"":0,""sf"":0},
                {""b"":3.909,""c"":0,""s"":0.5,""i"":2,""f"":2,""sb"":1,""sf"":1}]}]}]}";

        private BaseDifficulty originalMap;
        private float originalBpm;
        private BaseDifficulty map;
        private GameObject playbackRoot;
        private LightColorGroupEffect playback;
        private LightColorGroupContainer[] containers;
        private readonly RecordingLight[] lights = new RecordingLight[LightCount];
        private EventAppearanceSO appearance;

        // Real InsertData/UpdateTime provides the oracle; only the final renderer is replaced with a recording light.
        [SetUp]
        public void PrepareColliderPlayback()
        {
            var song = BeatSaberSongContainer.Instance;
            originalMap = song.Map;
            originalBpm = song.Info.BeatsPerMinute;
            song.Info.BeatsPerMinute = 100f;
            appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            LoadPlayback(MapJson);
        }

        // The interruption regression must reload a different synthetic map through the same real eight-light
        // effect without duplicating the recording-light registration or reflection container capture.
        private void LoadPlayback(string json)
        {
            var song = BeatSaberSongContainer.Instance;
            if (playback != null)
            {
                Object.DestroyImmediate(playback.ColorScheme);
                Object.DestroyImmediate(playbackRoot);
            }

            map = BeatmapFactory.GetDifficultyFromJson(JSON.Parse(json), "Collider wave", song.Info, song.MapDifficultyInfo);
            song.Map = map;
            playbackRoot = new GameObject("Collider wave deterministic playback");
            playbackRoot.SetActive(false);
            playback = playbackRoot.AddComponent<LightColorGroupEffect>();
            playback.ColorBoostEffect = playbackRoot.AddComponent<ColorBoostEffect>();
            playback.ColorScheme = ScriptableObject.CreateInstance<ColorSchemeSO>();
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
            containers = (LightColorGroupContainer[])typeof(LightColorGroupEffect)
                .GetField("idToContainer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(playback);
        }

        // Restore the scene's map/BPM even on failure; tests never edit the user's WIP files or use native audio playback.
        protected override void BeforeCleanup()
        {
            if (playback != null)
                Object.DestroyImmediate(playback.ColorScheme);
            Object.DestroyImmediate(playbackRoot);
            Object.DestroyImmediate(appearance);
            BeatSaberSongContainer.Instance.Map = originalMap;
            BeatSaberSongContainer.Instance.Info.BeatsPerMinute = originalBpm;
        }

        // Keep the current serialized arrangement and custom timing metadata distinct from the older Wave tests.
        [Test]
        public void FixtureMatchesCurrentColliderMap()
        {
            Assert.AreEqual(3, map.LightColorEventBoxGroups.Count);
            Assert.AreEqual(2, map.LightColorEventBoxGroups[1].Boxes.Count);
            Assert.AreEqual(29.75f, Node(1, 0, 2).JsonTime);
            Assert.AreEqual(39f, Node(1, 0, 3).JsonTime);
            Assert.AreEqual(22f, Node(1, 1).JsonTime);
            Assert.AreEqual(4, Node(1, 1).ChromaStrobeColorEasing);
            Assert.AreEqual(19, Node(1, 1).ChromaStrobeEasing);
            Assert.AreEqual(2f, Node(1, 1).ChromaStrobeInterval);
        }

        // The second all-light box cannot steal the first box's even lights, even though its node is later in time.
        [Test]
        public void SerializedFirstBoxWinsAndDistributionKeepsPhysicalOrder()
        {
            for (var light = 0; light < LightCount; light++)
            {
                var first = StateAt(light, 5f);
                Assert.AreSame(Node(0), first.Base);
                Assert.AreSame(light % 2 == 0 ? Node(1) : Node(1, 1), first.Next.Base);
                Assert.That(first.EndTime, Is.EqualTo(SongTime(light % 2 == 0 ? 11f : 22f + (0.4f * light))).Within(0.0001f));
                var later = StateAt(light, 40f);
                Assert.AreSame(light % 2 == 0 ? Node(1, 0, 3) : Node(1, 1), later.Base);
                Assert.AreSame(Node(2), later.Next.Base);
                // First-wins ownership must also materialize as per-light event states: every filtered node only
                // exists on the even lights its box claimed, while the same-group all-light node exists only on odd.
                Assert.That(HasState(light, Node(1, 0, 0)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 0 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 0, 1)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 1 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 0, 2)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 2 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 0, 3)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 3 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 1)), Is.EqualTo(light % 2 == 1), $"light {light}: the all-light node exists iff the light is odd");
            }
        }

        // A later separate all-lights group starts after the filtered box's final node, so every owned timeline
        // keeps its prior final source and converges on the later group's node without cancelling earlier children.
        [Test]
        public void DifferentAllLightsGroupAfterFilteredFinalTakesOverAllEightLights()
        {
            for (var light = 0; light < LightCount; light++)
            {
                var before = StateAt(light, 45f);
                Assert.AreSame(light % 2 == 0 ? Node(1, 0, 3) : Node(1, 1), before.Base);
                Assert.AreSame(Node(2), before.Next.Base);
                Assert.That(before.EndTime, Is.EqualTo(SongTime(46f)).Within(0.0001f));
                Assert.AreSame(Node(2), StateAt(light, 46f).Base);
                Assert.IsTrue(HasState(light, Node(1, 0, 3)) == (light % 2 == 0));
            }
        }

        // LightColorBeatmapEventDataBox.Unpack uses nodeBeat < nextElementStart: the beat-15 boundary child and
        // every later child are cancelled, while all eight lights transition to the interrupter's actual beat-17 node.
        [Test]
        public void DifferentAllLightsGroupInterruptsAndCancelsFilteredEventsAtOrAfterGroupStart()
        {
            LoadPlayback(InterruptedMapJson);
            var baseline = Node(0);
            var filteredSource = Node(1, 0, 0);
            var boundaryChild = Node(1, 0, 1);
            var laterChild = Node(1, 0, 2);
            var finalChild = Node(1, 0, 3);
            var interrupter = Node(2);

            for (var light = 0; light < LightCount; light++)
            {
                var even = light % 2 == 0;
                var before = StateAt(light, 14f);
                Assert.AreSame(even ? filteredSource : baseline, before.Base);
                Assert.AreSame(interrupter, before.Next.Base);
                Assert.That(before.EndTime, Is.EqualTo(SongTime(17f)).Within(0.0001f));
                Assert.That(HasState(light, filteredSource), Is.EqualTo(even));
                Assert.IsFalse(HasState(light, boundaryChild), $"light {light}: a prior child at the exact beat-15 ownership boundary must be omitted");
                Assert.IsFalse(HasState(light, laterChild), $"light {light}: a prior child after takeover must be omitted");
                Assert.IsFalse(HasState(light, finalChild), $"light {light}: the prior box must never resume after takeover");

                var sourceColor = even ? Color.red : Color.white;
                var progress = even ? (13.5f - 10f) / (17f - 10f) : 13.5f / 17f;
                AssertColor(
                    ColorAt(light, 13.5f),
                    Color.LerpUnclamped(sourceColor, Color.blue, progress),
                    0.002f,
                    $"light {light}: transition before the beat-17 all-light node");
                AssertColor(ColorAt(light, 17f), Color.blue, 0.002f, $"light {light}: all-light takeover");
                Assert.AreSame(interrupter, StateAt(light, 20f).Base);
            }
        }

        // Editing one light must preserve unrelated states and update the indexed retention bounds without rebuilding the ID.
        [Test]
        public void TimelineEditsRetainUnchangedLightsAndRefreshBounds()
        {
            var source = Node(0);
            var oddSource = Node(1, 1);
            var finalFiltered = Node(1, 0, 3);
            var end = Node(2);
            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.IsTrue(timeline.TryGetOutgoing(oddSource, 1, out var unchanged));
            var inserted = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{""b"":44,""g"":1,""e"":[{""f"":{""f"":2,""p"":0,""t"":8},""e"":[{""b"":0,""i"":1,""s"":1,""customData"":{""color"":[1,1,0]}}]}]}"));
            inserted.SetMap(map);
            inserted.RecomputeSongBpmTime();
            map.LightColorEventBoxGroups.Insert(2, inserted);
            GLSEventCommon.AddColorTransitionGroup(inserted);
            Assert.AreSame(timeline, GLSEventCommon.GetColorTimeline(source, LightCount),
                "An edit must update the existing indexed timeline rather than rebuilding every light's states.");
            Assert.IsTrue(timeline.TryGetOutgoing(oddSource, 1, out var afterInsert));
            Assert.AreSame(unchanged, afterInsert, "The unclaimed odd light's state must not be regenerated.");
            Assert.IsTrue(timeline.TryGetOutgoing(finalFiltered, 0, out var even));
            Assert.AreSame(inserted.Boxes[0].Events[0], even.Next.Base);
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(45f), null, retained);
            Assert.IsTrue(retained.Contains(inserted));
            map.LightColorEventBoxGroups.Remove(inserted);
            GLSEventCommon.RemoveColorTransitionGroup(inserted);
            Assert.AreSame(timeline, GLSEventCommon.GetColorTimeline(source, LightCount));
            Assert.IsTrue(timeline.TryGetOutgoing(finalFiltered, 0, out even));
            Assert.AreSame(end, even.Next.Base);
            Assert.IsTrue(timeline.TryGetOutgoing(oddSource, 1, out var afterRemove));
            Assert.AreSame(unchanged, afterRemove);
            retained.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(45f), null, retained);
            Assert.IsFalse(retained.Contains(inserted), "Retired group intervals must not survive in the viewport index.");
        }

        // Playback's initial sentinel is an internal held state, not an authored source extending an incoming ribbon before the map.
        [Test]
        public void FirstAuthoredNodeHasNoSentinelIncomingRibbon()
        {
            var timeline = GLSEventCommon.GetColorTimeline(Node(0), LightCount);
            for (var light = 0; light < LightCount; light++)
                Assert.IsFalse(timeline.TryGetIncoming(Node(0), light, out _),
                    "An initial sentinel must not become a visible incoming ribbon source.");
        }

        // One source now has different destinations across its width; hover must follow the visible physical strip, not light zero.
        [TestCase(0, 5f, true)]
        [TestCase(1, 5f, true)]
        [TestCase(7, 23.6f, true)]
        [TestCase(0, 20f, false)]
        public void RibbonHoverResolvesThePhysicalLightDestination(int light, float beat, bool hasTarget)
        {
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            try
            {
                owner.ObjectData = map.LightColorEventBoxGroups[0];
                owner.Setup();
                owner.PreviewEventData = Node(0);
                var ribbon = owner.lightGradientController;
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, Node(0), appearance, _ => false, LightCount);
                var renderer = (MeshRenderer)typeof(LightGradientController)
                    .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                var duration = ribbon.transform.localScale.x / (EditorScaleController.EditorScale * (4f / 3f));
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices;
                var uv = mesh.uv;
                var triangles = mesh.triangles;
                var a = triangles[0];
                var b = triangles[1];
                var c = triangles[2];
                var first = uv[b] - uv[a];
                var second = uv[c] - uv[a];
                var targetUv = new Vector2((SongTime(beat) - Node(0).SongBpmTime) / duration,
                    (LightCount - light - 0.5f) / LightCount);
                var delta = targetUv - uv[a];
                var determinant = (first.x * second.y) - (second.x * first.y);
                var weightB = ((delta.x * second.y) - (delta.y * second.x)) / determinant;
                var weightC = ((first.x * delta.y) - (first.y * delta.x)) / determinant;
                var point = renderer.transform.TransformPoint(vertices[a]
                    + ((vertices[b] - vertices[a]) * weightB) + ((vertices[c] - vertices[a]) * weightC));
                BeatmapRaycastCache.Invalidate();
                BeatmapRaycastCache.FirstHit = renderer.gameObject;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                // The pre-fix cache has no point field, so it still reaches the wrong-target assertion rather than a setup failure.
                typeof(BeatmapRaycastCache).GetField("FirstHitPoint")?.SetValue(null, point);
                Assert.AreEqual(hasTarget, GLSEventCommon.TryGetColorTransitionTarget(owner, Node(0), out var target),
                    "Masked portions of a ribbon must not resolve an endpoint for hover mutation.");
                if (hasTarget)
                    Assert.AreSame(light % 2 == 0 ? Node(1) : Node(1, 1), target,
                        "Hover must resolve the endpoint of the hit physical strip.");
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(owner.gameObject);
            }
        }

        // A 0->1 strobe ramp over five beats accumulates 2.5 cycles; it must not finish black and reset bright at beat 93.
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(7)]
        public void StartingBlackStrobeDoesNotSnapBrightAtBeat93(int light)
        {
            LoadPlayback(BlackStrobeStartMapJson);
            var before = ColorAt(light, 92.9999f);
            var at = ColorAt(light, 93f);
            var after = ColorAt(light, 93.0001f);
            Debug.Log($"[BlackStrobeStart] light={light} before={before} at={at} after={after}");
            AssertColor(before, at, 0.002f,
                "The beat-93 strobe must not snap from its black alternate phase to full primary brightness.");
            AssertColor(at, after, 0.002f, "The newly active strobe must continue smoothly after its boundary.");
            Assert.That(at.a, Is.EqualTo(0.5f).Within(0.002f), "The node's native phase-zero primary brightness must remain intact.");
        }

        // The same authored shifts, Back easing and phase anchor must reach every ribbon strip, not just playback.
        [TestCase(89.25f)]
        [TestCase(90f)]
        [TestCase(90.5f)]
        [TestCase(91f)]
        [TestCase(91.5f)]
        [TestCase(92f)]
        [TestCase(92.999f)]
        public void StartingBlackStrobeRibbonMatchesPreview(float beat)
        {
            LoadPlayback(BlackStrobeStartMapJson);
            AssertRibbonPixels(0, 0, 0, beat, -1);
        }

        // The inner lanes are the control: each keeps its own four lights and their complementary pulse phases.
        [TestCase(100.125f)]
        [TestCase(100.625f)]
        public void InnerAlternatingChunkRibbonsMatchPlayback(float beat)
        {
            LoadAlternatingChunks();
            AssertRibbonPixels(0, 0, 0, beat, -1);
            AssertRibbonPixels(0, 1, 0, beat, -1);
        }

        // The actual outer preview deduplicates the two timestamps, but its single ribbon must retain both boxes' owned strips.
        [TestCase(100.125f, false)]
        [TestCase(100.625f, false)]
        [TestCase(100.125f, true)]
        [TestCase(100.625f, true)]
        public void OuterAlternatingChunkRibbonsIncludeBothBoxes(float beat, bool rebindWithoutSecondBox)
        {
            LoadAlternatingChunks();
            var root = new GameObject("Alternating chunk outer preview test");
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            owner.transform.SetParent(root.transform, false);
            owner.ObjectData = map.LightColorEventBoxGroups[0];
            owner.Setup();
            owner.GlsLightCount = LightCount;
            var outerAppearance = (GLSGroupAppearanceSO)typeof(GLSGroupContainer)
                .GetField("glsGroupAppearance", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
            var appearanceField = typeof(GLSGroupAppearanceSO).GetField("eventAppearance", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalAppearance = appearanceField.GetValue(outerAppearance);
            var originalOpacity = Settings.Instance.GLSOuterTrackGhostNodeOpacity;
            Material material = null;
            try
            {
                appearanceField.SetValue(outerAppearance, appearance);
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
                owner.ConfigurePreviewNodes(_ => false);
                // Reusing the same outer renderer must clear rows formerly owned by a removed sibling box.
                if (rebindWithoutSecondBox)
                {
                    var json = JSON.Parse(AlternatingChunksMapJson);
                    json["lightColorEventBoxGroups"][0]["e"].Remove(1);
                    LoadPlayback(json.ToString());
                    playback.ColorScheme.EnvironmentLeftColor = Color.white;
                    owner.ObjectData = map.LightColorEventBoxGroups[0];
                    owner.ConfigurePreviewNodes(_ => false);
                }
                var ghosts = (System.Collections.Generic.List<GLSGroupContainer>)typeof(GLSGroupContainer)
                    .GetField("previewGhosts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
                Assert.AreEqual(1, ghosts.Count, "Keep one selectable preview per timestamp; do not overlay duplicate node bodies.");
                var ribbon = owner.lightGradientController;
                Assert.IsTrue(ribbon.gameObject.activeSelf);
                var renderer = (MeshRenderer)typeof(LightGradientController)
                    .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                var progress = (SongTime(beat) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                var secondHalf = beat - Mathf.Floor(beat) >= 0.5f;
                for (var light = 0; light < LightCount; light++)
                {
                    var firstBox = (light / 2) % 2 == 0;
                    var expectedOn = firstBox == secondHalf && (!rebindWithoutSecondBox || firstBox);
                    Assert.That(ColorAt(light, beat).a, Is.EqualTo(expectedOn ? 1f : 0f).Within(0.0001f),
                        $"Playback control: light {light} at {beat} must belong to the expected alternating chunk.");
                    var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress,
                        (LightCount - light - 0.5f) / LightCount);
                    if (expectedOn)
                        Assert.Greater(pixel.maxColorComponent, 0.05f, $"Outer preview omitted lit light {light} at {beat}.");
                    else
                        Assert.Less(pixel.maxColorComponent, 0.02f, $"Outer preview lit the off-phase light {light} at {beat}.");
                }
            }
            finally
            {
                appearanceField.SetValue(outerAppearance, originalAppearance);
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = originalOpacity;
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(owner.gameObject);
                Object.DestroyImmediate(root);
            }
        }

        // A fixed non-black environment color isolates alternating brightness while retaining the supplied node/filter data verbatim.
        private void LoadAlternatingChunks()
        {
            LoadPlayback(AlternatingChunksMapJson);
            appearance.RedColor = Color.white;
            playback.ColorScheme.EnvironmentLeftColor = Color.white;
        }

        // B's no-strobe transition and both final D paths must converge to primary RGBA regardless of the residual phase.
        [TestCase(0, 17f)]
        [TestCase(0, 46f)]
        [TestCase(1, 46f)]
        [TestCase(3, 46f)]
        [TestCase(6, 46f)]
        [TestCase(7, 46f)]
        public void NoStrobeTransitionConvergesBeforeItsBoundary(int light, float targetBeat)
        {
            var before = ColorAt(light, targetBeat - 0.0001f);
            var after = ColorAt(light, targetBeat);
            Debug.Log($"[ColliderWave] light={light} target={targetBeat} before={before} after={after}");
            AssertColor(before, after, 0.002f, "A transition must not snap from residual strobe color to primary at its boundary.");
        }

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

        // strobeEasing is node-scoped in ChromaGLS, so C's outgoing pulse must not silently adopt D's default cubic fade.
        [Test]
        public void COutgoingPulseUsesItsAuthoredStrobeEasing()
        {
            ColorAt(1, 30f);
            Assert.NotNull(containers[1].Tween.StrobeEasing,
                "C's authored strobeEasing must remain active until D instead of disappearing on the outgoing interval.");
            Assert.That(containers[1].Tween.StrobeEasing(0.3f), Is.EqualTo(Easing.FromID(19)(0.3f)).Within(0.00001f));
        }

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

        // B and C need the preceding group's incoming strips in their own inner lanes without losing their outgoing ribbons.
        [TestCase(0)]
        [TestCase(1)]
        public void InnerFirstNodeHasIncomingRibbonFromA(int box)
        {
            var collection = Object.FindAnyObjectByType<GLSEventGridContainer>();
            var container = (GLSEventContainer)collection.CreateContainer();
            var innerAppearance = ScriptableObject.CreateInstance<GLSEventAppearanceSO>();
            try
            {
                container.ObjectData = Node(1, box);
                container.Setup();
                container.GlsLightCount = LightCount;
                typeof(GLSEventAppearanceSO).GetField("eventAppearance", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(innerAppearance, appearance);
                innerAppearance.SetAppearance(container);
                container.UpdateGridPosition();
                innerAppearance.UpdateTransitionRibbon(container, _ => false);
                var incoming = false;
                var outgoing = false;
                foreach (var ribbon in container.GetComponentsInChildren<LightGradientController>(true))
                {
                    if (!ribbon.gameObject.activeSelf)
                        continue;
                    if (ribbon.transform.localPosition.z < -0.1f)
                        incoming = true;
                    else
                        outgoing = true;
                }
                Assert.IsTrue(incoming,
                    $"Inner box {box} must show A's incoming transition on its own controlled strips.");
                Assert.IsTrue(outgoing,
                    $"Inner box {box} must retain its outgoing transition while displaying the cross-group incoming one.");
            }
            finally
            {
                Object.DestroyImmediate(container.gameObject);
                Object.DestroyImmediate(innerAppearance);
            }
        }

        // All raster cases compare the actual shader against the same production light sample and its ownership mask.
        private void AssertRibbonPixels(int group, int box, int node, float beat, int onlyLight)
        {
            var source = Node(group, box, node);
            var ribbonObject = new GameObject("Collider per-light ribbon");
            Material material = null;
            Material reference = null;
            var referenceTexture = new Texture2D(LightCount, 4, TextureFormat.RGBAFloat, false, true);
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, source, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf, $"Source {source.JsonTime} must have a ribbon to its lights' next nodes.");
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                reference = new Material(Shader.Find("ChroMapper/Object/Basic Gradient")) { enableInstancing = false };
                reference.SetFloat("_UseLightDistribution", 1f);
                reference.SetFloat("_LightDistributionWidth", LightCount);
                reference.SetTexture("_LightDistributionTex", referenceTexture);
                var duration = ribbon.transform.localScale.x / (EditorScaleController.EditorScale * (4f / 3f));
                var progress = (SongTime(beat) - source.SongBpmTime) / duration;
                for (var light = 0; light < LightCount; light++)
                {
                    if (onlyLight >= 0 && light != onlyLight)
                        continue;
                    var state = StateAt(light, beat);
                    var expected = ReferenceEquals(state.Base, source) ? ColorAt(light, beat) : Color.black;
                    var colors = new Color[LightCount * 4];
                    Array.Fill(colors, expected);
                    referenceTexture.SetPixels(colors);
                    referenceTexture.Apply(false, false);
                    var lane = (LightCount - light - 0.5f) / LightCount;
                    var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                    var expectedPixel = GLSColorTransitionCacheTest.RenderGradientPixel(reference, 0.5f, lane);
                    // Separate prepared-state mismatches from shader interpolation errors without changing either rendering path.
                    if (Mathf.Abs(pixel.r - expectedPixel.r) > 0.02f
                        || Mathf.Abs(pixel.g - expectedPixel.g) > 0.02f
                        || Mathf.Abs(pixel.b - expectedPixel.b) > 0.02f)
                    {
                        var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
                        if (timeline.TryGetOutgoing(source, light, out var preparedState))
                        {
                            var prepared = new LightColorTween();
                            timeline.ConfigureTween(prepared, preparedState, appearance, _ => false);
                            prepared.UpdateTime(SongTime(beat));
                            var texture = properties.GetTexture(Shader.PropertyToID("_LightDistributionTex")) as Texture2D;
                            Debug.Log($"[ColliderRibbonMismatch] beat={beat} light={light} uv={progress} " +
                                $"live={expected} prepared={prepared.Color} start={prepared.StartColor} end={prepared.EndColor} " +
                                $"strobeStart={prepared.StartStrobeColor} strobeEnd={prepared.EndStrobeColor} " +
                                $"easeRow={texture.GetPixel(LightCount - light - 1, 8)} " +
                                $"texStart={texture.GetPixel(LightCount - light - 1, 0)} texEnd={texture.GetPixel(LightCount - light - 1, 1)} " +
                                $"times={texture.GetPixel(LightCount - light - 1, 4)} rates={texture.GetPixel(LightCount - light - 1, 5)} " +
                                $"levels={texture.GetPixel(LightCount - light - 1, 6)} flags={texture.GetPixel(LightCount - light - 1, 7)} " +
                                $"duration={material.GetFloat("_LightTimelineDuration")}");
                        }
                    }
                    AssertColor(pixel, expectedPixel, 0.02f,
                        $"source={source.JsonTime} light={light} beat={beat}: ribbon pixels must match the same light's preview state.");
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(referenceTexture);
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // These helpers query the actual generated state/tween rather than replaying filter or strobe rules in the test.
        private BaseLightColorBase Node(int group, int box = 0, int node = 0) =>
            map.LightColorEventBoxGroups[group].Boxes[box].Events[node];

        private float SongTime(float beat) => (float)map.JsonTimeToSongBpmTime(beat);

        // State membership directly proves whether EventGroupEffect unpacked a node for this physical light.
        private bool HasState(int light, BaseLightColorBase node)
        {
            foreach (var state in containers[light].EventContainer.Collection)
            {
                if (ReferenceEquals(state.Base, node)) return true;
            }

            return false;
        }

        // Inspecting an endpoint must not move the playback cursor and cause UpdateTime to skip rebuilding its tween.
        private LightColorEventStateData StateAt(int light, float beat) =>
            containers[light].EventContainer.GetStateAt(SongTime(beat)).state;

        private Color ColorAt(int light, float beat)
        {
            playback.UpdateTime(false, SongTime(beat));
            return lights[light].Color;
        }

        private static void AssertColor(Color actual, Color expected, float tolerance, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), message);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), message);
        }

        // Record the production light output without a renderer or audio clock dependency.
        private class RecordingLight : LightController
        {
            protected override bool Initialize() => true;
            public override void SetColor(Color color) => Color = color;
        }
    }
}
