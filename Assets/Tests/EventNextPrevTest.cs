using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class EventNextPrevTest : TestBase
    {
        [Test]
        public void Placement()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var event1 = new BaseEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };
            var event2 = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };
            var event3 = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };
            var event4 = new BaseEvent
            {
                JsonTime = 4, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            event1 = PlaceUtils.Place(event1);
            event4 = PlaceUtils.Place(event4);
            event2 = PlaceUtils.Place(event2);
            event3 = PlaceUtils.Place(event3);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 -> 4
            PlaceUtils.Delete(event2);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            PlaceUtils.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var event1 = new BaseEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };
            var event2 = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };
            var event3 = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };
            var event4 = new BaseEvent
            {
                JsonTime = 4, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn
            };

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            event1 = PlaceUtils.Place(event1);
            event4 = PlaceUtils.Place(event4);
            event2 = PlaceUtils.Place(event2);
            event3 = PlaceUtils.Place(event3);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 ->
            SelectionController.Select(event2);
            SelectionController.Select(event4, true);
            selectionController.Delete();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            PlaceUtils.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
        }

        [Test]
        public void ShiftingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventA1 = new BaseEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventT2 = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventA3 = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventT4 = new BaseEvent
            {
                JsonTime = 4, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventB1 = new BaseEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.RightLasers, Value = (int)LightValue.BlueOn
            };
            var eventB3 = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.RightLasers, Value = (int)LightValue.BlueOn
            };

            // Check state after placing
            // A1 -> T2 -> A3 -> T4
            // B1 ->    -> B3 ->
            eventA1 = PlaceUtils.Place(eventA1);
            eventA3 = PlaceUtils.Place(eventA3);
            eventB1 = PlaceUtils.Place(eventB1);
            eventB3 = PlaceUtils.Place(eventB3);
            eventT2 = PlaceUtils.Place(eventT2);
            eventT4 = PlaceUtils.Place(eventT4);

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after shifting eventT
            // A1 ->    -> A3 ->
            // B1 -> T2 -> B3 -> T4
            SelectionController.Select(eventT2);
            SelectionController.Select(eventT4, true);
            selectionController.ShiftSelection(1, 0);

            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after undo and redo
            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            PlaceUtils.Redo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventA = new BaseEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventT1 = new BaseEvent
            {
                JsonTime = 1.5f, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventB = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventT2 = new BaseEvent
            {
                JsonTime = 2.5f, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };

            // Check state after placing
            // A -> T1 -> B -> T2
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            eventT1 = PlaceUtils.Place(eventT1);
            eventT2 = PlaceUtils.Place(eventT2);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after moving eventT
            // A ->   -> B -> T1 -> T2
            SelectionController.Select(eventT1);
            SelectionController.Select(eventT2, true);
            selectionController.MoveSelection(0.75f);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            PlaceUtils.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var eventA = new BaseEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };
            var eventB = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn
            };

            // Check state after placing
            // A -> B
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after pasting
            // A -> B -> A Copy -> B copy
            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);
            atsc.MoveToJsonTime(3);
            if (eventPlacement.QueuedData != null) eventPlacement.QueuedData.JsonTime = 3;
            selectionController.Copy();
            selectionController.Paste();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            PlaceUtils.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        private void AssertMapObjectsAreLinkedAndSorted(EventGridContainer eventsContainer, int eventType)
        {
            var laneEvents = eventsContainer.MapObjects.Where(x => x.Type == eventType).ToList();
            BeatmapAssertion.IsEqual(
                BeatmapAssertion.EventsAreLinkedAndSorted,
                laneEvents,
                "Events are linked and sorted");
        }
    }
}