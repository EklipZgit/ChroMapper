using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // GLSColorTimeline segment bounds: head/tail sentinel extents, retention intervals, incremental
    // Add/Remove edits, and the ribbon bodies they drive at the start and end of the song.
    public class GLSColorTimelineBoundsTest : GLSColorPlaybackTestBase
    {
        // A lane ending in an unlit hold must render nothing past it: no tail ribbon and no retention.
        private const string DarkTailMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":40,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,1]}},
                {""b"":5,""c"":0,""s"":0,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,0,1]}}]}]}]}";

        // A single transition node owns both boundary directions: lit before it via the sentinel fade-in and lit after it by its hold.
        private const string SoloTransitionMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":50,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,1,0]}}]}]}]}";

        // An instant first node holds the pre-map black instead, so nothing may extend before its own beat.
        private const string InstantHeadMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":50,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,1,0]}}]}]}]}";

        // A first node with a transition easing owns a lit pre-node fade-in that must reach the song start.
        private const string BpmLitHeadJson = @"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":240}],""lightColorEventBoxGroups"":[
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,1,0]}}]}]}]}";

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

        // The last authored node's segment keeps every light lit until the song ends, so its ribbon must extend past the node instead of ending there.
        [TestCase(0, 47f)]
        [TestCase(1, 60f)]
        [TestCase(7, 99.5f)]
        public void LitTailExtendsOutgoingRibbonToSongEnd(int light, float beat) =>
            AssertRibbonPixels(2, 0, 0, beat, light);

        // The held tail must also retain its source group, or the visible strip disappears when the node scrolls offscreen.
        [Test]
        public void LitTailRetainsSourceGroupUntilSongEnd()
        {
            var final = Node(2);
            var timeline = GLSEventCommon.GetColorTimeline(final, LightCount);
            Assert.IsTrue(timeline.TryGetBounds(final, out _, out var end));
            Assert.That(end, Is.GreaterThan(SongTime(99f)),
                "A lit held tail must extend the source interval toward the loaded song's end.");
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(80f), null, retained);
            Assert.IsTrue(retained.Contains(map.LightColorEventBoxGroups[2]),
                "The final node's group must stay loaded while its held tail ribbon remains visible.");
        }

        // After a cross-group takeover the new last node holds every light; its tail must still reach the song end.
        [TestCase(0, 25f)]
        [TestCase(7, 60f)]
        public void InterruptedTailExtendsAfterTakeover(int light, float beat)
        {
            LoadPlayback(InterruptedMapJson);
            AssertRibbonPixels(2, 0, 0, beat, light);
        }

        // A lane that only holds black has nothing to show: no tail ribbon and no retention past the last lit segment.
        [Test]
        public void DarkTailDoesNotExtendOrRetain()
        {
            LoadPlayback(DarkTailMapJson);
            var final = Node(0, 0, 1);
            Assert.That(ColorAt(0, 50f).a, Is.EqualTo(0f).Within(0.001f),
                "Playback control: the lane holds black after the dark node.");
            var timeline = GLSEventCommon.GetColorTimeline(final, LightCount);
            Assert.IsFalse(timeline.TryGetBounds(final, out _, out _),
                "A dark terminal hold contributes no retention interval.");
            var ribbonObject = new GameObject("Dark tail ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, final, appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf, "A lane that only holds black must not draw a tail ribbon.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(60f), null, retained);
            Assert.IsFalse(retained.Contains(map.LightColorEventBoxGroups[0]),
                "Nothing remains visible after the dark node, so its group must not stay loaded.");
        }

        // A transition-type first node eases in from the pre-map sentinel, so each owned strip is lit before the node and must reach back to beat zero.
        [TestCase(0, 0, 99.5f)]
        [TestCase(0, 0, 99.25f)]
        [TestCase(0, 0, 50f)]
        [TestCase(0, 1, 99.5f)]
        [TestCase(0, 1, 99.25f)]
        public void FirstNodeHeadExtendsInnerIncomingRibbonToMapStart(int group, int box, float beat)
        {
            LoadAlternatingChunks();
            var firstBox = box == 0;
            AssertIncomingRibbonPixels(group, box, 0, beat, light => ((light / 2) % 2 == 0) == firstBox);
        }

        // The deduplicated outer body also owns each light's pre-node fade; no other outer body draws it forward.
        [TestCase(50f)]
        [TestCase(99.25f)]
        public void FirstNodeHeadExtendsOuterIncomingRibbonToMapStart(float beat)
        {
            LoadAlternatingChunks();
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            try
            {
                owner.ObjectData = map.LightColorEventBoxGroups[0];
                owner.Setup();
                owner.PreviewEventData = Node(0);
                Assert.IsNotNull(owner.IncomingLightGradientController,
                    "A spawned outer body must own a dedicated incoming ribbon controller.");
                var ribbon = owner.IncomingLightGradientController;
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0), appearance, _ => false, LightCount, aggregateSameTimeBoxes: true);
                Assert.IsTrue(ribbon.gameObject.activeSelf,
                    "The outer preview must project every light's lit fade-in before the shared first timestamp.");
                Assert.IsTrue(ribbon.IsIncomingColorTransition);
                Assert.IsTrue(ribbon.AggregatesSameTimeBoxes);
                Assert.That(ribbon.ColorTimelineStart, Is.LessThanOrEqualTo(SongTime(0f) + 0.001f),
                    "A lit head must extend the outer ribbon back to the map start.");
                var renderer = (MeshRenderer)typeof(LightGradientController)
                    .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                AssertIncomingStripPixels(renderer, ribbon, beat, _ => true);
            }
            finally
            {
                Object.DestroyImmediate(owner.gameObject);
            }
        }

        // A lone transition node owns both boundary directions at once: the pre-node fade-in and the post-node hold.
        [Test]
        public void SoloTransitionNodeExtendsBothDirections()
        {
            LoadPlayback(SoloTransitionMapJson);
            AssertIncomingRibbonPixels(0, 0, 0, 25f, _ => true);
            AssertIncomingRibbonPixels(0, 0, 0, 49.75f, _ => true);
            AssertRibbonPixels(0, 0, 0, 75f, -1);
        }

        // The pre-node fade must also retain the target's container while its incoming strip is on screen.
        [Test]
        public void LitHeadRetainsTargetGroupFromMapStart()
        {
            LoadPlayback(SoloTransitionMapJson);
            var node = Node(0);
            var timeline = GLSEventCommon.GetColorTimeline(node, LightCount);
            Assert.IsTrue(timeline.TryGetBounds(node, out var start, out _));
            Assert.That(start, Is.LessThanOrEqualTo(SongTime(0f) + 0.001f),
                "A lit sentinel fade must extend the source interval back to the map start.");
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(5f), null, retained);
            Assert.IsTrue(retained.Contains(map.LightColorEventBoxGroups[0]),
                "The first node's group must stay loaded while its incoming head ribbon remains visible.");
        }

        // An instant first node keeps the pre-map black, so neither an incoming ribbon nor early retention may appear.
        [Test]
        public void InstantFirstNodeDoesNotExtendIncoming()
        {
            LoadPlayback(InstantHeadMapJson);
            var node = Node(0);
            Assert.That(ColorAt(0, 49f).a, Is.EqualTo(0f).Within(0.001f),
                "Playback control: an instant first node keeps its lights dark until its beat.");
            var ribbonObject = new GameObject("Instant head ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(ribbon, node, appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf,
                    "An instant first node has no lit pre-node segment, so no incoming ribbon may render.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
            Assert.IsTrue(GLSEventCommon.TryGetColorRibbonBounds(node, LightCount, out var start, out var end));
            Assert.That(start, Is.EqualTo(node.SongBpmTime).Within(0.001f),
                "Retention must begin at the node itself, not at the pre-map sentinel.");
            Assert.That(end, Is.GreaterThan(SongTime(99f)),
                "The instant node's held output still needs its tail interval through the song end.");
        }

        // The lit pre-node fade-in already reaches the map start; under a BPM event it must still do so.
        [Test]
        public void BpmScaledLitHeadStillReachesSongStart()
        {
            LoadPlayback(BpmLitHeadJson);
            var node = Node(0);
            var ribbonObject = new GameObject("BPM head ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(ribbon, node, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf);
                Assert.That(ribbon.ColorTimelineStart, Is.LessThanOrEqualTo(SongTime(0f) + 0.001f),
                    "A lit head must extend the incoming ribbon back to the song start.");
                Assert.That(ribbon.ColorTimelineDuration,
                    Is.EqualTo(SongTime(4f)).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
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
    }
}
