using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class RotationEventTest : TestBase
    {
        [Test]
        [TestCase(new[] { 15, 30, 60 })]
        [TestCase(new[] { 3, 2, 1 })]
        [TestCase(new[] { 0, 15, -10 })]
        public void RotationCallbackProperties(int[] rotations)
        {
            var rotationEventA = new BaseRotationEvent
            {
                JsonTime = 1, Type = (int)EventTypeValue.LateRotationEventType, Rotation = rotations[0]
            };
            var rotationEventB = new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateRotationEventType, Rotation = rotations[1]
            };
            var rotationEventC = new BaseRotationEvent
            {
                JsonTime = 3, Type = (int)EventTypeValue.LateRotationEventType, Rotation = rotations[2]
            };

            rotationEventA = PlaceUtils.Place(rotationEventA);
            rotationEventB = PlaceUtils.Place(rotationEventB);
            rotationEventC = PlaceUtils.Place(rotationEventC);

            var laneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Rotations should add up
            atsc.MoveToJsonTime(0);
            Assert.AreEqual(0, (int)laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(1.5f);
            Assert.AreEqual(rotations[0], (int)laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(2.5f);
            Assert.AreEqual(rotations[0] + rotations[1], (int)laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(3.5f);
            Assert.AreEqual(rotations[0] + rotations[1] + rotations[2], (int)laneRotationProvider.PlaybackRotation);
        }

        [Test]
        public void RotationCallbackPropertiesOnTimeMatch()
        {
            const int rotation = 15;
            const float timeA = 1f;
            const float timeB = 2f;
            var rotationEventA = new BaseRotationEvent
            {
                JsonTime = timeA, Type = (int)EventTypeValue.LateRotationEventType, Rotation = rotation
            };
            var rotationEventB = new BaseRotationEvent
            {
                JsonTime = timeB, Type = (int)EventTypeValue.LateRotationEventType, Rotation = rotation
            };

            rotationEventA = PlaceUtils.Place(rotationEventA);
            rotationEventB = PlaceUtils.Place(rotationEventB);

            var laneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Should ignore events on same time
            atsc.MoveToJsonTime(timeA);
            Assert.AreEqual(0, (int)laneRotationProvider.PlaybackRotation);

            atsc.MoveToJsonTime(timeB);
            Assert.AreEqual(rotation, (int)laneRotationProvider.PlaybackRotation);
        }
    }
}
