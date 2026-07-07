using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EventTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupEvents();
        }

        // TODO: need to change rotation event here as well, man
        [Test]
        public void Invert()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var rotationEventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(ObjectType.RotationEvent);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var rotationEventPlacement = Object.FindAnyObjectByType<RotationEventPlacement>();
            var beatmapEventInputController = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var beatmapRotationInputController = Object.FindAnyObjectByType<BeatmapRotationInputController>();

            BaseRotationEvent baseEventA = new BaseRotationEvent { JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, Rotation = 45 };
            BaseEvent baseEventB = new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade };
            baseEventA = PlaceUtils.Place(baseEventA);
            baseEventB = PlaceUtils.Place(baseEventB);

            // TODO: u know, i forgot this events get converted and now i have to suffer the wrath of test pain 
            if (rotationEventsContainer.LoadedContainers[baseEventA] is RotationEventContainer containerA)
                RotationCommand.Invert(containerA.EventData);
            if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB)
                beatmapEventInputController.InvertEvent(containerB);

            CheckUtils.CheckRotationEvent("Perform first rotation inversion", rotationEventsContainer, 0, 2, 1, -45);
            CheckUtils.CheckEvent("Perform first light value inversion", eventsContainer, 0, 3,
                (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade);

            if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB2)
                beatmapEventInputController.InvertEvent(containerB2);

            CheckUtils.CheckEvent("Perform second light value inversion", eventsContainer, 0, 3,
                (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade);

            // Undo invert
            actionContainer.Undo();

            CheckUtils.CheckEvent("Undo second light value inversion", eventsContainer, 0, 3,
                (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade);
            CheckUtils.CheckRotationEvent("Check first rotation inversion", rotationEventsContainer, 0, 2, 1, -45);

            actionContainer.Undo();

            CheckUtils.CheckEvent("Undo first light value inversion", eventsContainer, 0, 3,
                (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
            CheckUtils.CheckRotationEvent("Check first rotation inversion", rotationEventsContainer, 0, 2, 1, -45);

            actionContainer.Undo();

            CheckUtils.CheckRotationEvent("Undo first rotation inversion", rotationEventsContainer, 0, 2, 1, 45);
            CheckUtils.CheckEvent("Check initial light value", eventsContainer, 0, 3,
                (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
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

                BaseEvent baseEventA = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.LeftLaserRotation, Value = 2 };
                baseEventA = PlaceUtils.Place(baseEventA);

                if (eventsContainer.LoadedContainers[baseEventA] is EventContainer containerA)
                    inputController.TweakMain(containerA, 1);

                CheckUtils.CheckEvent("Perform tweak value", eventsContainer, 0, 2,
                    (int)EventTypeValue.LeftLaserRotation, 3);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckEvent("Undo tweak value", eventsContainer, 0, 2, (int)EventTypeValue.LeftLaserRotation,
                    2);
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
                {
                    inputController.TweakMain(containerBoost, 1);
                }

                CheckUtils.CheckEvent("Perform tweak value on boost", eventsContainer, 0, 3, (int)EventTypeValue.ColorBoost, 1);

                if (eventsContainer.LoadedContainers[baseBoostEvent] is EventContainer containerBoostAgain)
                {
                    inputController.TweakMain(containerBoostAgain, 1);
                }

                CheckUtils.CheckEvent("Perform another tweak value on boost", eventsContainer, 0, 3, (int)EventTypeValue.ColorBoost, 0);

                actionContainer.Undo();
                CheckUtils.CheckEvent("Undo tweak value on boost", eventsContainer, 0, 3, (int)EventTypeValue.ColorBoost, 1);

                actionContainer.Undo();
                CheckUtils.CheckEvent("Undo tweak value on boost again", eventsContainer, 0, 3, (int)EventTypeValue.ColorBoost, 0);
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

                BaseEvent baseEventA = new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade };
                baseEventA.CustomEasing = easing;
                baseEventA.CustomColor = color;

                baseEventA = PlaceUtils.Place(baseEventA);

                CheckUtils.CheckEvent("Applies CustomProperties to CustomData", eventsContainer, 0, 3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f,
                    new JSONObject() { ["color"] = color, ["easing"] = easing });
            }
        }
    }
}