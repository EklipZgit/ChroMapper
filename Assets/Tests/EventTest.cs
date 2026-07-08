using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class EventTest : TestBase
    {
        // TODO: need to change rotation event here as well, man
        [Test]
        public void Invert()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var rotationEventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
                    ObjectType.RotationEvent);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var rotationEventPlacement = Object.FindAnyObjectByType<RotationEventPlacement>();
            var beatmapEventInputController = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var beatmapRotationInputController = Object.FindAnyObjectByType<BeatmapRotationInputController>();

            var baseEventA = new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, Rotation = 45
            };
            var baseEventB = new BaseEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade
            };
            baseEventA = PlaceUtils.Place(baseEventA);
            baseEventB = PlaceUtils.Place(baseEventB);

            // TODO: u know, i forgot this events get converted and now i have to suffer the wrath of test pain 
            if (rotationEventsContainer.LoadedContainers[baseEventA] is RotationEventContainer containerA)
                RotationCommand.Invert(containerA.EventData);
            if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB)
                beatmapEventInputController.InvertEvent(containerB);

            BeatmapAssertion.IsEqual(
                new BaseRotationEvent { JsonTime = 2, Type = 1 == 0 ? 14 : 15, Rotation = -45 },
                rotationEventsContainer.MapObjects[0],
                "Perform first rotation inversion");
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 3,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.WhiteFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform first light value inversion");

            if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB2)
                beatmapEventInputController.InvertEvent(containerB2);

            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 3,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.BlueFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform second light value inversion");

            // Undo invert
            actionContainer.Undo();

            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 3,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.WhiteFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Undo second light value inversion");
            BeatmapAssertion.IsEqual(
                new BaseRotationEvent { JsonTime = 2, Type = 1 == 0 ? 14 : 15, Rotation = -45 },
                rotationEventsContainer.MapObjects[0],
                "Check first rotation inversion");

            actionContainer.Undo();

            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 3,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.RedFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Undo first light value inversion");
            BeatmapAssertion.IsEqual(
                new BaseRotationEvent { JsonTime = 2, Type = 1 == 0 ? 14 : 15, Rotation = -45 },
                rotationEventsContainer.MapObjects[0],
                "Check first rotation inversion");

            actionContainer.Undo();

            BeatmapAssertion.IsEqual(
                new BaseRotationEvent { JsonTime = 2, Type = 1 == 0 ? 14 : 15, Rotation = 45 },
                rotationEventsContainer.MapObjects[0],
                "Undo first rotation inversion");
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 3,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.RedFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Check initial light value");
        }

        [Test]
        public void TweakValue()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

                var baseEventA =
                    new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.LeftLaserRotation, Value = 2 };
                baseEventA = PlaceUtils.Place(baseEventA);

                if (eventsContainer.LoadedContainers[baseEventA] is EventContainer containerA)
                    inputController.TweakMain(containerA, 1);

                BeatmapAssertion.IsEqual(
                    new BaseEvent
                    {
                        JsonTime = 2, Type = (int)EventTypeValue.LeftLaserRotation, Value = 3, FloatValue = 1f
                    },
                    eventsContainer.MapObjects[0],
                    "Perform tweak value");

                // Undo invert
                actionContainer.Undo();

                BeatmapAssertion.IsEqual(
                    new BaseEvent
                    {
                        JsonTime = 2, Type = (int)EventTypeValue.LeftLaserRotation, Value = 2, FloatValue = 1f
                    },
                    eventsContainer.MapObjects[0],
                    "Undo tweak value");
            }
        }

        [Test]
        public void TweakValueBoost()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapEventInputController>();

                var baseBoostEvent = new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.ColorBoost, Value = 0 };
                baseBoostEvent = PlaceUtils.Place(baseBoostEvent);

                if (eventsContainer.LoadedContainers[baseBoostEvent] is EventContainer containerBoost)
                    inputController.TweakMain(containerBoost, 1);

                BeatmapAssertion.IsEqual(
                    new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.ColorBoost, Value = 1, FloatValue = 1f },
                    eventsContainer.MapObjects[0],
                    "Perform tweak value on boost");

                if (eventsContainer.LoadedContainers[baseBoostEvent] is EventContainer containerBoostAgain)
                    inputController.TweakMain(containerBoostAgain, 1);

                BeatmapAssertion.IsEqual(
                    new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.ColorBoost, Value = 0, FloatValue = 1f },
                    eventsContainer.MapObjects[0],
                    "Perform another tweak value on boost");

                actionContainer.Undo();
                BeatmapAssertion.IsEqual(
                    new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.ColorBoost, Value = 1, FloatValue = 1f },
                    eventsContainer.MapObjects[0],
                    "Undo tweak value on boost");

                actionContainer.Undo();
                BeatmapAssertion.IsEqual(
                    new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.ColorBoost, Value = 0, FloatValue = 1f },
                    eventsContainer.MapObjects[0],
                    "Undo tweak value on boost again");
            }
        }


        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

                var color = new Color(0, 1, 2, 3);
                var easing = "easeOutQuad";

                var baseEventA = new BaseEvent
                {
                    JsonTime = 3, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade
                };
                baseEventA.CustomEasing = easing;
                baseEventA.CustomColor = color;

                baseEventA = PlaceUtils.Place(baseEventA);

                BeatmapAssertion.IsEqual(
                    new BaseEvent
                    {
                        JsonTime = 3,
                        Type = (int)EventTypeValue.BackLasers,
                        Value = (int)LightValue.RedFade,
                        FloatValue = 1f,
                        CustomData = new JSONObject { ["color"] = color, ["easing"] = easing }
                    },
                    eventsContainer.MapObjects[0],
                    "Applies CustomProperties to CustomData");
            }
        }
    }
}