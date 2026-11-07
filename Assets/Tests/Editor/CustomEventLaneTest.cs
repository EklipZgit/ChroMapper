using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;

namespace Tests.Editor
{
    public class CustomEventLaneTest : TestBase
    {
        private bool previousSplit;
        private bool previousNodeEditorActive;
        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        [SetUp]
        public void EnableSplit()
        {
            previousSplit = Settings.Instance.SplitCustomEventsByTrack;
            previousNodeEditorActive = NodeEditorController.IsActive;
            Settings.ApplyOptionByName(nameof(Settings.SplitCustomEventsByTrack), true);
        }

        protected override void AfterCleanup()
        {
            NodeEditorController.IsActive = previousNodeEditorActive;
            Settings.ApplyOptionByName(nameof(Settings.SplitCustomEventsByTrack), previousSplit);
        }

        [Test]
        public void EveryCategoryHasItsOwnGridAndGlsStyleSideHeading()
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.Gameplay;
            var collection = Collection();
            var types = new[] { "AssignPathAnimation", "AssignTrackParent", "AssignPlayerToTrack", "AnimateTrack" };
            foreach (var type in types)
            {
                var ev = Event("categoryTest");
                ev.Type = type;
                if (type == "AssignTrackParent")
                    ev.Data = new JSONObject { [ev.DataKeyParentTrack] = "categoryParent" };

                collection.SpawnObject(ev);
            }

            var grids = new HashSet<GridLane>();
            GridLane previousGrid = null;
            foreach (var section in collection.LaneSections)
            {
                var heading = GetCategoryHeading(section.Type);
                var categoryGrid = heading.GetComponentInParent<GridLane>();
                Assert.That(grids.Add(categoryGrid), Is.True, "Every category needs its own grid section.");
                Assert.That(categoryGrid.Lane, Is.EqualTo(section.LaneCount));
                Assert.That(
                    Vector3.Dot(heading.transform.right, categoryGrid.transform.forward),
                    Is.GreaterThan(0.999f),
                    "Category headings must follow the GLS side-label orientation.");
                if (previousGrid != null)
                {
                    var end = previousGrid.transform.TransformPoint(Vector3.right * previousGrid.Lane).x;
                    Assert.That(
                        categoryGrid.transform.position.x - end,
                        Is.EqualTo(BeatmapConstant.LaneSize).Within(0.0001f));
                }

                previousGrid = categoryGrid;
            }
        }

        [Test]
        public void ObjectTrackAssignmentsExposeEmptyAnimationLanesThroughUndoRedo()
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.Gameplay;
            var collection = Collection();
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 1,
                Color = 0,
                PosX = 0,
                PosY = 0,
                CutDirection = 1,
                CustomData = new JSONObject { ["track"] = "unanimated" }
            });
            collection.RefreshPool(false);
            Assert.That(
                collection.Lanes.Any(lane => lane.Type == "AnimateTrack" && lane.Label == "unanimated"),
                Is.True);
            NodeEditorController.IsActive = true;
            SelectionController.Select(note);
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            nodeEditor.ObjectWasSelected();
            var json = note.ToJson().Clone();
            json["customData"][note.CustomKeyTrack] = "renamed";
            nodeEditor.NodeEditor_EndEdit(json.ToString());
            nodeEditor.Close();
            var notes = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var edited = notes.MapObjects.Single(item => item.CustomTrack?.Value == "renamed");
            collection.RefreshPool(false);
            Assert.That(collection.Lanes.Any(lane => lane.Label == "unanimated"), Is.False);
            Assert.That(collection.Lanes.Any(lane => lane.Type == "AnimateTrack" && lane.Label == "renamed"), Is.True);
            PlaceUtils.Undo();
            collection.RefreshPool(false);
            Assert.That(collection.Lanes.Any(lane => lane.Label == "unanimated"), Is.True);
            PlaceUtils.Redo();
            collection.RefreshPool(false);
            PlaceUtils.Delete(edited);
            collection.RefreshPool(false);
            Assert.That(collection.Lanes.Any(lane => lane.Label == "renamed"), Is.False);
        }

        [Test]
        public void AnimationSectionHasARealGapAndMatchingContainerPositions()
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.Gameplay;
            var collection = Collection();
            var other = Event("FOG");
            var animation = Event("THING");
            animation.Type = "AnimateTrack";
            collection.SpawnObject(other);
            collection.SpawnObject(animation);
            var originalGrid = collection.TargetTransform.GetComponentInParent<GridLane>();
            var animationGrid = Object.FindObjectsByType<GridLane>(FindObjectsSortMode.None)
                .Single(grid => grid.gameObject.name == "AnimateTrack lanes");
            var animationLanes = collection.Lanes.Where(lane => lane.Type == "AnimateTrack").ToArray();
            var animationIndex = System.Array.FindIndex(animationLanes, lane => lane.Label == "THING");
            Assert.That(animationGrid.gameObject.activeSelf, Is.True);
            Assert.That(animationGrid.Lane, Is.EqualTo(animationLanes.Length));
            var previousSection = collection.LaneSections[collection.LaneSections.Count - 2];
            var previousGrid = GetCategoryHeading(previousSection.Type).GetComponentInParent<GridLane>();
            var endOfOriginal = previousGrid.transform.TransformPoint(Vector3.right * previousGrid.Lane).x;
            Assert.That(
                animationGrid.transform.position.x - endOfOriginal,
                Is.EqualTo(BeatmapConstant.LaneSize).Within(0.0001f));
            var expectedX = animationGrid.transform.TransformPoint(new Vector3(animationIndex + 0.5f, 0.5f, 0)).x;
            Assert.That(
                collection.LoadedContainers[animation].transform.position.x,
                Is.EqualTo(expectedX).Within(0.0001f));
            var gapColumn = collection.GetLaneIndex(animation) - animationIndex - 1;
            Assert.That(collection.GetLaneAtColumn(gapColumn), Is.Null);
            var placement = Object.FindAnyObjectByType<CustomEventPlacement>();
            placement.QueuedData = Event("discarded");
            placement.QueuedData.JsonTime = 2;
            placement.PlacementVisualContainer.transform.localPosition = new Vector3(
                collection.GetLaneIndex(animation) + 0.5f,
                0.5f,
                0);
            placement.HandleApply();
            var placed = collection.MapObjects.Single(ev => ev.JsonTime == 2);
            Assert.That(placed.Type, Is.EqualTo("AnimateTrack"));
            Assert.That(placed.CustomTrack.Value, Is.EqualTo("THING"));
            Settings.ApplyOptionByName(nameof(Settings.SplitCustomEventsByTrack), false);
            Assert.That(animationGrid.gameObject.activeSelf, Is.False);
            Assert.That(originalGrid.Lane, Is.EqualTo(collection.CustomEventTypes.Count));
        }

        [Test]
        public void BoxSelectionUsesTheAnimationSectionsColumns()
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.Gameplay;
            var collection = Collection();
            var other = Event("other");
            var animation = Event("selected");
            animation.Type = "AnimateTrack";
            other.JsonTime = 1;
            animation.JsonTime = 1;
            collection.SpawnObject(other);
            collection.SpawnObject(animation);
            var grid = Object.FindObjectsByType<GridLane>(FindObjectsSortMode.None)
                .Single(item => item.gameObject.name == "AnimateTrack lanes");
            var box = Object.FindAnyObjectByType<BoxSelectionPlacement>();
            var originalState = box.State;
            var originalBounds = box.Bounds;
            var originalBoundsPosition = box.BoundsPosition;
            var originalBoxSelect = Settings.Instance.BoxSelect;
            try
            {
                Settings.Instance.BoxSelect = true;
                box.Initialize(grid.GetComponent<PlacementProvider>());
                box.BoundsPosition = grid.transform.localPosition;
                box.State = PlacementState.Active;
                var nodePosition = collection.LoadedContainers[animation].transform.position;
                var x = box.PlacementTrack.InverseTransformPoint(nodePosition).x;
                box.UpdateState(Hit(0.5f, x), PlacementInputState.Hover);
                box.HandleApply();
                box.UpdateState(Hit(1.5f, x), PlacementInputState.Hover);
                Assert.That(SelectionController.IsObjectSelected(animation), Is.True);
                Assert.That(SelectionController.IsObjectSelected(other), Is.False);
            }
            finally
            {
                box.Cancel();
                box.State = originalState;
                box.Bounds = originalBounds;
                box.BoundsPosition = originalBoundsPosition;
                Settings.Instance.BoxSelect = originalBoxSelect;
            }

            Intersections.IntersectionHit Hit(float beat, float x)
            {
                var localPoint = new Vector3(x, 0, beat * EditorScaleController.EditorScale);
                var point = box.PlacementTrack.TransformPoint(localPoint);
                return new Intersections.IntersectionHit(
                    grid.XY.gameObject,
                    new Bounds(Vector3.zero, Vector3.one),
                    new Ray(point, Vector3.forward),
                    0);
            }
        }

        [Test]
        public void CuttingAndPastingAtTheSameBeatAppendsInExportAndSurvivesUndoRedo()
        {
            var previousCopied = new HashSet<BaseObject>(SelectionController.CopiedObjects);
            try
            {
                Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.Gameplay;
                var parsed = V3Difficulty.GetFromJson(JSON.Parse(
                    "{\"version\":\"3.3.0\",\"customData\":{\"customEvents\":["
                    + "{\"b\":0,\"t\":\"LaneTest\",\"d\":{\"id\":1}},"
                    + "{\"b\":0,\"t\":\"LaneTest\",\"d\":{\"id\":2}},"
                    + "{\"b\":0,\"t\":\"LaneTest\",\"d\":{\"id\":3}}]}}"), "lane-order-test");
                var collection = Collection();
                foreach (var ev in parsed.CustomEvents)
                    collection.SpawnObject(ev);

                SelectionController.Select(parsed.CustomEvents[0]);
                var selection = Object.FindAnyObjectByType<SelectionController>();
                selection.Copy(true);
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0);
                selection.Paste();
                AssertExportOrder(2, 3, 1);
                PlaceUtils.Undo();
                PlaceUtils.Undo();
                AssertExportOrder(1, 2, 3);
                PlaceUtils.Redo();
                PlaceUtils.Redo();
                AssertExportOrder(2, 3, 1);
            }
            finally
            {
                SelectionController.CopiedObjects.Clear();
                SelectionController.CopiedObjects.UnionWith(previousCopied);
            }
        }

        private static void AssertExportOrder(params int[] expected)
        {
            var json = V3Difficulty.GetOutputJson(BeatSaberSongContainer.Instance.Map);
            Assert.That(
                json["customData"]["customEvents"].Children.Select(ev => ev["d"]["id"].AsInt),
                Is.EqualTo(expected));
        }

        [Test]
        public void DifferentTracksHaveDifferentGridPositions()
        {
            var collection = Collection();
            var first = Event("left");
            var second = Event("right");
            collection.SpawnObject(first);
            collection.SpawnObject(second);
            collection.RefreshPool(true);
            Assert.That(
                collection.LoadedContainers[first].transform.localPosition.x,
                Is.Not.EqualTo(collection.LoadedContainers[second].transform.localPosition.x),
                "Custom events targeting different tracks must occupy separate lanes by default.");
        }

        [Test]
        public void JsonTrackEditsCreateAndRemoveLanesThroughUndoRedo()
        {
            var collection = Collection();
            var first = Event("left");
            var second = Event("right");
            collection.SpawnObject(first);
            collection.SpawnObject(second);
            var originalLaneCount = TrackLaneCount(collection);
            NodeEditorController.IsActive = true;
            SelectionController.Select(first);
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            nodeEditor.ObjectWasSelected();
            var editedJson = first.ToJson().Clone();
            editedJson[first.KeyData][first.CustomKeyTrack] = "newTrack";
            nodeEditor.NodeEditor_EndEdit(editedJson.ToString());
            var edited = collection.MapObjects.Single(ev => ev.CustomTrack.Value == "newTrack");
            nodeEditor.Close();
            Assert.That(collection.Lanes.Any(lane => lane.Type == "LaneTest" && lane.Label == "left"), Is.False);
            Assert.That(collection.Lanes.Any(lane => lane.Type == "LaneTest" && lane.Label == "newTrack"), Is.True);
            Assert.That(TrackLaneCount(collection), Is.EqualTo(originalLaneCount));
            AssertPosition(collection, edited);
            AssertPosition(collection, second);
            PlaceUtils.Undo();
            Assert.That(collection.Lanes.Any(lane => lane.Type == "LaneTest" && lane.Label == "left"), Is.True);
            Assert.That(collection.Lanes.Any(lane => lane.Type == "LaneTest" && lane.Label == "newTrack"), Is.False);
            AssertPosition(collection, first);
            PlaceUtils.Redo();
            AssertPosition(collection, edited);
            collection.DeleteObject(edited);
            Assert.That(TrackLaneCount(collection), Is.EqualTo(originalLaneCount - 1));
            PlaceUtils.Undo();
            Assert.That(TrackLaneCount(collection), Is.EqualTo(originalLaneCount));
            AssertPosition(collection, edited);
        }

        [Test]
        public void MovingIntoExistingLaneKeepsItUntilItsLastEventIsRemoved()
        {
            var collection = Collection();
            var first = Event("left");
            var second = Event("right");
            collection.SpawnObject(first);
            collection.SpawnObject(second);
            var originalCount = TrackLaneCount(collection);
            var edited = (BaseCustomEvent)first.Clone();
            edited.Data = new JSONObject { ["track"] = "right" };
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedCollectionAction(
                    new List<BaseObject> { edited },
                    new List<BaseObject> { first }),
                true);
            Assert.That(collection.GetLaneIndex(edited), Is.EqualTo(collection.GetLaneIndex(second)));
            Assert.That(TrackLaneCount(collection), Is.EqualTo(originalCount - 1));
            collection.DeleteObject(second);
            Assert.That(TrackLaneCount(collection), Is.EqualTo(originalCount - 1));
            collection.DeleteObject(edited);
            Assert.That(TrackLaneCount(collection), Is.EqualTo(originalCount - 2));
        }

        [Test]
        public void PlacementInGeneratedLaneCopiesItsTrackSetIntoJson()
        {
            var collection = Collection();
            var source = Event(JSON.Parse("[\"right\",\"left\"]"));
            collection.SpawnObject(source);
            var placement = Object.FindAnyObjectByType<CustomEventPlacement>();
            placement.QueuedData = Event("discarded");
            placement.QueuedData.JsonTime = 1;
            placement.PlacementVisualContainer.transform.localPosition = new Vector3(
                collection.GetLaneIndex(source) + 0.5f,
                0.5f,
                0);
            placement.HandleApply();
            var placed = collection.MapObjects.Single(ev => ev.JsonTime == 1);
            Assert.That(placed.Data["track"].ToString(), Is.EqualTo("[\"left\",\"right\"]"));
            Assert.That(collection.GetLaneIndex(placed), Is.EqualTo(collection.GetLaneIndex(source)));
            PlaceUtils.Undo();
            Assert.That(collection.MapObjects.Contains(placed), Is.False);
            PlaceUtils.Redo();
            AssertPosition(collection, placed);
        }

        [Test]
        public void TogglingOffRestoresOriginalTypeLanesAndEmptyPlacementJson()
        {
            var collection = Collection();
            var first = Event("left");
            var second = Event("right");
            collection.SpawnObject(first);
            collection.SpawnObject(second);
            Settings.ApplyOptionByName(nameof(Settings.SplitCustomEventsByTrack), false);
            Assert.That(collection.Lanes.Count, Is.EqualTo(collection.CustomEventTypes.Count));
            Assert.That(collection.GetLaneIndex(first), Is.EqualTo(collection.CustomEventTypes.IndexOf(first.Type)));
            Assert.That(collection.GetLaneIndex(first), Is.EqualTo(collection.GetLaneIndex(second)));
            AssertPosition(collection, first);
            var placement = Object.FindAnyObjectByType<CustomEventPlacement>();
            placement.QueuedData = Event("discarded");
            placement.QueuedData.JsonTime = 1;
            placement.PlacementVisualContainer.transform.localPosition = new Vector3(
                collection.GetLaneIndex(first) + 0.5f,
                0.5f,
                0);
            placement.HandleApply();
            Assert.That(collection.MapObjects.Single(ev => ev.JsonTime == 1).Data.Count, Is.Zero);
            Settings.ApplyOptionByName(nameof(Settings.SplitCustomEventsByTrack), true);
            Assert.That(collection.GetLaneIndex(first), Is.Not.EqualTo(collection.GetLaneIndex(second)));
            AssertPosition(collection, second);
        }

        private static void AssertPosition(CustomEventGridContainer collection, BaseCustomEvent ev) =>
            Assert.That(
                collection.LoadedContainers[ev].transform.localPosition.x,
                Is.EqualTo(collection.GetLaneIndex(ev) + 0.5f));

        private static CustomEventGridContainer Collection() =>
            BeatmapObjectContainerCollection.GetCollectionForType<CustomEventGridContainer>(ObjectType.CustomEvent);

        private static int TrackLaneCount(CustomEventGridContainer collection) =>
            collection.Lanes.Count(lane => lane.Type == "LaneTest" && lane.DataKey != null);

        private static TextMeshProUGUI GetCategoryHeading(string type)
        {
            var headings = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)
                .Where(label => label.text == type).Where(label =>
                {
                    var grid = label.GetComponentInParent<GridLane>();
                    return grid != null && grid.transform.InverseTransformPoint(label.transform.position).x < 0;
                }).ToArray();
            Assert.That(headings.Length, Is.EqualTo(1), type + " needs a heading beside its lanes.");
            return headings[0];
        }

        private static BaseCustomEvent Event(JSONNode track) => new()
        {
            JsonTime = 0,
            Type = "LaneTest",
            Data = new JSONObject { ["track"] = track }
        };
    }
}
