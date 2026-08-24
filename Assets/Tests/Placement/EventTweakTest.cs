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
            // An existing Chroma rotation remains precision-adjustable after the directional initialization gesture.
            eventA.CustomRingRotation = 90f;
            eventA.CustomDirection = 0;
            eventA.WriteCustom();
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
        public void TweakUnsetRingRotationUpStartsAtClockwiseNinetyDegrees()
        {
            // The first upward Alt+Scroll is a directional initializer, not a precision increment above the baseline.
            var eventA = PlaceEvent(4.25f, EventTypeValue.Event8);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.Low;

            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            Assert.AreEqual(90f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Initial upward ring rotation");
            Assert.AreEqual(1, eventA.CustomDirection, "Initial upward ring direction");
        }

        [Test]
        public void TweakUnsetRingRotationDownStartsAtCounterClockwiseNinetyDegrees()
        {
            // The first downward Alt+Scroll initializes the same positive magnitude and expresses CCW through direction.
            var eventA = PlaceEvent(4.5f, EventTypeValue.Event8);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.Low;

            controller.TweakMain(GetContainer(eventA), -1);
            eventA = Refresh(eventA);

            Assert.AreEqual(90f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Initial downward ring rotation");
            Assert.AreEqual(0, eventA.CustomDirection, "Initial downward ring direction");
        }

        [Test]
        public void TweakRingRotationDownClampsAtZero()
        {
            // Crossing zero must clamp the non-negative magnitude instead of encoding direction through a negative rotation.
            var eventA = PlaceEvent(4.75f, EventTypeValue.Event8);
            eventA.CustomRingRotation = 10f;
            eventA.CustomDirection = 0;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.Low;

            controller.TweakMain(GetContainer(eventA), -1);
            eventA = Refresh(eventA);

            Assert.AreEqual(0f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Ring rotation lower bound");
            Assert.AreEqual(0, eventA.CustomDirection, "Clamping rotation must preserve direction");
        }

        [Test]
        public void TweakNegativeDirectedRingRotationNormalizesEquivalentDirectionBeforeEditing()
        {
            // A signed rotation with an explicit direction is redundant, so normalize it before applying the scroll delta.
            var eventA = PlaceEvent(4.875f, EventTypeValue.Event8);
            eventA.CustomRingRotation = -90f;
            eventA.CustomDirection = 1;
            eventA.WriteCustom();
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            precision.CurrentPrecision = ScrollPrecision.High;

            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            Assert.AreEqual(92.5f, eventA.CustomRingRotation ?? float.NaN, 0.001f, "Normalized ring rotation");
            Assert.AreEqual(0, eventA.CustomDirection, "Normalized ring direction");
        }

        [Test]
        public void TweakRingZoomMainValue()
        {
            var eventA = PlaceEvent(5, EventTypeValue.Event9);
            var original = BeatmapFactory.Clone(eventA);
            var controller = Object.FindAnyObjectByType<BeatmapEventInputController>();
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();

            // Ring zoom starts at its 2-unit baseline and Medium uses the configured 0.25-unit zoom step.
            // Expecting 2.25 keeps this regression synchronized with the current dedicated precision ladder.
            precision.CurrentPrecision = ScrollPrecision.Medium;
            controller.TweakMain(GetContainer(eventA), 1);
            eventA = Refresh(eventA);

            AssertEventWithChroma(
                original,
                eventA,
                e =>
                {
                    e.CustomStep = 2.25f;
                    e.WriteCustom();
                },
                2.25f,
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
