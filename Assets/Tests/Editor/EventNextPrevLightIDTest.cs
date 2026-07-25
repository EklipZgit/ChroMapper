using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
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

        [Test]
        public void Placement()
        {
            var scenario = new LightIDScenario();
            scenario.Place();

            // Check state after placing
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after deleting
            // V1             V10
            //    A2              A12
            //       B3    B5          B13
            PlaceUtils.Delete(scenario.A4);
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            PlaceUtils.Delete(scenario.V10);
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertAllLinkGroups(scenario.EventsContainer);

            PlaceUtils.Redo();
            AssertAllLinkGroups(scenario.EventsContainer);
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var scenario = new LightIDScenario();
            scenario.Place();

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            SelectionController.Select(scenario.A4);
            SelectionController.Select(scenario.V10, true);
            selectionController.Delete();
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertAllLinkGroups(scenario.EventsContainer);

            PlaceUtils.Redo();
            AssertAllLinkGroups(scenario.EventsContainer);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var scenario = new LightIDScenario();
            scenario.Place();

            // Check state after pasting
            // V1             V1C         V10
            //    A2    A4        A2C         A12     A12C
            //       B3    B5         B3C         B13      B13C
            SelectionController.Select(scenario.V1);
            SelectionController.Select(scenario.A2, true);
            SelectionController.Select(scenario.B3, true);
            SelectionController.Select(scenario.A12, true);
            SelectionController.Select(scenario.B13, true);
            atsc.MoveToJsonTime(6);
            selectionController.Copy();
            selectionController.Paste();

            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertAllLinkGroups(scenario.EventsContainer);

            PlaceUtils.Redo();
            AssertAllLinkGroups(scenario.EventsContainer);
        }

        [Test]
        public void ShiftingSelection()
        {
            var scenario = new LightIDScenario();
            scenario.EventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;
            scenario.Place();

            // Check state after shifting
            // V1                
            //    A2    A4    V10 A12
            //       B3    B5          B13
            SelectionController.Select(scenario.V10);
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after shifting
            // V1                
            //    A2              A12
            //       B3 A4 B5 V10      B13
            SelectionController.Select(scenario.A4);
            SelectionController.Select(scenario.V10, true);
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertAllLinkGroups(scenario.EventsContainer);

            PlaceUtils.Redo();
            AssertAllLinkGroups(scenario.EventsContainer);
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var scenario = new LightIDScenario();
            scenario.Place();

            // Check state after moving
            // V1             V10
            //          A4        A12      A2
            //             B5          B13    B3
            SelectionController.Select(scenario.A2);
            SelectionController.Select(scenario.B3, true);
            selectionController.MoveSelection(12);
            AssertAllLinkGroups(scenario.EventsContainer);

            // Check state after undo and redo
            PlaceUtils.Undo();
            AssertAllLinkGroups(scenario.EventsContainer);

            PlaceUtils.Redo();
            AssertAllLinkGroups(scenario.EventsContainer);
        }

        private void AssertAllLinkGroups(EventGridContainer eventsContainer)
        {
            BeatmapAssertion.IsEqual(BeatmapAssertion.EventsAreSorted, eventsContainer.MapObjects, "Events are sorted");
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.Event4, null);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.Event4, 1);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.Event4, 2);
        }

        private static void AssertMapObjectsAreLinkedAndSorted(
            EventGridContainer eventsContainer,
            int eventType,
            int? lightID)
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

        /// <summary>
        /// Sealed scenario encapsulating the 8 named events used across all LightID tests.
        /// </summary>
        private sealed class LightIDScenario
        {
            public BaseEvent V1;
            public BaseEvent V10;
            public BaseEvent A2;
            public BaseEvent A4;
            public BaseEvent A12;
            public BaseEvent B3;
            public BaseEvent B5;
            public BaseEvent B13;

            /// <summary>
            /// Events in placement order (ascending by time).
            /// </summary>
            public IEnumerable<BaseEvent> All => new[] { V1, A2, B3, A4, B5, V10, A12, B13 };

            public EventGridContainer EventsContainer { get; }

            static LightIDScenario()
            {
                Settings.Instance.MapVersion = 3;
            }

            public LightIDScenario()
            {
                EventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

                V1 = MakeEvent(1f, null);
                A2 = MakeEvent(2f, 1);
                B3 = MakeEvent(3f, 2);
                A4 = MakeEvent(4f, 1);
                B5 = MakeEvent(5f, 2);
                V10 = MakeEvent(10f, null);
                A12 = MakeEvent(12f, 1);
                B13 = MakeEvent(13f, 2);
            }

            /// <summary>
            /// Place all events into the beatmap and store returned references in place fields.
            /// </summary>
            public void Place()
            {
                var placed = PlaceUtils.Place(All).ToList();
                V1 = (BaseEvent)placed[0];
                A2 = (BaseEvent)placed[1];
                B3 = (BaseEvent)placed[2];
                A4 = (BaseEvent)placed[3];
                B5 = (BaseEvent)placed[4];
                V10 = (BaseEvent)placed[5];
                A12 = (BaseEvent)placed[6];
                B13 = (BaseEvent)placed[7];
            }

            private static BaseEvent MakeEvent(float time, int? lightID)
            {
                var customData = lightID.HasValue
                    ? new JSONObject { ["lightID"] = new JSONArray { [0] = lightID } }
                    : null;

                return new BaseEvent
                {
                    JsonTime = time,
                    Type = (int)EventTypeValue.Event4,
                    Value = (int)LightValue.BlueOn,
                    CustomData = customData
                };
            }
        }
    }
}
