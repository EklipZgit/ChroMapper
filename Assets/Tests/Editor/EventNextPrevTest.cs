using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class EventNextPrevTest : TestBase
    {
        [Test]
        public void Placement()
        {
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            PlaceEvent(1);
            PlaceEvent(4);
            PlaceEvent(2);
            PlaceEvent(3);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after deleting
            // 1 ->   -> 3 -> 4
            var e2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 2);
            PlaceUtils.Delete(e2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            PlaceEvent(1);
            PlaceEvent(4);
            PlaceEvent(2);
            PlaceEvent(3);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after deleting
            // 1 ->   -> 3 ->
            var e2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 2);
            var e4 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 4);
            SelectionController.Select(e2);
            SelectionController.Select(e4, true);
            selectionController.Delete();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event4);
        }

        [Test]
        public void ShiftingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // A1 -> T2 -> A3 -> T4
            // B1 ->    -> B3 ->
            PlaceLeftLasers(1);  // A1
            PlaceLeftLasers(3);  // A3
            PlaceRightLasers(1); // B1
            PlaceRightLasers(3); // B3
            PlaceLeftLasers(2);  // T2
            PlaceLeftLasers(4);  // T4

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);

            // Check state after shifting eventT
            // A1 ->    -> A3 ->
            // B1 -> T2 -> B3 -> T4
            var t2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2f && e.Type == (int)EventTypeValue.Event2);
            var t4 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 4f && e.Type == (int)EventTypeValue.Event2);
            SelectionController.Select(t2);
            SelectionController.Select(t4, true);
            selectionController.ShiftSelection(1, 0);

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);

            // Check state after undo and redo
            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);

            PlaceUtils.Redo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event3);
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            // Check state after placing
            // A -> T1 -> B -> T2
            PlaceLeftLasers(1);   // A
            PlaceLeftLasers(2);   // B
            PlaceLeftLasers(1.5f); // T1
            PlaceLeftLasers(2.5f); // T2
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after moving eventT
            // A ->   -> B -> T1 -> T2
            var t1 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 1.5f && e.Type == (int)EventTypeValue.Event2);
            var t2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2.5f && e.Type == (int)EventTypeValue.Event2);
            SelectionController.Select(t1);
            SelectionController.Select(t2, true);
            selectionController.MoveSelection(0.75f);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
        }

        // Reproduce the scene-only preview error by moving node 2, then playing through the unchanged grid timeline.
        [Test]
        public void MovingTransitionSourceSelectionUpdatesPreviewFadeSource()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var lightshowController = Object.FindAnyObjectByType<LightshowController>();
            const int eventType = (int)EventTypeValue.Event2;

            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 1f,
                Type = eventType,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f
            });
            var movedEvent = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = eventType,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f
            });
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 3f,
                Type = eventType,
                Value = (int)LightValue.WhiteTransition,
                FloatValue = 1f
            });

            // Keep playback before node 1 so UpdateTime(true, ...) follows the same forward-only cache path as the scene.
            atsc.MoveToJsonTime(0f);
            lightshowController.UpdateTime(false, atsc.CurrentSongBpmTime);
            SelectionController.Select(movedEvent);
            selectionController.MoveSelection(0.5f);

            var movedPreviewSource = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(movedPreviewSource.Prev, Is.Not.Null);
            Assert.That(movedPreviewSource.Next, Is.Not.Null);
            Assert.That(movedPreviewSource.Prev.IsTransition, Is.False);
            Assert.That(movedPreviewSource.Next.IsTransition, Is.True);

            // Node 1 must remain solid before moved node 2; the grid already has no transition ribbon in this interval.
            lightshowController.UpdateTime(true, ToSongBpmTime(1f));
            lightshowController.UpdateTime(true, ToSongBpmTime(2f));
            var expected = context.ColorScheme.GetColorFrom(LightColor.Red, false);
            var rendered = Object.FindObjectsByType<LightIntensityController>(FindObjectsSortMode.None)
                .First(controller => controller.Type == eventType)
                .Color;
            Assert.That(rendered, Is.EqualTo(expected));

            // Once node 2 becomes active, the reported preview repairs itself and resumes its node-2-to-node-3 fade.
            lightshowController.UpdateTime(true, ToSongBpmTime(2.5f));
            lightshowController.UpdateTime(true, ToSongBpmTime(2.75f));

            var expectedAfterNode2 = Color.LerpUnclamped(
                context.ColorScheme.GetColorFrom(LightColor.Blue, false),
                context.ColorScheme.GetColorFrom(LightColor.White, false),
                0.5f);
            var renderedAfterNode2 = Object.FindObjectsByType<LightIntensityController>(FindObjectsSortMode.None)
                .First(controller => controller.Type == eventType)
                .Color;
            Assert.That(renderedAfterNode2, Is.EqualTo(expectedAfterNode2));
        }

        [Test]
        public void CopyPasteSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Check state after placing
            // A -> B
            PlaceLeftLasers(1);
            PlaceLeftLasers(2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after pasting
            // A -> B -> A Copy -> B copy
            var a = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 1f && e.Type == (int)EventTypeValue.Event2);
            var b = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2f && e.Type == (int)EventTypeValue.Event2);
            SelectionController.Select(a);
            SelectionController.Select(b, true);
            atsc.MoveToJsonTime(3);
            if (eventPlacement.QueuedData != null) eventPlacement.QueuedData.JsonTime = 3;
            selectionController.Copy();
            selectionController.Paste();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
        }

        // Keep basic-event neighbor state correct when a collection edit moves several events across a populated lane.
        [Test]
        public void MovingSelectionAcrossExistingEventsKeepsNeighborsLinked()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer = GetEventsContainer();

            PlaceLeftLasers(1);
            PlaceLeftLasers(2);
            var movedA = PlaceLeftLasers(4);
            var movedB = PlaceLeftLasers(5);
            PlaceLeftLasers(7);

            SelectionController.Select(movedA);
            SelectionController.Select(movedB, true);
            selectionController.MoveSelection(-1.5f);

            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.Event2);
        }

        // Preserve a shared name-filter lane until the final matching ring event is removed, then restore it on undo.
        [Test]
        public void RingNameFilterLanesTrackDuplicateEventsAcrossDeleteAndUndo()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ringType = GetRingRotationType();
            var first = PlaceRingRotation(1, ringType, "drums");
            var second = PlaceRingRotation(2, ringType, "drums");

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            Assert.AreNotEqual(labels.EventTypeToLaneId(first.Type), labels.EventToLaneId(first));

            PlaceUtils.Delete(first);
            Assert.AreNotEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));

            PlaceUtils.Delete(second);
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));

            PlaceUtils.Undo();
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreNotEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));
        }

        // Keep filter lanes distinct and alphabetical while duplicate events share a single lane.
        [Test]
        public void RingNameFilterLanesAreDistinctAndAlphabetical()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ringType = GetRingRotationType();
            var zebra = PlaceRingRotation(1, ringType, "zebra");
            var alpha = PlaceRingRotation(2, ringType, "alpha");
            PlaceRingRotation(3, ringType, "zebra");

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            var baseLane = labels.EventTypeToLaneId(ringType);
            var alphaLane = labels.EventToLaneId(alpha);
            var zebraLane = labels.EventToLaneId(zebra);
            Assert.Greater(alphaLane, baseLane);
            Assert.Greater(zebraLane, alphaLane);
        }

        // Prevent name filters on ordinary light tracks from creating ring-only virtual lanes.
        [Test]
        public void LightNameFiltersDoNotCreateVirtualLanes()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var light = PlaceLeftLasers(1);
            light.CustomNameFilter = "ignored";
            light.WriteCustom();

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            Assert.AreEqual(labels.EventTypeToLaneId(light.Type), labels.EventToLaneId(light));
        }

        // Apply collection replacements so filter counts follow final names and types without a map-wide scan.
        [Test]
        public void RingNameFilterLanesReflectCollectionReplacements()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ringType = GetRingRotationType();
            var first = PlaceRingRotation(1, ringType, "drums");
            var second = PlaceRingRotation(2, ringType, "synth");

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreNotEqual(labels.EventToLaneId(first), labels.EventToLaneId(second));

            second = ReplaceEvent(second, evt => evt.CustomNameFilter = "drums");
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventToLaneId(first), labels.EventToLaneId(second));

            first = ReplaceEvent(first, evt => evt.CustomNameFilter = null);
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventTypeToLaneId(first.Type), labels.EventToLaneId(first));
            Assert.AreNotEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));

            second = ReplaceEvent(second, evt => evt.Type = (int)EventTypeValue.Event2);
            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);
            Assert.AreEqual(labels.EventTypeToLaneId(second.Type), labels.EventToLaneId(second));
        }

        // Ignore empty filter values so they cannot create a blank virtual lane.
        [Test]
        public void EmptyRingNameFiltersDoNotCreateVirtualLanes()
        {
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var ring = PlaceRingRotation(1, GetRingRotationType(), string.Empty);

            labels.UpdateLabels(EventGridContainer.PropMode.Off, 0, 0);

            Assert.AreEqual(labels.EventTypeToLaneId(ring.Type), labels.EventToLaneId(ring));
        }

        private static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

        private static float ToSongBpmTime(float jsonTime) =>
            (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);

        // Read the active definition so the name-filter tests remain valid for every test environment.
        private static int GetRingRotationType()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            return context.TracksDefinition.Basic.Values
                .First(definition => definition.Components.HasFlag(BasicEventComponent.RingRotation)).Type;
        }

        // Exercise the same final-object replacement action used by selection moves and mirrors.
        private static BaseEvent ReplaceEvent(BaseEvent original, System.Action<BaseEvent> edit)
        {
            var edited = BeatmapFactory.Clone(original);
            edit(edited);
            edited.WriteCustom();
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedCollectionAction(
                    new List<BaseObject> { edited },
                    new List<BaseObject> { original },
                    "Replace basic event filter."),
                true);
            return edited;
        }

        private void AssertLinksAndSorted(EventGridContainer eventsContainer, int eventType)
        {
            var laneEvents = eventsContainer.MapObjects.Where(x => x.Type == eventType).ToList();
            BeatmapAssertion.IsEqual(
                BeatmapAssertion.EventsAreLinkedAndSorted,
                laneEvents,
                "Events are linked and sorted");
        }

        private void PlaceEvent(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.Event4,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }

        private BaseEvent PlaceLeftLasers(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.BlueOn
            };
            return PlaceUtils.Place(evt);
        }

        // Create a ring-rotation event because only ring-rotation tracks expose name-filter lanes.
        private static BaseEvent PlaceRingRotation(float time, int eventType, string nameFilter)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = eventType,
                Value = 0,
                CustomNameFilter = nameFilter
            };
            return PlaceUtils.Place(evt);
        }

        private void PlaceRightLasers(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.Event3,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }
    }
}
