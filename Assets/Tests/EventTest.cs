using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class EventTest : TestBase
    {
        [Test]
        public void Invert()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var rotationEventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
                    ObjectType.RotationEvent);

            var beatmapEventInputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

            var eventA = new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, Rotation = 45
            };
            var eventB = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade
            };
            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);
            var originalEventB = BeatmapFactory.Clone(eventB);
            eventB = PlaceUtils.Place(eventB);

            var expectedRotInverted = BeatmapFactory.Clone(originalEventA);
            expectedRotInverted.Rotation = -45;
            var expectedLightFirstInvert = BeatmapFactory.Clone(originalEventB);
            expectedLightFirstInvert.Value = (int)LightValue.WhiteFade;
            expectedLightFirstInvert.FloatValue = 1f;
            var expectedLightSecondInvert = BeatmapFactory.Clone(originalEventB);
            expectedLightSecondInvert.Value = (int)LightValue.BlueFade;
            expectedLightSecondInvert.FloatValue = 1f;
            var expectedLightUndoFirstInvert = BeatmapFactory.Clone(originalEventB);
            expectedLightUndoFirstInvert.FloatValue = 1f;
            var expectedRotUninverted = BeatmapFactory.Clone(originalEventA);
            var expectedLightInitial = BeatmapFactory.Clone(originalEventB);
            expectedLightInitial.FloatValue = 1f;

            if (rotationEventsContainer.LoadedContainers[eventA] is RotationEventContainer containerA)
                eventA = RotationCommand.Invert(containerA.EventData);
            if (eventsContainer.LoadedContainers[eventB] is EventContainer containerB)
                beatmapEventInputController.InvertEvent(containerB);

            BeatmapAssertion.IsEqual(
                expectedRotInverted,
                eventA,
                "Perform first rotation inversion");
            BeatmapAssertion.IsEqual(
                expectedLightFirstInvert,
                eventB,
                "Perform first light value inversion");

            if (eventsContainer.LoadedContainers[eventB] is EventContainer containerB2)
                beatmapEventInputController.InvertEvent(containerB2);

            BeatmapAssertion.IsEqual(
                expectedLightSecondInvert,
                eventB,
                "Perform second light value inversion");

            var undoSecondLightInvertObjects = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.IsEqual(
                expectedLightFirstInvert,
                undoSecondLightInvertObjects[0],
                "Undo second light value inversion");
            BeatmapAssertion.IsEqual(
                expectedRotInverted,
                eventA,
                "Check first rotation inversion");

            var undoFirstLightInvertObjects = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.IsEqual(
                expectedLightUndoFirstInvert,
                undoFirstLightInvertObjects[0],
                "Undo first light value inversion");
            BeatmapAssertion.IsEqual(
                expectedRotInverted,
                eventA,
                "Check first rotation inversion");

            var undoRotationObjects = PlaceUtils.Undo<BaseRotationEvent>().ToList();

            BeatmapAssertion.IsEqual(
                expectedRotUninverted,
                undoRotationObjects[0],
                "Undo first rotation inversion");
            BeatmapAssertion.IsEqual(
                expectedLightInitial,
                undoFirstLightInvertObjects[0],
                "Check initial light value");
        }

        [Test]
        public void TweakValue()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var inputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

                var eventA =
                    new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.LeftLaserRotation, Value = 2 };
                var originalEventA = BeatmapFactory.Clone(eventA);
                eventA = PlaceUtils.Place(eventA);

                if (eventsContainer.LoadedContainers[eventA] is EventContainer containerA)
                    inputController.TweakMain(containerA, 1);

                BeatmapAssertion.IsEqualWithChanges(
                    originalEventA,
                    eventA,
                    e => { e.Value = 3; e.FloatValue = 1f; },
                    "Perform tweak value");

                var undoObjects = PlaceUtils.Undo<BaseEvent>().ToList();

                BeatmapAssertion.IsEqualWithChanges(
                    originalEventA,
                    undoObjects[0],
                    e => { e.FloatValue = 1f; },
                    "Undo tweak value");
            }
        }

        [Test]
        public void TweakValueBoost()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event) as EventGridContainer;
            if (eventsContainer == null) Assert.Fail("Event container is missing somehow");
            var inputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

            var boostEvent = new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.ColorBoost, Value = 0 };
            var originalBoostEvent = BeatmapFactory.Clone(boostEvent);
            boostEvent = PlaceUtils.Place(boostEvent);

            if (eventsContainer.LoadedContainers[boostEvent] is EventContainer containerBoost)
                inputController.TweakMain(containerBoost, 1);

            BeatmapAssertion.IsEqualWithChanges(
                originalBoostEvent,
                boostEvent,
                e => { e.Value = 1; e.FloatValue = 1f; },
                "Perform tweak value on boost");

            if (eventsContainer.LoadedContainers[boostEvent] is EventContainer containerBoostAgain)
                inputController.TweakMain(containerBoostAgain, 1);

            BeatmapAssertion.IsEqualWithChanges(
                originalBoostEvent,
                boostEvent,
                e => { e.FloatValue = 1f; },
                "Perform another tweak value on boost");

            var undoTweak2Objects = PlaceUtils.Undo<BaseEvent>().ToList();
            BeatmapAssertion.IsEqualWithChanges(
                originalBoostEvent,
                undoTweak2Objects[0],
                e => { e.Value = 1; e.FloatValue = 1f; },
                "Undo tweak value on boost");

            var undoTweak1Objects = PlaceUtils.Undo<BaseEvent>().ToList();
            BeatmapAssertion.IsEqualWithChanges(
                originalBoostEvent,
                undoTweak1Objects[0],
                e => { e.FloatValue = 1f; },
                "Undo tweak value on boost again");
        }


        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event) as EventGridContainer;
            if (eventsContainer == null) Assert.Fail("Event container is missing somehow");
            var color = new Color(0, 1, 2, 3);
            var easing = "easeOutQuad";

            var eventA = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade
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
