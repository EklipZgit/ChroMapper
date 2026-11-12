using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChangeOfSceneDockFogParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousFov;
        private float previousOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Requires the local Change of scene map.")]
        public IEnumerator FullMapUsesNativeFogAndTubeIntensityAtBeat122()
        {
            yield return CheckDock(
                @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\Chroma\3f19a (Change of scene - Loopymtb)\ExpertPlusStandard.dat");
        }

        [UnityTest]
        [Explicit("Replays native capture 20261019T191623525 using the local Change of scene map.")]
        public IEnumerator FullMapReplaysNativeDockSnapshot()
        {
            yield return CheckDock(
                @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\Chroma\3f19a (Change of scene - Loopymtb)\ExpertPlusStandard.dat",
                114.765625f, 100f, new Vector3(0f, 2.30000019f, 0f));
        }

        [UnityTest]
        public IEnumerator ReducedDockUsesNativeFogAndTubeIntensity()
        {
            yield return CheckDock(PathUtils.Combine(
                Application.dataPath, "Tests", "Fixtures", "ChangeOfSceneDockFogFixture.json"));
        }

        [UnityTest]
        public IEnumerator SunTrackKeepsNativeLowerBloomBandAfterInstantTransform()
        {
            yield return LoadSunFixture(ReadSunFixture());
            var time = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var tubes = BloomFogObject.AllBloomFogLights
                .Where(value => value.transform.parent != null && value.transform.parent.name == "Sun").ToArray();
            Assert.That(tubes.Length, Is.EqualTo(4));
            // Native capture 20261019T191623525 records three tubes below the horizon and one at its centre.
            // The fixture retains only the shared parent/child track and its two source events.
            foreach (var beat in new[] { 122f, 106f, 122f, 79.25f, 107f, 114.765625f, 122f })
            {
                time.MoveToJsonTime(beat);
                for (var frame = 0; frame < 2; frame++)
                {
                    foreach (var tube in tubes)
                    {
                        var expected = beat < 107f ? tube.transform.parent.position
                            : tube.name == "NeonTube (2)"
                                ? new Vector3(0f, 0f, 464.9172f)
                                : new Vector3(332.2705f, -419.1781f, 464.9172f);
                        Assert.That(Vector3.Distance(tube.transform.position, expected), Is.LessThan(0.01f),
                            $"{tube.name} at beat {beat}, frame {frame}");
                    }
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator SharedSunTrackRetainsNativeWorldRotationAtInstantCuts()
        {
            var difficulty = ReadSunFixture();
            difficulty["_customData"]["_customEvents"][1]["_data"]["_rotation"] = JSON.Parse("[0,0,97]");
            yield return LoadSunFixture(difficulty);
            var time = Object.FindAnyObjectByType<AudioTimeSyncController>();
            time.MoveToJsonTime(122f);
            var beforeCut = Quaternion.Euler(0f, 0f, 270f);
            var afterCut = Quaternion.Euler(0f, 0f, 97f);
            foreach (var tube in BloomFogObject.AllBloomFogLights
                .Where(value => value.transform.parent != null && value.transform.parent.name == "Sun"))
            {
                // Native TransformController writes the child's world rotation before the parent changes its rotation.
                var expected = tube.name == "NeonTube (2)" ? afterCut
                    : afterCut * Quaternion.Inverse(beforeCut) * afterCut;
                Assert.That(Quaternion.Angle(tube.transform.rotation, expected), Is.LessThan(0.05f), tube.name);
            }
        }

        [UnityTest]
        public IEnumerator SunTrackKeepsNativeBloomBandsDuringPlaybackWithoutNativeAudio()
        {
            yield return LoadSunFixture(ReadSunFixture());
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 106.5f);
            var tubes = BloomFogObject.AllBloomFogLights
                .Where(value => value.transform.parent != null && value.transform.parent.name == "Sun").ToArray();
            Assert.That(tubes.Length, Is.EqualTo(4));
            foreach (var beat in new[] { 106.5f, 106.984375f, 107f, 107.015625f, 114.765625f, 122f })
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                yield return null;
                yield return null;
                Assert.That(clock.CurrentJsonTime, Is.EqualTo(beat).Within(0.0001f));
                Assert.That(clock.SongAudioSource.isPlaying, Is.False);
                foreach (var tube in tubes)
                {
                    var expected = beat < 107f ? tube.transform.parent.position
                        : tube.name == "NeonTube (2)"
                            ? new Vector3(0f, 0f, 464.9172f)
                            : new Vector3(332.2705f, -419.1781f, 464.9172f);
                    Assert.That(Vector3.Distance(tube.transform.position, expected), Is.LessThan(0.01f),
                        $"{tube.name} during playback at beat {beat}");
                }
            }
        }

        [UnityTest]
        public IEnumerator LocalPositionRequestsKeepTheirAuthoredParentSpace()
        {
            var difficulty = ReadSunFixture();
            foreach (var entry in difficulty["_customData"]["_customEvents"].AsArray.Values)
            {
                var data = entry["_data"];
                data["_localPosition"] = data["_position"];
                data.Remove("_position");
            }
            yield return LoadSunFixture(difficulty);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(122f);
            foreach (var tube in BloomFogObject.AllBloomFogLights
                .Where(value => value.transform.parent != null && value.transform.parent.name == "Sun"))
            {
                Assert.That(Vector3.Distance(tube.transform.localPosition, new Vector3(0f, 0f, 464.9172f)),
                    Is.LessThan(0.01f), tube.name);
            }
        }

        [UnityTest]
        public IEnumerator SharedTrackKeepsExistingWorldWritesForDeeperEnvironmentChildren()
        {
            yield return LoadSunFixture(ReadSunFixture());
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(122f);
            var markers = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers;
            var nested = markers.Where(value => value.name == "BakedBloom" && value.ChromaID.Contains("]Sun.")).ToArray();
            Assert.That(nested.Length, Is.EqualTo(4));
            foreach (var marker in nested)
            {
                Assert.That(Vector3.Distance(marker.transform.position, new Vector3(0f, 0f, 464.9172f)),
                    Is.LessThan(0.01f), marker.ChromaID);
            }
        }

        [UnityTest]
        public IEnumerator LaterColorWriteReappliesTheSharedWorldPose()
        {
            var difficulty = ReadSunFixture();
            difficulty["_customData"]["_customEvents"].AsArray.Add(JSON.Parse(
                "{\"_time\":110,\"_type\":\"AnimateTrack\",\"_data\":{\"_track\":\"sun\",\"_color\":[1,1,1,1]}}"));
            yield return LoadSunFixture(difficulty);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(122f);
            foreach (var tube in BloomFogObject.AllBloomFogLights
                .Where(value => value.transform.parent != null && value.transform.parent.name == "Sun"))
            {
                Assert.That(Vector3.Distance(tube.transform.position, tube.transform.parent.position),
                    Is.LessThan(0.01f), tube.name);
            }
        }

        [UnityTest]
        public IEnumerator IdenticalConstantRepeatDoesNotReapplyTheHeldWorldPoseOnSeek()
        {
            var difficulty = ReadSunFixture();
            difficulty["_customData"]["_customEvents"].AsArray.Add(JSON.Parse(
                "{\"_time\":110,\"_type\":\"AnimateTrack\",\"_data\":{\"_track\":\"sun\",\"_position\":[0,0,774.862],\"_scale\":[50,50,0]}}"));
            yield return LoadSunFixture(difficulty);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(122f);
            var tube = BloomFogObject.AllBloomFogLights.Single(value => value.name == "NeonTube (3)"
                && value.transform.parent != null && value.transform.parent.name == "Sun");
            Assert.That(Vector3.Distance(tube.transform.position, new Vector3(332.2705f, -419.1781f, 464.9172f)),
                Is.LessThan(0.01f));
        }

        private static JSONNode ReadSunFixture() => JSON.Parse(File.ReadAllText(PathUtils.Combine(
            Application.dataPath, "Tests", "Fixtures", "ChangeOfSceneSunTrackFixture.json")));

        private static IEnumerator LoadSunFixture(JSONNode difficulty)
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                2,
                difficulty,
                beatsPerMinute: 145.03f,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 100,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    CustomRequirements = new List<string> { "Noodle Extensions" },
                    CustomSuggestions = new List<string> { "Chroma" }
                });
        }

        private IEnumerator CheckDock(string path, float beat = 122.046875f, float fov = 90f, Vector3? position = null)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = fov;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(path)),
                editorState: new JSONObject
                {
                    ["components"] = new JSONObject
                    {
                        ["currentJsonTime"] = new JSONObject { ["value"] = beat, ["gridMeasureSnapping"] = 64 }
                    }
                },
                beatsPerMinute: 145.03f,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 100,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 16f, NoteStartBeatOffset = -0.791f,
                    CustomRequirements = new List<string> { "Noodle Extensions" },
                    CustomSuggestions = new List<string> { "Chroma" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            cameras.SelectedCameraController.Camera.fieldOfView = fov;
            yield return null;
            yield return null;

            Assert.That(clock.CurrentJsonTime, Is.EqualTo(beat).Within(0.001f));
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "ChangeOfSceneDock",
                System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" +
                (Path.GetExtension(path) == ".dat" ? "full-map" : "reduced-dock") +
                (position.HasValue ? "-native-reference" : ""));
            Directory.CreateDirectory(directory);
            var camera = cameras.SelectedCameraController.Camera;
            if (position.HasValue)
            {
                camera.transform.SetPositionAndRotation(position.Value, Quaternion.identity);
            }
            File.WriteAllText(PathUtils.Combine(directory, "capture.json"), new JSONObject
            {
                ["sourcePath"] = path,
                ["beat"] = clock.CurrentJsonTime,
                ["verticalFov"] = camera.fieldOfView,
                ["cameraPosition"] = camera.transform.position.ToString(),
                ["cameraRotation"] = camera.transform.eulerAngles.ToString()
            }.ToString(2));
            var pixels = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "actual.png"));
            if (position.HasValue)
            {
                // This unobstructed foreground region comes from native capture 20261019T191623525.
                var mean = Vector3.zero;
                for (var y = 10; y < 100; y++)
                {
                    for (var x = 350; x < 850; x++)
                    {
                        var display = pixels[y * 1280 + x].gamma;
                        mean += new Vector3(display.r, display.g, display.b);
                    }
                }
                mean *= 255f / (90f * 500f);
                File.WriteAllText(PathUtils.Combine(directory, "native-reference-metrics.json"), new JSONObject
                {
                    ["nativeCapture"] = "20261019T191623525",
                    ["displayRgbMean"] = mean.ToString("F4"),
                    ["nativeDisplayRgbMean"] = "(44.179, 46.459, 61.671)"
                }.ToString(2));
                Assert.That(Vector3.Distance(mean, new Vector3(44.179f, 46.459f, 61.671f)), Is.LessThan(5f),
                    "The near-foot bloom band must match the native foreground brightness.");
            }
            var parameters = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams;
            Assert.That(parameters.AutoExposureLimit, Is.EqualTo(1000f));
            Assert.That(parameters.LegacyAutoExposure, Is.False);
            var sunLights = BloomFogObject.AllBloomFogLights
                .Where(value => value.transform.parent != null && value.transform.parent.name == "Sun").ToArray();
            Assert.That(sunLights.Length, Is.EqualTo(4));
            foreach (var light in sunLights)
            {
                var controller = light.GetComponent<ParametricBloomFogLightController>();
                var alpha = controller.Color.a * light.IntensityMultiplier;
                var expectedAlpha = alpha <= 0.0031308f
                    ? System.Math.Max(0, alpha * 12.92)
                    : 1.055 * System.Math.Pow(alpha, 1.0 / 2.4) - 0.055;
                var count = 0;
                var quads = new BloomfogQuad[1];
                light.ApplyToQuad(
                    ref count,
                    quads,
                    camera.worldToCameraMatrix,
                    camera.projectionMatrix,
                    0.02f);
                Assert.That(count, Is.EqualTo(1));
                Assert.That(quads[0].Vertex0Color.a, Is.EqualTo(expectedAlpha).Within(0.00002), light.name);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(clock);
                }
                clock.enabled = previousClockEnabled;
            }

            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameras != null)
            {
                cameras.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

