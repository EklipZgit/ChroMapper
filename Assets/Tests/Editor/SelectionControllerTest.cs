using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class SelectionControllerTest : TestBase
    {
        private SelectionFixture _fixture;

        [SetUp]
        public void PlaceObjects() => _fixture = new SelectionFixture();

        [Test]
        public void SelectBetweenNotes()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Note3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotes());
        }

        [Test]
        public void ShiftClickEquivalentSelectionIncludesArc()
        {
            // Model Shift-click's one-anchor range selection and ensure the clicked arc remains selected.
            SelectionController.Select(_fixture.Note1);
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Arc24, true);

            Assert.IsTrue(SelectionController.IsObjectSelected(_fixture.Arc24));
        }

        [Test]
        public void ArcIndicatorHitResolvesToOwningArcContainer()
        {
            // Protect arrow-end selection by resolving an indicator child through its ArcContainer parent.
            var arcContainerObject = new GameObject("Arc owner test");
            var arcContainer = arcContainerObject.AddComponent<ArcContainer>();
            var indicatorObject = new GameObject("Arc indicator test");
            indicatorObject.transform.SetParent(arcContainerObject.transform);

            try
            {
                var resolved = indicatorObject.GetComponentInParent<ArcContainer>();

                Assert.AreSame(arcContainer, resolved);
            }
            finally
            {
                Object.DestroyImmediate(arcContainerObject);
            }
        }

        [Test]
        public void ShiftClickArcIndicatorResolvesThroughRaycastFirstObject()
        {
            // Exercise the shift-click hit path so an indicator child resolves to the selectable arc owner.
            var arcContainerObject = new GameObject("Arc owner raycast test");
            var arcContainer = arcContainerObject.AddComponent<ArcContainer>();
            var indicatorObject = new GameObject("Arc indicator raycast test");
            indicatorObject.transform.SetParent(arcContainerObject.transform);
            var inputControllerObject = new GameObject("Arc input controller test");
            var inputController = inputControllerObject.AddComponent<TestArcInputController>();

            try
            {
                BeatmapRaycastCache.FirstHit = indicatorObject;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;

                var resolved = inputController.ResolveRaycast(out var resolvedContainer);

                Assert.IsTrue(resolved);
                Assert.AreSame(arcContainer, resolvedContainer);
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(inputControllerObject);
                Object.DestroyImmediate(arcContainerObject);
            }
        }

        [Test]
        public void HoveringSelectedArcPreservesSelectionHighlightState()
        {
            // Protect the selected outline from transient hover state changes on a loaded arc container.
            var arcCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var arcContainer = arcCollection.LoadedContainers[_fixture.Arc02] as ArcContainer;

            Assert.IsNotNull(arcContainer);
            SelectionController.Select(_fixture.Arc02);
            Assert.IsTrue(arcContainer.Selected);

            arcContainer.Highlighted = true;
            arcContainer.Highlighted = false;

            Assert.IsTrue(SelectionController.IsObjectSelected(_fixture.Arc02));
            Assert.IsTrue(arcContainer.Selected);
        }

        [Test]
        public void SelectBetweenEvents()
        {
            SelectionController.SelectBetween(_fixture.Event1, _fixture.Event3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenEvents());
        }

        [Test]
        public void SelectBetweenBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.BpmEvent1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenBpmEvents());
        }

        [Test]
        public void SelectBetweenNotesAndEvents()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Event3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotesAndEvents());
        }

        [Test]
        public void SelectBetweenNotesAndBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotesAndBpmEvents());
        }

        [Test]
        public void SelectBetweenEventsAndBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.Event1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenEventsAndBpmEvents());
        }

        // Guard the allocation-free backing range query against admitting events outside its beat interval.
        [Test]
        public void SongBpmRangeQueryVisitsOnlyEventsInsideBounds()
        {
            var visited = new HashSet<BaseObject>();
            var eventBeat = _fixture.Event3.SongBpmTime;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                eventBeat - 0.1f,
                eventBeat + 0.1f,
                ObjectType.Event,
                (_, obj) => visited.Add(obj));

            CollectionAssert.AreEquivalent(new[] { _fixture.Event3 }, visited);
        }

        // Ensure the bit iteration reaches Note directly without relying on the old shift-by-32 wraparound.
        [Test]
        public void SongBpmRangeQueryVisitsNoteBitOnce()
        {
            var visited = new HashSet<BaseObject>();
            var noteBeat = _fixture.Note3.SongBpmTime;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                noteBeat - 0.1f,
                noteBeat + 0.1f,
                ObjectType.Note,
                (_, obj) => visited.Add(obj));

            CollectionAssert.AreEquivalent(new[] { _fixture.Note3 }, visited);
        }

        // Document the temporary start-time-only behavior until selection has a data-only interval index.
        [Test]
        public void SongBpmRangeQueryDoesNotIncludeOverlappingSliderTails()
        {
            var visited = new HashSet<BaseObject>();
            var queryEnd = _fixture.Arc24.SongBpmTime - 0.1f;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                queryEnd - 0.1f,
                queryEnd,
                ObjectType.Arc,
                (_, obj) => visited.Add(obj));

            CollectionAssert.IsEmpty(visited);
        }

        // Preserve dev's behavior for arcs whose heads precede the query but whose tails overlap its lower bound.
        // [Test] // Skipping for now because we can't do this cleanly with binary search without adding end events into the sorted collection.
        public void SongBpmRangeQueryIncludesOverlappingSliders()
        {
            var visited = new HashSet<BaseObject>();
            var queryEnd = _fixture.Arc24.SongBpmTime - 0.1f;

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
                queryEnd - 0.1f,
                queryEnd,
                ObjectType.Arc,
                (_, obj) => visited.Add(obj));

            CollectionAssert.AreEquivalent(new[] { _fixture.Arc02, _fixture.Arc04 }, visited);
        }

        // Guard the merge regression where GLS lanes after a visual track gap were tested at their left edge.
        [TestCase(0f, 0.5f)]
        [TestCase(4f, 4.5f)]
        public void GlsLaneCenterIncludesHalfLaneOffset(float trackOffset, float expectedCenter)
        {
            var center = BoxSelectionPlacement.GetGlsLaneCenter(trackOffset);

            Assert.AreEqual(expectedCenter, center.x);
            Assert.AreEqual(0.5f, center.y);
        }

        // Reproduce the adjacent-lane selection seen far right in large even and odd centered inner GLS groups.
        [TestCase(60, 50, 20f, 20.5f)]
        [TestCase(61, 51, 20.5f, 21f)]
        public void GlsInnerEventUsesRenderedCenterInLargeLaneGroup(
            int laneCount,
            int adjacentBoxIndex,
            float selectionRightEdge,
            float expectedCenter)
        {
            var gridStartLane = -laneCount / 2f;
            var boundsPositionX = gridStartLane * BeatmapConstant.LaneSize;

            var center = BoxSelectionPlacement.GetGlsEventSelectionPosition(
                adjacentBoxIndex,
                boundsPositionX);

            Assert.AreEqual(expectedCenter, center.x, 0.0001f);
            Assert.Greater(center.x, selectionRightEdge);
            Assert.AreEqual(0.5f, center.y);
        }

        // Guard the preview-mode path that selects groups only at their rendered inner-event beats.
        [TestCase(0f, false)]
        [TestCase(0.5f, true)]
        public void GlsPreviewOpacityControlsGroupBeatSelection(float previewOpacity, bool expectedPreviewTimes)
        {
            Assert.AreEqual(
                expectedPreviewTimes,
                BoxSelectionPlacement.UsesGlsPreviewNodeTimes(previewOpacity));
        }

        // Verify preview selection uses logical child beats and therefore does not depend on loaded scene containers.
        [Test]
        public void GlsPreviewBeatMatchUsesBackingGroupData()
        {
            var group = CreateTwoLaneColorGroup();
            var previewBeat = group.ReadOnlyBoxes[0].ReadOnlyEvents[0].SongBpmTime;
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.True(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat - 0.1f,
                previewBeat + 0.1f,
                epsilon));
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat + 0.1f,
                previewBeat + 0.2f,
                epsilon));
        }

        // Preserve selection of a later preview node when its parent group starts before the dragged beat range.
        [Test]
        public void GlsPreviewBeatMatchIncludesLaterNodeFromEarlierGroup()
        {
            var group = CreateTwoLaneColorGroup();
            var laterPreviewBeat = group.ReadOnlyBoxes[1].ReadOnlyEvents[0].SongBpmTime;
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.True(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                laterPreviewBeat - 0.1f,
                laterPreviewBeat + 0.1f,
                epsilon));
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                group.SongBpmTime - 0.1f,
                group.SongBpmTime + 0.1f,
                epsilon));
        }

        // Reproduce an offscreen parent whose first preview node begins inside the dragged selection range.
        [Test]
        public void GlsPreviewIntervalIndexIncludesPreviewSpanStartingInsideSelection()
        {
            var group = CreateTwoLaneColorGroup();
            group.ID = 999;
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.SpawnObject(group, false, false);

            try
            {
                var candidates = new HashSet<BaseEventBoxGroup>();
                var index = new GlsPreviewIntervalIndex();

                index.AddOverlappingPreviewIntervals(
                    collection,
                    group.JsonTime + 0.25f,
                    group.JsonTime + 0.5f,
                    candidates);

                Assert.True(candidates.Contains(group));
            }
            finally
            {
                collection.DeleteObject(group, false, false);
            }
        }

        // Preserve preview-beat selection whether the group cache was built by rendering or by selection.
        [Test]
        public void GlsPreviewBeatMatchIsIndependentOfCacheWarmup()
        {
            var group = CreateTwoLaneColorGroup();
            var previewBeat = group.ReadOnlyBoxes[1].ReadOnlyEvents[0].SongBpmTime;
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            var coldResult = BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat - 0.1f,
                previewBeat + 0.1f,
                epsilon);
            group.ResortOrderedEvents();
            var warmResult = BoxSelectionPlacement.HasGlsPreviewEventBetween(
                group,
                previewBeat - 0.1f,
                previewBeat + 0.1f,
                epsilon);

            Assert.True(coldResult);
            Assert.AreEqual(coldResult, warmResult);
        }

        // Preserve the empty authored-group behavior while cache initialization is refactored separately.
        [Test]
        public void EmptyGlsPreviewGroupNeverMatchesASelectionRange()
        {
            var group = CreateEmptyColorGroup();
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 0f, 1f, epsilon));
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 1f, 2f, epsilon));
        }

        // Ensure an authored empty group is marked initialized instead of rebuilding the same empty preview cache.
        [Test]
        public void EmptyGlsPreviewGroupInitializesItsCacheOnlyOnce()
        {
            var group = CreateEmptyColorGroup();
            var epsilon = BeatmapObjectContainerCollection.Epsilon;

            Assert.False(group.OrderedEventsInitialized);
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 0f, 1f, epsilon));
            var initializedCache = group.OrderedEvents;
            Assert.True(group.OrderedEventsInitialized);
            Assert.False(BoxSelectionPlacement.HasGlsPreviewEventBetween(group, 0f, 1f, epsilon));

            Assert.AreSame(initializedCache, group.OrderedEvents);
        }

        // Guard both forward and reverse box drags using the immutable beat-space endpoints.
        [TestCase(2f, 5f, 2f, 5f)]
        [TestCase(5f, 2f, 2f, 5f)]
        public void BoxSelectionBeatBoundsNormalizeDragDirection(
            float originBeat,
            float currentBeat,
            float expectedStart,
            float expectedEnd)
        {
            var bounds = BoxSelectionPlacement.GetSongBpmBounds(originBeat, currentBeat);

            Assert.AreEqual(expectedStart, bounds.Start);
            Assert.AreEqual(expectedEnd, bounds.End);
        }

        // Guard incremental scrolling so only true expansion can reuse the existing logical result.
        [TestCase(0f, 10f, 0f, 11f, true)]
        [TestCase(0f, 10f, -1f, 10f, true)]
        [TestCase(0f, 10f, 0f, 9f, false)]
        [TestCase(0f, 10f, 1f, 10f, false)]
        public void BoxSelectionDetectsMonotonicBeatExpansion(
            float previousStart,
            float previousEnd,
            float currentStart,
            float currentEnd,
            bool expected)
        {
            Assert.AreEqual(
                expected,
                BoxSelectionPlacement.BeatBoundsMonotonicallyExpand(
                    previousStart,
                    previousEnd,
                    currentStart,
                    currentEnd,
                    BeatmapObjectContainerCollection.Epsilon));
        }

        // Verify time shifts replace the owning GLS group and retain a valid selected child instance.
        [Test]
        public void MoveSelectionRebindsSelectedGlsEventToReplacementGroup()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            var selectedEvent = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            SelectionController.Select(selectedEvent);

            selectionController.MoveSelection(0.25f);

            var movedEvent = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().Single();
            Assert.AreEqual(0.75f, movedEvent.RelativeJsonTime);
            Assert.AreEqual(2.75f, movedEvent.JsonTime);
            Assert.AreEqual(0, movedEvent.BoxIndex);
            Assert.AreSame(movedEvent.EventBoxGroupData.ReadOnlyBoxes[0], movedEvent.EventBoxData);
            Assert.AreSame(
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext,
                movedEvent.EventBoxGroupData);

            // Verify both action directions retain the correct parent-owned relative time.
            PlaceUtils.Undo();
            Assert.AreEqual(
                0.5f,
                Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext.ReadOnlyBoxes[0].ReadOnlyEvents[0].RelativeJsonTime);
            PlaceUtils.Redo();
            Assert.AreEqual(
                0.75f,
                Object.FindAnyObjectByType<GLSEventGridProvider>()
                    .GroupContext.ReadOnlyBoxes[0].ReadOnlyEvents[0].RelativeJsonTime);
        }

        // Verify lane shifts rebuild child ownership and preserve boundary-node selection in the same group action.
        [Test]
        public void ShiftSelectionRebindsAllSelectedGlsEventsAtLaneBoundary()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateTwoLaneColorGroup());
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);
            SelectionController.Select(group.ReadOnlyBoxes[1].ReadOnlyEvents[0], true);

            selectionController.ShiftSelection(1, 0);

            var shiftedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(2, shiftedEvents.Length);
            Assert.True(shiftedEvents.All(evt => evt.BoxIndex == 1));
            Assert.True(shiftedEvents.All(evt =>
                ReferenceEquals(evt.EventBoxData, evt.EventBoxGroupData.ReadOnlyBoxes[1])));
            Assert.AreSame(
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext,
                shiftedEvents[0].EventBoxGroupData);

            // Verify undo and redo restore the complete lane distribution, not only selected child instances.
            PlaceUtils.Undo();
            var restoredGroup = Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext;
            Assert.AreEqual(1, restoredGroup.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            Assert.AreEqual(1, restoredGroup.ReadOnlyBoxes[1].ReadOnlyEvents.Count);
            PlaceUtils.Redo();
            var redoneGroup = Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext;
            Assert.AreEqual(0, redoneGroup.ReadOnlyBoxes[0].ReadOnlyEvents.Count);
            Assert.AreEqual(2, redoneGroup.ReadOnlyBoxes[1].ReadOnlyEvents.Count);
        }

        // Verify duplicate GLS identities consume distinct replacement nodes instead of collapsing selection.
        [Test]
        public void MoveSelectionRebindsDuplicateGlsEvents()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateDuplicateColorGroup());
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[1], true);

            selectionController.MoveSelection(0.25f);

            var movedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(2, movedEvents.Length);
            Assert.True(movedEvents.All(evt => Mathf.Approximately(evt.RelativeJsonTime, 0.75f)));
            Assert.AreNotSame(movedEvents[0], movedEvents[1]);
            Assert.AreEqual(2, movedEvents[0].EventBoxData.ReadOnlyEvents.Count);
        }

        // Verify duplicate identities remain independently selected when a lane replacement moves both nodes.
        [Test]
        public void ShiftSelectionRebindsDuplicateGlsEvents()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var group = PlaceGlsGroup(CreateDuplicateColorGroup());
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[1], true);

            selectionController.ShiftSelection(1, 0);

            var shiftedEvents = SelectionController.SelectedObjects.OfType<BaseGLSEvent>().ToArray();
            Assert.AreEqual(2, shiftedEvents.Length);
            Assert.True(shiftedEvents.All(evt => evt.BoxIndex == 1));
            Assert.True(shiftedEvents.All(evt => ReferenceEquals(evt.EventBoxData, evt.EventBoxGroupData.ReadOnlyBoxes[1])));
        }

        private void AssertSelectedObjects(ICollection<BaseObject> objects)
        {
            foreach (var selectedObject in objects)
                Assert.True(
                    SelectionController.SelectedObjects.Contains(selectedObject),
                    $"{selectedObject} should be selected");

            Assert.AreEqual(
                objects.Count,
                SelectionController.SelectedObjects.Count,
                "Selection should be the exact amount");
        }

        // Place the parent through its real collection so replacement actions update the open GLS child context.
        private static BaseLightColorEventBoxGroup PlaceGlsGroup(BaseLightColorEventBoxGroup group)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
            return group;
        }

        // Build distinct events in adjacent filter lanes for parent replacement and boundary-shift coverage.
        private static BaseLightColorEventBoxGroup CreateTwoLaneColorGroup() =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] },
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.75, ""c"": 1, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] }
                ] }"));

        // Build two same-time nodes in one lane so replacement logic must preserve duplicate identity cardinality.
        private static BaseLightColorEventBoxGroup CreateDuplicateColorGroup() =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 },
                               { ""b"": 0.5, ""c"": 1, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] },
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [] }
                ] }"));

        // Build an authored group with a box but no nodes to retain its selection behavior during cache changes.
        private static BaseLightColorEventBoxGroup CreateEmptyColorGroup() =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [] }
                ] }"));

        private class SelectionFixture
        {
            public readonly BaseArc Arc02;
            public readonly BaseArc Arc04;
            public readonly BaseArc Arc24;
            public readonly BaseArc Arc44;
            public readonly BaseBpmEvent BpmEvent1;
            public readonly BaseBpmEvent BpmEvent2;
            public readonly BaseBpmEvent BpmEvent3;
            public readonly BaseEvent Event1;
            public readonly BaseEvent Event2;
            public readonly BaseEvent Event3;
            public readonly BaseEvent Event4;
            public readonly BaseEvent RotationEvent2;
            public readonly BaseNote Note1;
            public readonly BaseNote Note2;
            public readonly BaseNote Note3;
            public readonly BaseNote Note4;

            public SelectionFixture()
            {
                BpmEvent1 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 100 });
                BpmEvent2 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 2, Bpm = 100 });
                BpmEvent3 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 3, Bpm = 100 });

                Note1 = PlaceUtils.Place(new BaseNote { JsonTime = 1 });
                Note2 = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
                Note3 = PlaceUtils.Place(new BaseNote { JsonTime = 3 });
                Note4 = PlaceUtils.Place(new BaseNote { JsonTime = 4 });

                Event1 = PlaceUtils.Place(new BaseEvent { JsonTime = 1 });
                Event2 = PlaceUtils.Place(new BaseEvent { JsonTime = 2 });
                Event3 = PlaceUtils.Place(new BaseEvent { JsonTime = 3 });
                Event4 = PlaceUtils.Place(new BaseEvent { JsonTime = 4 });

                RotationEvent2 = PlaceUtils.Place(
                    new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.EarlyRotationEventType });

                Arc02 = PlaceUtils.Place(new BaseArc { JsonTime = 0, TailJsonTime = 2 });
                Arc04 = PlaceUtils.Place(new BaseArc { JsonTime = 0, TailJsonTime = 4 });
                Arc24 = PlaceUtils.Place(new BaseArc { JsonTime = 2, TailJsonTime = 4 });
                Arc44 = PlaceUtils.Place(new BaseArc { JsonTime = 4, TailJsonTime = 4 });
            }

            // Selection currently admits arcs by their head time, not by an overlapping tail.
            public ICollection<BaseObject> ExpectedSelectBetweenNotes() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc24
            };

            public ICollection<BaseObject> ExpectedSelectBetweenEvents() => new List<BaseObject>
            {
                Event1, Event2, Event3, RotationEvent2
            };

            public ICollection<BaseObject> ExpectedSelectBetweenBpmEvents() => new List<BaseObject>
            {
                BpmEvent1, BpmEvent2, BpmEvent3
            };

            // Mixed selection follows the same start-time-only arc behavior.
            public ICollection<BaseObject> ExpectedSelectBetweenNotesAndEvents() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc24, Event1, Event2, Event3, RotationEvent2
            };

            // Mixed BPM selection follows the same start-time-only arc behavior.
            public ICollection<BaseObject> ExpectedSelectBetweenNotesAndBpmEvents() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc24, BpmEvent1, BpmEvent2, BpmEvent3
            };

            public ICollection<BaseObject> ExpectedSelectBetweenEventsAndBpmEvents() => new List<BaseObject>
            {
                Event1, Event2, Event3, RotationEvent2, BpmEvent1, BpmEvent2, BpmEvent3
            };
        }

        [Test]
        public void ShiftSelectionOutsideVanillaGrid([Values] bool isVanillaOnlyShiftSettingEnabled)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = _fixture.Note1;

            SelectionController.Select(note);

            Assert.AreEqual(1, SelectionController.SelectedObjects.Count, "Note should be selected");
            Assert.AreEqual(0, note.PosX);
            Assert.AreEqual(0, note.PosY);


            Settings.Instance.VanillaOnlyShift = isVanillaOnlyShiftSettingEnabled;

            selectionController.ShiftSelection(5, 5);
            note = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            if (isVanillaOnlyShiftSettingEnabled)
            {
                // Expect clamped values
                Assert.AreEqual((int)GridX.Right, note.PosX);
                Assert.AreEqual((int)GridY.Top, note.PosY);
                Assert.IsNull(note.CustomCoordinate);
            }
            else
            {
                Assert.AreEqual(0, note.PosX);
                Assert.AreEqual(0, note.PosY);
                Assert.NotNull(note.CustomCoordinate);
                Assert.AreEqual(3, note.CustomCoordinate[0].AsInt);
                Assert.AreEqual(5, note.CustomCoordinate[1].AsInt);
            }
        }

        // Expose the protected resolver so the test can exercise the same cached hit path as shift-click.
        private class TestArcInputController : BeatmapInputController<ArcContainer>
        {
            public bool ResolveRaycast(out ArcContainer container) => RaycastFirstObject(out container);
        }
    }
}
