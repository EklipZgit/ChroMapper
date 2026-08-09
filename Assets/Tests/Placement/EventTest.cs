using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Placement
{
    public class EventTest : TestBase
    {
        [Test]
        public void InvertBasicRotationEvent()
        {
            var rotationEventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
                    ObjectType.RotationEvent);

            var eventA = new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 45
            };
            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedRotInverted = BeatmapFactory.Clone(originalEventA);
            expectedRotInverted.Rotation = -45;
            var expectedRotUninverted = BeatmapFactory.Clone(originalEventA);

            if (rotationEventsContainer.LoadedContainers[eventA] is not RotationEventContainer containerA)
                throw new Exception($"Wrong type {rotationEventsContainer.LoadedContainers[eventA].GetType().FullName}"); // Assert.Fail doesn't tell the compiler it always terminates, line below wouldn't compile without throw

            eventA = RotationCommand.Invert(containerA.EventData);

            BeatmapAssertion.IsEqual(
                expectedRotInverted,
                eventA,
                "Perform first rotation inversion");

            var undoRotationObjects = PlaceUtils.Undo<BaseRotationEvent>().ToList();

            BeatmapAssertion.IsEqual(
                expectedRotUninverted,
                undoRotationObjects[0],
                "Undo first rotation inversion");
        }

        [Test]
        public void InvertBasicLightEvent()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var inputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

            var eventB = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.Event0, Value = (int)LightValue.RedFade
            };
            var originalEventB = BeatmapFactory.Clone(eventB);
            eventB = PlaceUtils.Place(eventB);
            var expectedLightFirstInvert = BeatmapFactory.Clone(originalEventB);
            expectedLightFirstInvert.Value = (int)LightValue.WhiteFade;
            expectedLightFirstInvert.FloatValue = 1f;
            var expectedLightSecondInvert = BeatmapFactory.Clone(originalEventB);
            expectedLightSecondInvert.Value = (int)LightValue.BlueFade;
            expectedLightSecondInvert.FloatValue = 1f;
            var expectedLightUndoFirstInvert = BeatmapFactory.Clone(originalEventB);
            expectedLightUndoFirstInvert.FloatValue = 1f;

            if (eventsContainer.LoadedContainers[eventB] is not EventContainer containerB)
                throw new Exception($"Wrong type {eventsContainer.LoadedContainers[eventB].GetType().FullName}");

            inputController.InvertEvent(containerB);
            // Invert finalizes through an update action, so refresh the live reference after replacement.
            eventB = eventsContainer.MapObjects.First(x => x.JsonTime == 3 && x.Type == (int)EventTypeValue.Event0);
            BeatmapAssertion.IsEqual(
                expectedLightFirstInvert,
                eventB,
                "Perform first light value inversion");

            if (eventsContainer.LoadedContainers[eventB] is EventContainer containerB2)
                inputController.InvertEvent(containerB2);

            // The second invert also replaces the live object; assert against the newly mapped instance.
            eventB = eventsContainer.MapObjects.First(x => x.JsonTime == 3 && x.Type == (int)EventTypeValue.Event0);
            BeatmapAssertion.IsEqual(
                expectedLightSecondInvert,
                eventB,
                "Perform second light value inversion");

            var undoSecondLightInvertObjects = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.IsEqual(
                expectedLightFirstInvert,
                undoSecondLightInvertObjects[0],
                "Undo second light value inversion");

            var undoFirstLightInvertObjects = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.IsEqual(
                expectedLightUndoFirstInvert,
                undoFirstLightInvertObjects[0],
                "Undo first light value inversion");
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            if (eventsContainer == null) Assert.Fail("Event container is missing somehow");
            var color = new Color(0, 1, 2, 3);
            var easing = "easeOutQuad";

            var eventA = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.Event0, Value = (int)LightValue.RedFade
            };
            eventA.CustomEasing = easing;
            eventA.CustomColor = color;

            var expectedCustomProperty = BeatmapFactory.Clone(eventA);
            expectedCustomProperty.FloatValue = 1f;
            expectedCustomProperty.CustomData = new JSONObject { ["color"] = color, ["easing"] = easing };

            eventA = PlaceUtils.Place(eventA);

            BeatmapAssertion.IsEqual(
                expectedCustomProperty,
                eventA,
                "Applies CustomProperties to CustomData");
        }
    }
}
