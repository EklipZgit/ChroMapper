using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class EventNextPrevLightIDTest : TestBase
    {
        protected override void OnReturnSettings()
        {
            Settings.Instance.LightIDTransitionSupport = false;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            // This is an opt-in setting
            Settings.Instance.LightIDTransitionSupport = true;
        }

        protected override void BeforeCleanup()
        {
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event)
                .PropagationEditing = EventGridContainer.PropMode.Off;
        }

        private BaseEvent EventWithTimeAndLightID(float time, int? lightID)
        {
            Settings.Instance.MapVersion = 3;

            var customData = lightID.HasValue
                ? new JSONObject { ["lightID"] = new JSONArray { [0] = lightID } }
                : null;

            var evt = new BaseEvent
            {
                JsonTime = time,
                Type = (int)EventTypeValue.CenterLights,
                Value = (int)LightValue.BlueOn,
                CustomData = customData
            };
            return evt;
        }

        [Test]
        public void Placement()
        {
            var actionsContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            var V1 = EventWithTimeAndLightID(1, null);
            var V10 = EventWithTimeAndLightID(10, null);

            var A2 = EventWithTimeAndLightID(2, 1);
            var A4 = EventWithTimeAndLightID(4, 1);
            var A12 = EventWithTimeAndLightID(12, 1);

            var B3 = EventWithTimeAndLightID(3, 2);
            var B5 = EventWithTimeAndLightID(5, 2);
            var B13 = EventWithTimeAndLightID(13, 2);

            // Check state after placing
            PlaceEvents(ref V1, ref A2, ref B3, ref A4, ref B5, ref V10, ref A12, ref B13);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after deleting
            // V1             V10
            //    A2              A12
            //       B3    B5          B13
            eventsContainer.DeleteObject(A4);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            eventsContainer.DeleteObject(V10);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var actionsContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            var V1 = EventWithTimeAndLightID(1, null);
            var V10 = EventWithTimeAndLightID(10, null);

            var A2 = EventWithTimeAndLightID(2, 1);
            var A4 = EventWithTimeAndLightID(4, 1);
            var A12 = EventWithTimeAndLightID(12, 1);

            var B3 = EventWithTimeAndLightID(3, 2);
            var B5 = EventWithTimeAndLightID(5, 2);
            var B13 = EventWithTimeAndLightID(13, 2);

            PlaceEvents(ref V1, ref A2, ref B3, ref A4, ref B5, ref V10, ref A12, ref B13);

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            SelectionController.Select(A4);
            SelectionController.Select(V10, true);
            selectionController.Delete();
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var actionsContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            var V1 = EventWithTimeAndLightID(1, null);
            var V10 = EventWithTimeAndLightID(10, null);

            var A2 = EventWithTimeAndLightID(2, 1);
            var A4 = EventWithTimeAndLightID(4, 1);
            var A12 = EventWithTimeAndLightID(12, 1);

            var B3 = EventWithTimeAndLightID(3, 2);
            var B5 = EventWithTimeAndLightID(5, 2);
            var B13 = EventWithTimeAndLightID(13, 2);

            PlaceEvents(ref V1, ref A2, ref B3, ref A4, ref B5, ref V10, ref A12, ref B13);

            // Check state after pasting
            // V1             V1C         V10
            //    A2    A4        A2C         A12     A12C
            //       B3    B5         B3C         B13      B13C
            SelectionController.Select(V1);
            SelectionController.Select(A2, true);
            SelectionController.Select(B3, true);
            SelectionController.Select(A12, true);
            SelectionController.Select(B13, true);
            atsc.MoveToJsonTime(6);
            selectionController.Copy();
            selectionController.Paste();

            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);


            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void ShiftingSelection()
        {
            var actionsContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            var V1 = EventWithTimeAndLightID(1, null);
            var V10 = EventWithTimeAndLightID(10, null);

            var A2 = EventWithTimeAndLightID(2, 1);
            var A4 = EventWithTimeAndLightID(4, 1);
            var A12 = EventWithTimeAndLightID(12, 1);

            var B3 = EventWithTimeAndLightID(3, 2);
            var B5 = EventWithTimeAndLightID(5, 2);
            var B13 = EventWithTimeAndLightID(13, 2);

            PlaceEvents(ref V1, ref A2, ref B3, ref A4, ref B5, ref V10, ref A12, ref B13);

            // Check state after shifting
            // V1                
            //    A2    A4    V10 A12
            //       B3    B5          B13
            SelectionController.Select(V10);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after shifting
            // V1                
            //    A2              A12
            //       B3 A4 B5 V10      B13
            SelectionController.Select(A4);
            SelectionController.Select(V10, true);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var actionsContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            var V1 = EventWithTimeAndLightID(1, null);
            var V10 = EventWithTimeAndLightID(10, null);

            var A2 = EventWithTimeAndLightID(2, 1);
            var A4 = EventWithTimeAndLightID(4, 1);
            var A12 = EventWithTimeAndLightID(12, 1);

            var B3 = EventWithTimeAndLightID(3, 2);
            var B5 = EventWithTimeAndLightID(5, 2);
            var B13 = EventWithTimeAndLightID(13, 2);

            PlaceEvents(ref V1, ref A2, ref B3, ref A4, ref B5, ref V10, ref A12, ref B13);

            // Check state after moving
            // V1             V10
            //          A4        A12      A2
            //             B5          B13    B3
            SelectionController.Select(A2);
            SelectionController.Select(B3, true);
            selectionController.MoveSelection(12);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        private void AssertMapObjectsLinksState(EventGridContainer eventsContainer)
        {
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights, null);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights, 1);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights, 2);
        }

        private static void PlaceEvents(
            ref BaseEvent v1,
            ref BaseEvent a2,
            ref BaseEvent b3,
            ref BaseEvent a4,
            ref BaseEvent b5,
            ref BaseEvent v10,
            ref BaseEvent a12,
            ref BaseEvent b13)
        {
            var placedEvents = PlaceUtils.Place(
                new List<BaseEvent>
                {
                    v1,
                    a2,
                    b3,
                    a4,
                    b5,
                    v10,
                    a12,
                    b13
                });

            v1 = placedEvents[0];
            a2 = placedEvents[1];
            b3 = placedEvents[2];
            a4 = placedEvents[3];
            b5 = placedEvents[4];
            v10 = placedEvents[5];
            a12 = placedEvents[6];
            b13 = placedEvents[7];
        }

        private void AssertMapObjectsAreLinkedAndSorted(EventGridContainer eventsContainer, int eventType, int? lightID)
        {
            var laneEvents = lightID == null
                ? eventsContainer.MapObjects.Where(x => x.Type == eventType && x.CustomLightID == null).ToList()
                : eventsContainer
                    .MapObjects.Where(x =>
                        x.Type == eventType && x.CustomLightID != null && x.CustomLightID[0] == lightID)
                    .ToList();

            BeatmapAssertion.IsEqual(
                BeatmapAssertion.EventsAreLinkedAndSorted,
                laneEvents,
                "Events are linked and sorted");
        }
    }
}