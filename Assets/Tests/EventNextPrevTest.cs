using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EventNextPrevTest : TestBase
    {
        [Test]
        public void Placement()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEvent1 = new BaseEvent{ JsonTime = 1, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };
            var baseEvent2 = new BaseEvent{ JsonTime = 2, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };
            var baseEvent3 = new BaseEvent{ JsonTime = 3, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };
            var baseEvent4 = new BaseEvent{ JsonTime = 4, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            baseEvent1 = PlaceUtils.Place(baseEvent1);
            baseEvent4 = PlaceUtils.Place(baseEvent4);
            baseEvent2 = PlaceUtils.Place(baseEvent2);
            baseEvent3 = PlaceUtils.Place(baseEvent3);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 -> 4
            eventsContainer.DeleteObject(baseEvent2);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
        }

        [Test]
        public void DeletingSelection()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEvent1 = new BaseEvent{ JsonTime = 1, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };
            var baseEvent2 = new BaseEvent{ JsonTime = 2, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };
            var baseEvent3 = new BaseEvent{ JsonTime = 3, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };
            var baseEvent4 = new BaseEvent{ JsonTime = 4, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn };

            // Check state after placing
            // 1 -> 2 -> 3 -> 4
            baseEvent1 = PlaceUtils.Place(baseEvent1);
            baseEvent4 = PlaceUtils.Place(baseEvent4);
            baseEvent2 = PlaceUtils.Place(baseEvent2);
            baseEvent3 = PlaceUtils.Place(baseEvent3);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 ->
            SelectionController.Select(baseEvent2);
            SelectionController.Select(baseEvent4, true);
            selectionController.Delete();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
        }

        [Test]
        public void ShiftingSelection()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA1 = new BaseEvent{ JsonTime = 1, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventT2 = new BaseEvent{ JsonTime = 2, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventA3 = new BaseEvent{ JsonTime = 3, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventT4 = new BaseEvent{ JsonTime = 4, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventB1 = new BaseEvent{ JsonTime = 1, Type = (int)EventTypeValue.RightLasers, Value = (int)LightValue.BlueOn };
            var baseEventB3 = new BaseEvent{ JsonTime = 3, Type = (int)EventTypeValue.RightLasers, Value = (int)LightValue.BlueOn };

            // Check state after placing
            // A1 -> T2 -> A3 -> T4
            // B1 ->    -> B3 ->
            baseEventA1 = PlaceUtils.Place(baseEventA1);
            baseEventA3 = PlaceUtils.Place(baseEventA3);
            baseEventB1 = PlaceUtils.Place(baseEventB1);
            baseEventB3 = PlaceUtils.Place(baseEventB3);
            baseEventT2 = PlaceUtils.Place(baseEventT2);
            baseEventT4 = PlaceUtils.Place(baseEventT4);

            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after shifting eventT
            // A1 ->    -> A3 ->
            // B1 -> T2 -> B3 -> T4
            SelectionController.Select(baseEventT2);
            SelectionController.Select(baseEventT4, true);
            selectionController.ShiftSelection(1, 0);

            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after undo and redo
            actionContainer.Undo();
            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            actionContainer.Redo();
            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);
        }

        [Test]
        public void MovingSelection()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent { JsonTime = 1, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventT1 = new BaseEvent { JsonTime = 1.5f, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventB = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventT2 = new BaseEvent { JsonTime = 2.5f, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };

            // Check state after placing
            // A -> T1 -> B -> T2
            baseEventA = PlaceUtils.Place(baseEventA);
            baseEventB = PlaceUtils.Place(baseEventB);
            baseEventT1 = PlaceUtils.Place(baseEventT1);
            baseEventT2 = PlaceUtils.Place(baseEventT2);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after moving eventT
            // A ->   -> B -> T1 -> T2
            SelectionController.Select(baseEventT1);
            SelectionController.Select(baseEventT2, true);
            selectionController.MoveSelection(0.75f);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var baseEventA = new BaseEvent { JsonTime = 1, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };
            var baseEventB = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.LeftLasers, Value = (int)LightValue.BlueOn };

            // Check state after placing
            // A -> B
            baseEventA = PlaceUtils.Place(baseEventA);
            baseEventB = PlaceUtils.Place(baseEventB);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after pasting
            // A -> B -> A Copy -> B copy
            SelectionController.Select(baseEventA);
            SelectionController.Select(baseEventB, true);
            atsc.MoveToJsonTime(3);
            if (eventPlacement.QueuedData != null) eventPlacement.QueuedData.JsonTime = 3;
            selectionController.Copy();
            selectionController.Paste();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        private void AssertMapObjectsAreLinkedAndSorted(EventGridContainer eventsContainer, int eventType)
        {
            var laneEvents = eventsContainer.MapObjects.Where(x => x.Type == eventType).ToList();
            CheckUtils.CheckEventsLinksAreCorrectAndSorted(laneEvents);
        }
    }
}
