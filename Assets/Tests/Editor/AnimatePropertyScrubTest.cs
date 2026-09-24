using System;
using System.Collections.Generic;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    // AnimatePropertyScrubTest constrains animation evaluation as a pure function of the requested beat, so seeking
    // backward, restarting at beat zero, and replaying a previously visited beat cannot inherit a later transform.
    public class AnimatePropertyScrubTest
    {
        private static readonly float[] SampleTimes = { 0f, 2f, 4f, 5f, 7.5f, 8f, 10f };

        // ScrubbingBeforeFirstEventRestoresEveryPropertyDefault reproduces the stale-state leak for all values used by
        // track/object animation: a later evaluation must not leave position, rotation, scale, color, or float state set.
        [Test]
        public void ScrubbingBeforeFirstEventRestoresEveryPropertyDefault()
        {
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseVector3,
                new Vector3(11f, 12f, 13f),
                "[[20,30,40,0],[50,60,70,1]]",
                (expected, actual) => Vector3.Distance(expected, actual) < 0.0001f,
                "position");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseQuaternion,
                Quaternion.Euler(11f, 12f, 13f),
                "[[20,30,40,0],[50,60,70,1]]",
                (expected, actual) => Quaternion.Angle(expected, actual) < 0.001f,
                "rotation");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseVector3,
                new Vector3(2f, 3f, 4f),
                "[[5,6,7,0],[8,9,10,1]]",
                (expected, actual) => Vector3.Distance(expected, actual) < 0.0001f,
                "scale");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseColor,
                new Color(0.1f, 0.2f, 0.3f, 0.4f),
                "[[0.5,0.6,0.7,0.8,0],[0.9,1,0.8,0.7,1]]",
                (expected, actual) => Vector4.Distance(expected, actual) < 0.0001f,
                "color");
            AssertReturnsDefaultBeforeFirstEvent(
                PointDataParsers.ParseFloat,
                0.25f,
                "[[0.75,0],[1,1]]",
                (expected, actual) => Mathf.Abs(expected - actual) < 0.0001f,
                "float");
        }

        // ScrubbingInArbitraryOrderMatchesForwardPlayback constrains every sampled beat to the value obtained by a
        // fresh monotonic pass, including repeated forward/backward seeks across two distinct AnimateTrack events.
        [Test]
        public void ScrubbingInArbitraryOrderMatchesForwardPlayback()
        {
            var forward = CreatePositionProperty();
            var expected = new Dictionary<float, Vector3>();
            foreach (var sampleTime in SampleTimes)
            {
                expected[sampleTime] = forward.GetLerpedValue(sampleTime);
            }

            var scrubbed = CreatePositionProperty();
            var scrubOrder = new[] { 10f, 0f, 7.5f, 2f, 8f, 4f, 5f, 10f, 0f, 5f };
            foreach (var sampleTime in scrubOrder)
            {
                var actual = scrubbed.GetLerpedValue(sampleTime);
                Assert.That(
                    Vector3.Distance(actual, expected[sampleTime]),
                    Is.LessThan(0.0001f),
                    $"Seeking to beat {sampleTime} depended on the previously evaluated beat.");
            }
        }

        // Use two non-contiguous events so the arbitrary-order test covers interpolation, post-event holds, and
        // selection of the correct prior event rather than only the before-first-event reset edge case.
        private static AnimateProperty<Vector3> CreatePositionProperty()
        {
            var property = new AnimateProperty<Vector3>(
                new List<PointDefinition<Vector3>>(),
                _ => { },
                new Vector3(1f, 2f, 3f));
            AddEvent(property, PointDataParsers.ParseVector3, 4f, 2f, "[[10,20,30,0],[20,30,40,1]]");
            AddEvent(property, PointDataParsers.ParseVector3, 8f, 2f, "[[50,60,70,0],[80,90,100,1]]");
            property.Sort();
            return property;
        }

        // Evaluating the later point first mirrors the reported scrub sequence; UpdateProperty exercises the same
        // setter path used by TrackAnimator and ObjectAnimator before checking that beat zero restores the default.
        private static void AssertReturnsDefaultBeforeFirstEvent<T>(
            PointDefinition<T>.Parser parser,
            T defaultValue,
            string points,
            Func<T, T, bool> equals,
            string propertyName)
            where T : struct
        {
            var actual = defaultValue;
            var property = new AnimateProperty<T>(
                new List<PointDefinition<T>>(),
                value => actual = value,
                defaultValue);
            AddEvent(property, parser, 4f, 2f, points);
            property.Sort();

            property.UpdateProperty(5f);
            Assert.That(equals(defaultValue, actual), Is.False, $"The {propertyName} fixture did not leave its default.");
            property.UpdateProperty(0f);
            Assert.That(
                equals(defaultValue, actual),
                Is.True,
                $"Seeking before the first {propertyName} event retained a later animated value.");
        }

        // Construct production point definitions through the same JSON parser and timing fields used by AnimateTrack.
        private static void AddEvent<T>(
            AnimateProperty<T> property,
            PointDefinition<T>.Parser parser,
            float startTime,
            float duration,
            string points)
            where T : struct
        {
            property.AddPointDef(
                parser,
                new IPointDefinition.UntypedParams
                {
                    Points = JSON.Parse(points),
                    Time = startTime,
                    Duration = duration,
                    TimeBegin = startTime,
                    TimeEnd = startTime + duration
                },
                null);
        }
    }
}
