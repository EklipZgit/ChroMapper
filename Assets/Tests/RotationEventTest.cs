using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class RotationEventTest
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

        [Test]
        [TestCase(new[] { 15, 30, 60 })]
        [TestCase(new[] { 3, 2, 1 })]
        [TestCase(new[] { 0, 15, -10 })]
        public void RotationCallbackProperties(int[] rotations)
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(ObjectType.RotationEvent);

            var rotationEventA = new BaseRotationEvent { JsonTime = 1, Type = (int)EventTypeValue.LateLaneRotation, Rotation = rotations[0] };
            var rotationEventB = new BaseRotationEvent { JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, Rotation = rotations[1] };
            var rotationEventC = new BaseRotationEvent { JsonTime = 3, Type = (int)EventTypeValue.LateLaneRotation, Rotation = rotations[2] };
            eventsContainer.SpawnObject(rotationEventA);
            eventsContainer.SpawnObject(rotationEventB);
            eventsContainer.SpawnObject(rotationEventC);

            var laneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Rotations should add up
            atsc.MoveToJsonTime(0);
            Assert.AreEqual(0, laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(1.5f);
            Assert.AreEqual(rotations[0], laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(2.5f);
            Assert.AreEqual(rotations[0] + rotations[1], laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(3.5f);
            Assert.AreEqual(rotations[0] + rotations[1] + rotations[2], laneRotationProvider.PlaybackRotation);
        }

        [Test]
        public void RotationCallbackPropertiesOnTimeMatch()
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(ObjectType.RotationEvent);

            const int rotation = 15;
            const float timeA = 1f;
            const float timeB = 2f;
            var rotationEventA = new BaseRotationEvent { JsonTime = timeA, Type = (int)EventTypeValue.LateLaneRotation, Rotation = rotation };
            var rotationEventB = new BaseRotationEvent { JsonTime = timeB, Type = (int)EventTypeValue.LateLaneRotation, Rotation = rotation };
            eventsContainer.SpawnObject(rotationEventA);
            eventsContainer.SpawnObject(rotationEventB);

            var laneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Should ignore events on same time
            atsc.MoveToJsonTime(timeA);
            Assert.AreEqual(0, laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(timeB);
            Assert.AreEqual(rotation, laneRotationProvider.PlaybackRotation);
        }
    }
}