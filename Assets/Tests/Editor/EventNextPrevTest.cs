using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
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
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 -> 4
            var e2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 2);
            PlaceUtils.Delete(e2);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
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
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 ->
            var e2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 2);
            var e4 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => (int)e.JsonTime == 4);
            SelectionController.Select(e2);
            SelectionController.Select(e4, true);
            selectionController.Delete();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
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
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after shifting eventT
            // A1 ->    -> A3 ->
            // B1 -> T2 -> B3 -> T4
            var t2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2f && e.Type == (int)EventTypeValue.LeftLasers);
            var t4 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 4f && e.Type == (int)EventTypeValue.LeftLasers);
            SelectionController.Select(t2);
            SelectionController.Select(t4, true);
            selectionController.ShiftSelection(1, 0);

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after undo and redo
            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            PlaceUtils.Redo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);
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
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after moving eventT
            // A ->   -> B -> T1 -> T2
            var t1 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 1.5f && e.Type == (int)EventTypeValue.LeftLasers);
            var t2 = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2.5f && e.Type == (int)EventTypeValue.LeftLasers);
            SelectionController.Select(t1);
            SelectionController.Select(t2, true);
            selectionController.MoveSelection(0.75f);
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
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
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after pasting
            // A -> B -> A Copy -> B copy
            var a = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 1f && e.Type == (int)EventTypeValue.LeftLasers);
            var b = eventsContainer.MapObjects.OfType<BaseEvent>().First(e => e.JsonTime == 2f && e.Type == (int)EventTypeValue.LeftLasers);
            SelectionController.Select(a);
            SelectionController.Select(b, true);
            atsc.MoveToJsonTime(3);
            if (eventPlacement.QueuedData != null) eventPlacement.QueuedData.JsonTime = 3;
            selectionController.Copy();
            selectionController.Paste();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            PlaceUtils.Redo();
            AssertLinksAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        private static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

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
                Type = (int)EventTypeValue.CenterLights,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }

        private void PlaceLeftLasers(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.LeftLasers,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }

        private void PlaceRightLasers(float time)
        {
            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.RightLasers,
                Value = (int)LightValue.BlueOn
            };
            PlaceUtils.Place(evt);
        }
    }
}
