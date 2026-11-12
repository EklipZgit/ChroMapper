using System.Collections;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class DenseWallLoadingTest
    {
        private bool previousAnimations;
        private AudioTimeSyncController clock;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return TestUtils.LoadMap(3);
            previousAnimations = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
        }

        [UnityTest]
        public IEnumerator DenseEditWindowKeepsNearbyAndCrossingWalls()
        {
            var obstacles = new JSONArray();
            for (var index = 0; index < 4000; index++)
            {
                obstacles.Add(Wall(150f + index * 0.006f));
            }
            obstacles.Add(Wall(100f, 80f));
            yield return TestUtils.ReloadMap(3, new JSONObject
            {
                ["version"] = "3.2.0", ["obstacles"] = obstacles
            }, songLengthSeconds: 200);
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            clock.MoveToJsonTime(162f);
            yield return null;
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(walls.LoadedContainers.Count, Is.LessThanOrEqualTo(2200),
                "Dense wall maps should shorten the edit window instead of loading the entire chunk range.");
            foreach (var wall in walls.MapObjects)
            {
                if (wall.JsonTime >= 160.5f && wall.JsonTime <= 163f || wall.JsonTime == 100f)
                {
                    Assert.That(walls.LoadedContainers.ContainsKey(wall), Is.True,
                        "Nearby walls and walls crossing the window must remain editable.");
                }
            }
        }

        [UnityTest]
        public IEnumerator DenseEditPlaybackPreparesDistantWallsGradually()
        {
            var obstacles = new JSONArray();
            for (var index = 0; index < 4000; index++)
            {
                obstacles.Add(Wall(150f + index * 0.006f));
            }
            yield return TestUtils.ReloadMap(3, new JSONObject
            {
                ["version"] = "3.2.0", ["obstacles"] = obstacles
            }, songLengthSeconds: 200);
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            clock.MoveToJsonTime(162f);
            yield return null;
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            walls.RefreshPool(-10f, -5f, true);
            var distantSpawns = 0;
            void CountDistantSpawn(BaseObject wall)
            {
                if (wall.SongBpmTime > 163f)
                {
                    distantSpawns++;
                }
            }
            walls.OnContainerSpawned += CountDistantSpawn;
            try
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 162f);
                clock.enabled = false;
                walls.RefreshPool(false);
                Assert.That(distantSpawns, Is.GreaterThan(0));
                Assert.That(distantSpawns, Is.LessThanOrEqualTo(32),
                    "Preparing distant edit-mode walls should not grow the pool in one large burst.");
                foreach (var wall in walls.MapObjects)
                {
                    if (wall.JsonTime >= 161f && wall.JsonTime <= 163f)
                    {
                        Assert.That(walls.LoadedContainers.ContainsKey(wall), Is.True);
                    }
                }
            }
            finally
            {
                walls.OnContainerSpawned -= CountDistantSpawn;
            }
        }

        [UnityTest]
        public IEnumerator EarlyPlaybackPreloadsAreSpreadAcrossFramesButFinishBeforeSpawn()
        {
            var obstacles = new JSONArray();
            for (var index = 0; index < 200; index++)
            {
                obstacles.Add(Wall(20f));
            }
            yield return TestUtils.ReloadMap(3, new JSONObject
            {
                ["version"] = "3.2.0", ["obstacles"] = obstacles
            });
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(0f);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var spawn = walls.MapObjects[0].SpawnSongBpmTime;
            clock.MoveToSongBpmTime(spawn - 1f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, spawn - 1f);
            clock.enabled = false;
            Assert.That(walls.LoadedContainers.Count, Is.GreaterThan(0));
            Assert.That(walls.LoadedContainers.Count, Is.LessThanOrEqualTo(32),
                "The early preload window should not activate a whole stacked group in one frame.");
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, spawn);
            Assert.That(walls.LoadedContainers.Count, Is.EqualTo(200),
                "Preload throttling must never postpone walls beyond their actual spawn time.");
            yield break;
        }

        private static JSONObject Wall(float beat, float duration = 0.1f) => new()
        {
            ["b"] = beat, ["x"] = 0, ["y"] = 0, ["d"] = duration, ["w"] = 1, ["h"] = 1
        };

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
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
        }
    }
}
