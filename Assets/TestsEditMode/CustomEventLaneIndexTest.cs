using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class CustomEventLaneIndexTest
    {
        private int previousMapVersion;

        [SetUp]
        public void SetUp()
        {
            previousMapVersion = Settings.Instance.MapVersion;
            Settings.Instance.MapVersion = 3;
        }

        [TearDown]
        public void TearDown() => Settings.Instance.MapVersion = previousMapVersion;

        [Test]
        public void EveryCategoryHasASeparatorAndCompactTrackLabels()
        {
            var path = Event("path");
            path.Type = "AssignPathAnimation";
            var player = Event("player");
            player.Type = "AssignPlayerToTrack";
            var index = Build(new[] { path, player });
            var pathEnd = index.Lanes.Where(lane => lane.Type == path.Type).Max(lane => lane.Column);
            var playerStart = index.Lanes.Where(lane => lane.Type == player.Type).Min(lane => lane.Column);
            Assert.That(index.GetLaneAtColumn(pathEnd + 1), Is.Null, "Each category must end with a real gap.");
            Assert.That(playerStart, Is.EqualTo(pathEnd + 2));
            Assert.That(index.GetLaneAtColumn(index.GetLaneIndex(path)).Label, Is.EqualTo("path"));
            Assert.That(index.GetLaneAtColumn(index.GetLaneIndex(player)).Label, Is.EqualTo("player"));
        }

        [Test]
        public void AnimateTrackLanesFollowOtherTypesWithABreakAndCompactLabels()
        {
            var animation = Event(JSON.Parse("[\"B\",\"A\"]"));
            animation.Type = "AnimateTrack";
            var other = Event("fog");
            other.Type = "AnimateComponent";
            var index = Build(new[] { animation, other });
            Assert.That(index.Lanes.Last().Type, Is.EqualTo("AnimateTrack"));
            Assert.That(index.GetLaneIndex(animation), Is.GreaterThan(index.GetLaneIndex(other) + 1));
            Assert.That(
                index.Lanes.Where(lane => lane.Type == "AnimateTrack").Select(lane => lane.Label),
                Does.Contain("\"A\" + \"B\""));
            Assert.That(index.Lanes.Any(lane => lane.Type == "AnimateTrack" && lane.DataKey == null), Is.False);
            Assert.That(index.Lanes.Single(lane => lane.Type == "AnimateComponent" && lane.DataKey != null).Label,
                Is.EqualTo("fog"));
        }

        [Test]
        public void EmptyMapOffersAnAnimateTrackLane()
        {
            var index = Build(System.Array.Empty<BaseCustomEvent>());
            Assert.That(index.Lanes.Select(lane => lane.Label), Is.EqualTo(new[] { "AnimateTrack" }));
        }

        [Test]
        public void ReferencedAnimationTracksRemainUntilTheirLastObjectIsRemoved()
        {
            var animation = Event("shared");
            animation.Type = "AnimateTrack";
            var index = Build(new[] { animation });
            var first = new BaseNote { CustomTrack = "shared" };
            var second = new BaseNote { CustomTrack = "shared" };
            index.AddTrackReferences(first);
            index.AddTrackReferences(second);
            index.Remove(animation);
            index.RemoveTrackReferences(animation);
            index.SortLanes();
            Assert.That(index.Lanes.Any(lane => lane.Label == "shared"), Is.True);
            Assert.That(index.Lanes.Any(lane => lane.Label == "AnimateTrack"), Is.True);
            index.RemoveTrackReferences(first);
            index.SortLanes();
            Assert.That(index.Lanes.Any(lane => lane.Label == "shared"), Is.True);
            index.RemoveTrackReferences(second);
            index.SortLanes();
            Assert.That(index.Lanes.Select(lane => lane.Label), Is.EqualTo(new[] { "AnimateTrack" }));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ObjectArraysAndMaterialTracksCreateIndependentEmptyLanes(int version)
        {
            Settings.Instance.MapVersion = version;
            var index = Build(System.Array.Empty<BaseCustomEvent>());
            var note = new BaseNote { CustomTrack = JSON.Parse("[\"B\",\"A\"]") };
            var material = new BaseMaterial { Track = "material" };
            index.AddTrackReferences(note);
            index.AddTrackReferences(material);
            index.SortLanes();
            Assert.That(
                index.Lanes.Select(lane => lane.Label),
                Is.EqualTo(new[] { "AnimateTrack", "A", "B", "material" }));
            var placed = new BaseCustomEvent();
            index.Lanes.Single(lane => lane.Label == "A").ApplyTo(placed);
            Assert.That(placed.Data[placed.CustomKeyTrack].Value, Is.EqualTo("A"));
            note.CustomTrack = "renamed";
            index.AddTrackReferences(note);
            index.SortLanes();
            Assert.That(
                index.Lanes.Select(lane => lane.Label),
                Is.EqualTo(new[] { "AnimateTrack", "material", "renamed" }));
            index.RemoveTrackReferences(material);
            index.SortLanes();
            Assert.That(index.Lanes.Any(lane => lane.Label == "material"), Is.False);
        }

        [Test]
        public void OverlappingGroupsLeadTheirTracksRecursively()
        {
            var events = new[]
            {
                Event("f"), Event("e"), Event("d"), Event("c"), Event("b"), Event("a"),
                Event(JSON.Parse("[\"c\",\"d\"]")),
                Event(JSON.Parse("[\"b\",\"c\"]")),
                Event(JSON.Parse("[\"a\",\"b\",\"c\",\"d\"]"))
            };
            var index = Build(events);
            Assert.That(
                Targets(index),
                Is.EqualTo(new[]
                {
                    "[\"a\",\"b\",\"c\",\"d\"]", "\"a\"", "[\"b\",\"c\"]", "\"b\"",
                    "[\"c\",\"d\"]", "\"c\"", "\"d\"", "\"e\"", "\"f\""
                }));
            var reversed = Build(events.Reverse().ToArray());
            Assert.That(Targets(reversed), Is.EqualTo(Targets(index)), "File order must not change lane order.");
        }

        [Test]
        public void NestedGroupsKeepNonAdjacentMemberTracksTogether()
        {
            var index = Build(new[]
            {
                Event("a"), Event("b"), Event("c"), Event("d"),
                Event(JSON.Parse("[\"a\",\"c\"]")),
                Event(JSON.Parse("[\"a\",\"b\",\"c\",\"d\"]"))
            });
            Assert.That(
                Targets(index),
                Is.EqualTo(new[] { "[\"a\",\"b\",\"c\",\"d\"]", "[\"a\",\"c\"]", "\"a\"", "\"c\"", "\"b\"", "\"d\"" }));
        }

        [Test]
        public void TrackSetsIgnoreOrderAndDuplicatesWithoutRewritingEvents()
        {
            var first = Event(JSON.Parse("[\"b\",\"a\",\"b\"]"));
            var second = Event(JSON.Parse("[\"a\",\"b\"]"));
            var scalar = Event("a");
            var singleton = Event(JSON.Parse("[\"a\"]"));
            var original = first.ToJson().ToString();
            var index = Build(new[] { first, second, scalar, singleton });
            Assert.That(index.GetLaneIndex(first), Is.EqualTo(index.GetLaneIndex(second)));
            Assert.That(index.GetLaneIndex(scalar), Is.EqualTo(index.GetLaneIndex(singleton)));
            Assert.That(first.ToJson().ToString(), Is.EqualTo(original));
            Assert.That(index.Lanes.Count(lane => lane.Type == "LaneTest"), Is.EqualTo(2));
            Assert.That(index.Remove(first), Is.False);
            Assert.That(index.Remove(second), Is.True);
            index.SortLanes();
            Assert.That(index.Lanes.Count(lane => lane.Type == "LaneTest"), Is.EqualTo(1));
            Assert.That(index.GetLaneIndex(singleton), Is.EqualTo(0));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void PlacementFillsVersionSpecificTrackAndParentKeys(int version)
        {
            Settings.Instance.MapVersion = version;
            var ev = Event(JSON.Parse("[\"right\",\"left\"]"));
            var parent = new BaseCustomEvent { Type = "AssignTrackParent" };
            parent.Data = new JSONObject { [parent.DataKeyParentTrack] = "root" };
            var index = Build(new[] { ev, parent });
            var placed = new BaseCustomEvent { Type = "OldType", Data = new JSONObject { ["old"] = 1 } };
            index.GetLaneAtColumn(index.GetLaneIndex(ev)).ApplyTo(placed);
            Assert.That(placed.Type, Is.EqualTo(ev.Type));
            Assert.That(placed.CustomTrack.ToString(), Is.EqualTo("[\"left\",\"right\"]"));
            Assert.That(
                placed.ToJson()[placed.KeyData][placed.CustomKeyTrack].ToString(),
                Is.EqualTo(placed.CustomTrack.ToString()));
            Assert.That(placed.Data.HasKey("old"), Is.False);
            placed.CustomTrack[0] = "edited";
            index.GetLaneAtColumn(index.GetLaneIndex(ev)).ApplyTo(placed);
            Assert.That(placed.CustomTrack[0].Value, Is.EqualTo("left"), "Placement must clone the lane target.");
            index.GetLaneAtColumn(index.GetLaneIndex(parent)).ApplyTo(placed);
            Assert.That(placed.DataParentTrack.Value, Is.EqualTo("root"));
            Assert.That(placed.CustomTrack, Is.Null);
            Assert.That(placed.DataChildrenTracks, Is.Null);
            Assert.That(placed.ToJson()[placed.KeyData][placed.DataKeyParentTrack].Value, Is.EqualTo("root"));
        }

        [Test]
        public void TypeLanesRemainAvailableAndDisabledModeHasOnlyOriginalLanes()
        {
            var first = Event("same");
            var second = Event("same");
            second.Type = "OtherType";
            var noTrack = Event(null);
            var index = Build(new[] { first, second, noTrack });
            Assert.That(index.GetLaneIndex(first), Is.Not.EqualTo(index.GetLaneIndex(second)));
            Assert.That(index.GetLaneAtColumn(index.GetLaneIndex(noTrack)).DataKey, Is.Null);
            index.Rebuild(new[] { "LaneTest", "OtherType" }, new[] { first, second, noTrack }, false);
            Assert.That(index.Lanes.Select(lane => lane.Label), Is.EqualTo(new[] { "LaneTest", "OtherType" }));
            Assert.That(index.GetLaneIndex(first), Is.EqualTo(0));
            Assert.That(index.GetLaneIndex(noTrack), Is.EqualTo(0));
            Assert.That(index.GetLaneIndex(second), Is.EqualTo(1));
            var placed = Event("old");
            index.Lanes[0].ApplyTo(placed);
            Assert.That(placed.CustomTrack, Is.Null);
            Assert.That(placed.Data.Count, Is.Zero);
        }

        [Test]
        public void DistinctTrackNamesCannotCollideWithGroupLabels()
        {
            var single = Event("a, b");
            var group = Event(JSON.Parse("[\"a\",\"b\"]"));
            var index = Build(new[] { single, group });
            Assert.That(index.GetLaneIndex(single), Is.Not.EqualTo(index.GetLaneIndex(group)));
            index.Rebuild(new[] { "LaneTest" }, new[] { single }, true);
            Assert.That(
                index.Lanes.Count(lane => lane.Type == "LaneTest"),
                Is.EqualTo(1),
                "A map reload must discard the previous map's generated lanes.");
        }

        private static string[] Targets(CustomEventLaneIndex index) => index.Lanes
            .Where(lane => lane.Type == "LaneTest" && lane.DataKey != null)
            .Select(lane =>
            {
                var ev = new BaseCustomEvent();
                lane.ApplyTo(ev);
                return ev.Data[lane.DataKey].ToString();
            }).ToArray();

        private static CustomEventLaneIndex Build(BaseCustomEvent[] events)
        {
            var index = new CustomEventLaneIndex();
            index.Rebuild(events.Select(ev => ev.Type).Distinct(), events, true);
            return index;
        }

        private static BaseCustomEvent Event(JSONNode track)
        {
            var ev = new BaseCustomEvent { Type = "LaneTest" };
            ev.Data = new JSONObject { [ev.CustomKeyTrack] = track };
            return ev;
        }
    }
}
