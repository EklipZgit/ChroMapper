using System.Collections;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Visual
{
    public class CountersPlusTest : TestBase
    {
        private const float delta = 0.001f;
        private AudioTimeSyncController atsc;

        private CountersPlusController countersPlusController;

        [SetUp]
        public void EnableCountersPlus()
        {
            Settings.Instance.CountersPlus["enabled"] = true;
            countersPlusController = Object.FindAnyObjectByType<CountersPlusController>();

            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
        }

        protected override void AfterCleanup()
        {
            countersPlusController.UpdateStatistic(CountersPlusStatistic.NJSEvents);
        }

        [Test]
        public void NJSEventsStats_InitialState()
        {
            Assert.AreEqual(10f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
        }

        [UnityTest]
        public IEnumerator NJSEventsStats_CursorBeforeNJSEvent()
        {
            atsc.MoveToJsonTime(7.5f);

            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 10, RelativeNJS = 10 // 20 NJS
                });
            yield return null;

            // 75% of the way from 10 to 20 -> NJS and JD increases
            Assert.AreEqual(17.5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(42f, countersPlusController.CurrentJD, delta);

            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 10,
                    RelativeNJS = 10, // 20 NJS
                    Easing = 1
                });
            yield return null;

            // 75% of the way after easing is 56.25%
            Assert.AreEqual(15.625f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(37.5f, countersPlusController.CurrentJD, delta);

            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 10, RelativeNJS = -8 // 2 NJS
                });
            yield return null;

            // 70% of the way form 10 to 2 -> NJS decreases while HJD and RT increases
            Assert.AreEqual(4f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(5f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(3000f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
        }

        [UnityTest]
        public IEnumerator NJSEventsStats_CursorAfterNJSEvent()
        {
            atsc.MoveToJsonTime(7.5f);

            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    RelativeNJS = 10 // 20 NJS
                });
            yield return null;

            // Doubled NJS and JD
            Assert.AreEqual(20f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(48f, countersPlusController.CurrentJD, delta);

            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    RelativeNJS = -5 // 5 NJS
                });
            yield return null;

            // Halved NJS and Doubled HJD and RT
            Assert.AreEqual(5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(4f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(2400f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
        }

        [UnityTest]
        public IEnumerator NJSEventsStats_CursorBetweenNJSEvents()
        {
            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 0, RelativeNJS = -5 // 5 NJS
                });
            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 10, RelativeNJS = 5 // 15 NJS
                });

            atsc.MoveToJsonTime(2.5f);
            yield return null;

            Debug.Log(
                $"{countersPlusController.CurrentNJS} {countersPlusController.CurrentHJD} {countersPlusController.CurrentRT} {countersPlusController.CurrentJD}");

            // Halved between first njs event and base njs 
            Assert.AreEqual(7.5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2.666f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1600f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);

            atsc.MoveToJsonTime(7.5f);
            yield return null;

            // Halfway between base njs and second njs event
            Assert.AreEqual(12.5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(30f, countersPlusController.CurrentJD, delta);
        }

        [UnityTest]
        public IEnumerator NJSEventsStats_CursorBetweenExtendNJSEvents()
        {
            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 0, RelativeNJS = -5 // 5 NJS
                });
            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 5,
                    RelativeNJS = 999, // Ignored
                    UsePrevious = 1
                });
            PlaceUtils.Place(
                new BaseNJSEvent
                {
                    JsonTime = 10, RelativeNJS = 5 // 15 NJS
                });

            atsc.MoveToJsonTime(2.5f);
            yield return null;

            // Between first NJS event and extend event => State of the first NJS 
            Assert.AreEqual(5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(4f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(2400f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);

            atsc.MoveToJsonTime(7.5f);
            yield return null;

            // Halfway between extend njs and second njs event
            NJSEventsStats_InitialState();
        }
    }
}
