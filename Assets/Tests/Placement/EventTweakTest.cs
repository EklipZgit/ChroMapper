using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Placement
{
    public class EventTweakTest : TestBase
    {
        [Test]
        public void TweakLaserSpeedMainValue()
        {
            var eventA = PlaceEvent(2, EventTypeValue.Event12, 2);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Laser speed depends on scroll precision; Medium makes the main-value increment exactly one.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e => e.Value = 3,
                3f,
                e => e.CustomSpeed,
                "Laser speed main value");
        }

        [Test]
        public void TweakRingRotationMainValue()
        {
            var eventA = PlaceEvent(4, EventTypeValue.Event8);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Ring rotation depends on scroll precision; High selects the 2.5-degree main-value increment.
            precision.CurrentPrecision = ScrollPrecision.High;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e =>
                {
                    e.CustomRingRotation = 92.5f;
                    e.WriteCustom();
                },
                92.5f,
                e => e.CustomRingRotation,
                "Ring rotation main value");
        }

        [Test]
        public void TweakRingZoomMainValue()
        {
            var eventA = PlaceEvent(5, EventTypeValue.Event9);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Ring zoom depends on scroll precision; Medium selects the 0.25-step main value.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e =>
                {
                    e.CustomStep = 0.25f;
                    e.WriteCustom();
                },
                0.25f,
                e => e.CustomStep,
                "Ring zoom main value");
        }

        [Test]
        public void TweakLightMainBrightness()
        {
            var eventA = PlaceEvent(6, EventTypeValue.Event0, 2);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Light brightness depends on scroll precision; Medium selects the 0.1 brightness increment.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            BeatmapAssertion.IsEqualWithChanges(
                original,
                eventA,
                e => e.FloatValue = 1.1f,
                "Light main brightness");
        }

        [Test]
        public void TweakColorBoostMainValue()
        {
            var eventA = PlaceEvent(7, EventTypeValue.ColorBoostEventType);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            BeatmapAssertion.IsEqualWithChanges(
                original,
                eventA,
                e => e.Value = 1,
                "Color boost main value");
        }

        [Test]
        public void InvertLaserSpeedDirection()
        {
            var eventA = PlaceEvent(8, EventTypeValue.Event12, 2);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.InvertEvent(GetContainer(eventA));
            eventA = Refresh(eventA);

            Assert.AreEqual(1, eventA.CustomDirection, "Laser direction Chroma value");
        }

        [Test]
        public void InvertRingRotationDirection()
        {
            var eventA = PlaceEvent(9, EventTypeValue.Event8);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.InvertEvent(GetContainer(eventA));
            eventA = Refresh(eventA);

            Assert.AreEqual(1, eventA.CustomDirection, "Ring rotation direction Chroma value");
        }

        [Test]
        public void InvertRingZoomStep()
        {
            var eventA = PlaceEvent(10, EventTypeValue.Event9);
            eventA.CustomStep = 0.25f;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            controller.InvertEvent(GetContainer(eventA));
            eventA = Refresh(eventA);

            Assert.AreEqual(-0.25f, eventA.CustomStep, 0.001f, "Ring zoom step Chroma value");
        }

        private static BaseEvent PlaceEvent(float time, EventTypeValue type, int value = 0)
        {
            return PlaceUtils.Place(new BaseEvent { JsonTime = time, Type = (int)type, Value = value });
        }

        private static EventContainer GetContainer(BaseEvent beatmapEvent)
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            if (eventsContainer.LoadedContainers[beatmapEvent] is not EventContainer container)
                throw new Exception($"Wrong event container for type {beatmapEvent.Type}");
            return container;
        }

        private static BaseEvent Refresh(BaseEvent beatmapEvent)
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            return eventsContainer.MapObjects.First(
                x => Mathf.Approximately(x.JsonTime, beatmapEvent.JsonTime) && x.Type == beatmapEvent.Type);
        }

        private static void AssertEventWithChroma(
            BaseEvent baseline,
            BaseEvent actual,
            Action<BaseEvent> applyExpectedChanges,
            float expectedChromaValue,
            Func<BaseEvent, float?> getChromaValue,
            string message)
        {
            BeatmapAssertion.IsEqualWithChanges(baseline, actual, e =>
            {
                e.FloatValue = 1f;
                applyExpectedChanges(e);
            }, message);
            Assert.AreEqual(expectedChromaValue, getChromaValue(actual) ?? actual.Value, 0.001f, message + ": Chroma value");
        }
    }
}
