using System.Collections;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class DenseCustomEventLoadingTest
    {
        private AudioTimeSyncController clock;
        private bool previousAnimations;

        [UnityTest]
        public IEnumerator DenseEditWindowKeepsNearbyNodesAndTheCompleteAnimationTimeline()
        {
            yield return TestUtils.LoadMap(3);
            previousAnimations = Settings.Instance.Animations;
            var events = new JSONArray();
            for (var index = 0; index < 4000; index++)
            {
                events.Add(new JSONObject
                {
                    ["b"] = 150f + index * 0.006f,
                    ["t"] = "AnimateTrack",
                    ["d"] = new JSONObject
                    {
                        ["track"] = "dense-events",
                        ["position"] = JSON.Parse($"[[{index},0,0,0]]")
                    }
                });
            }
            yield return TestUtils.ReloadMap(3, new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject { ["customEvents"] = events }
            }, songLengthSeconds: 200);
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            clock.MoveToJsonTime(162f);
            yield return null;
            var collection = Object.FindAnyObjectByType<CustomEventGridContainer>();
            Assert.That(collection.LoadedContainers.Count, Is.LessThanOrEqualTo(2200),
                "Dense AnimateTrack sections should keep nearby edit nodes instead of the full chunk range.");
            foreach (var ev in collection.MapObjects)
            {
                if (ev.JsonTime >= 161.25f && ev.JsonTime <= 162.75f)
                {
                    Assert.That(collection.LoadedContainers.ContainsKey(ev), Is.True);
                }
            }
            Assert.That(collection.MapObjects.Count, Is.EqualTo(4000));
            var manager = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(manager.TryGetAnimationTrack("dense-events", out var track), Is.True);
            var position = (AnimateProperty<Vector3>)track.AnimatedProperties["position"];
            Assert.That(position.PointDefinitions.Count, Is.EqualTo(4000),
                "Limiting visible nodes must not shorten track animation timelines.");
            Assert.That(position.GetLerpedValue(collection.MapObjects[3999].JsonTime).x, Is.EqualTo(3999f),
                "Events outside the visible edit window must still contribute their animation values.");

            collection.RefreshPool(0f, 1000f, true);
            Assert.That(collection.LoadedContainers.Count, Is.EqualTo(4000),
                "Explicit range queries must retain their requested bounds.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(clock);
                }
                clock.enabled = true;
            }
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
        }
    }
}
