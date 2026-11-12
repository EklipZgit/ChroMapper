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
    public class MegamixRunwayParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChroma;
        private bool previousClockEnabled;
        private float previousFov;
        private float previousOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChroma = Settings.Instance.EmulateChromaLite;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Requires the local original Megamix Lawless difficulty.")]
        public IEnumerator FullMapRunwayAndPillarLightsUseNativeAxisRotation()
        {
            yield return InspectMap(PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix", "FullMap.json"), "full");
        }

        [UnityTest]
        public IEnumerator RunwayAndPillarLightsUseNativeAxisRotation()
        {
            yield return InspectMap(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "MegamixRunwayFixture.json"), "reduced");
        }

        [UnityTest]
        [Explicit("Requires the local original Megamix Lawless difficulty.")]
        public IEnumerator FullMapColumnsReturnToAuthoredPoseAtBeat112()
        {
            yield return InspectMap(PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix", "FullMap.json"), "full-columns", true);
        }

        [UnityTest]
        [Explicit("Requires a local frozen column export and its snapshot manifest.")]
        public IEnumerator FrozenColumnSnapshotPreservesRenderedTransforms()
        {
            var path = PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix", "UprightSnapshotControl.json");
            var manifest = JSON.Parse(File.ReadAllText(Path.ChangeExtension(path, ".manifest.json")));
            var expected = JSON.Parse(File.ReadAllText(manifest["snapshot"].Value)).AsArray
                .Children.ToDictionary(row => row["track"].Value);
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)), beatsPerMinute: 160f,
                environmentName: "BigMirrorEnvironment", songLengthSeconds: 120, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(112f);
            yield return null;
            yield return null;
            var columns = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Where(item => item.EnvironmentEnhancement.Geometry != null).ToArray();
            Assert.That(columns.Length, Is.EqualTo(expected.Count));
            foreach (var column in columns)
            {
                var matrix = column.MpbController.Renderers.Single().localToWorldMatrix;
                var recorded = expected[column.EnvironmentEnhancement.Track]["worldMatrix"];
                for (var index = 0; index < 16; index++)
                {
                    Assert.That(matrix[index], Is.EqualTo(recorded[index].AsFloat).Within(0.02f),
                        column.EnvironmentEnhancement.Track + " exported matrix element " + index);
                }
            }
        }

        private IEnumerator InspectMap(string path, string label, bool checkColumns = false)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 18f,
                NoteStartBeatOffset = -0.75f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)), beatsPerMinute: 160f,
                environmentName: "BigMirrorEnvironment", songLengthSeconds: label.StartsWith("full") ? 2400 : 120, forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(checkColumns ? 112f : 163.109f);
            yield return null;
            yield return null;
            var camera = cameras.CameraControllers[1].Camera;
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + label);
            Directory.CreateDirectory(directory);
            var pixels = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "actual.png"));
            File.WriteAllText(PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix", label + "-capture-path.txt"), directory);
            if (checkColumns)
            {
                var columns = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                    .Where(item => item.EnvironmentEnhancement.Geometry != null
                        && item.EnvironmentEnhancement.Track.StartsWith("eA")).ToArray();
                Assert.That(columns.Length, Is.GreaterThan(1000));
                foreach (var beat in new[] { 112f, 196.75f, 112f, 264f, 112f, 112.015625f, 111.984375f, 163.109f })
                {
                    clock.MoveToJsonTime(beat);
                    if (beat <= 164f)
                    {
                        DumpGeometrySnapshot(columns, PathUtils.Combine(directory, "pose-" + beat + "-immediate.json"));
                        AssertColumnPoses(columns, beat, "immediate");
                    }
                    yield return null;
                    yield return null;
                    if (beat > 164f)
                    {
                        continue;
                    }
                    LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "columns-" + beat + ".png"));
                    AssertColumnPoses(columns, beat, "settled");
                }
                foreach (var animations in new[] { true, false, true })
                {
                    Settings.Instance.Animations = animations;
                    mode.SetUIMode(UIModeType.Normal, false);
                    cameras.SelectCamera(CameraType.Editing);
                    clock.MoveToJsonTime(112f);
                    yield return null;
                    mode.SetUIMode(UIModeType.Preview, false);
                    cameras.SelectCamera(CameraType.Editing);
                    yield return null;
                    mode.SetUIMode(UIModeType.Playing, false);
                    cameras.SelectCamera(CameraType.Playing);
                    clock.MoveToJsonTime(112f);
                    yield return null;
                    LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "columns-mode-" + animations + ".png"));
                    DumpGeometrySnapshot(columns, PathUtils.Combine(directory, "pose-mode-" + animations + ".json"));
                    AssertColumnPoses(columns, 112f, "mode transition");
                }
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 111.9f);
                var secondsProperty = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                foreach (var beat in new[] { 111.95f, 112f, 112.015625f, 113f, 196.75f })
                {
                    secondsProperty.SetValue(clock, clock.GetSecondsFromBeat(beat));
                    yield return null;
                    yield return null;
                    if (beat > 164f)
                    {
                        continue;
                    }
                    DumpGeometrySnapshot(columns, PathUtils.Combine(directory, "pose-playing-" + beat + ".json"));
                    LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "columns-playing-" + beat + ".png"));
                    AssertColumnPoses(columns, beat, "playback");
                }
                TestUtils.PauseDeterministicPlayback(clock);
                clock.MoveToJsonTime(112f);
                yield return null;
                yield return null;
                DumpGeometrySnapshot(columns, PathUtils.Combine(directory, "pose-paused-rewind.json"));
                LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "columns-paused-rewind.png"));
                AssertColumnPoses(columns, 112f, "paused rewind after playback");
                yield break;
            }
            if (label == "reduced")
            {
                // The retained floor fan covers this lower-left region. The Euler control leaves it dim blue.
                var blue = 0f;
                for (var y = 20; y < 290; y++)
                {
                    for (var x = 0; x < 480; x++)
                    {
                        blue += pixels[y * 1280 + x].b;
                    }
                }
                Assert.That(blue / (480 * 270), Is.GreaterThan(0.12f),
                    "The runway-side floor must receive the native side-light illumination.");
            }
            var visuals = Object.FindObjectsByType<LightRotation>(FindObjectsSortMode.None)
                .Where(visual => visual.name == "LightWrapper0" || visual.name == "LightWrapper1").ToArray();
            Assert.That(visuals.Length, Is.EqualTo(2), "Both oblique side-light wrappers must be present.");
            foreach (var beat in new[] { 163.109f, 163.125f, 163.09375f, 112f, 163.109f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                foreach (var visual in visuals)
                {
                    var direction = visual.name == "LightWrapper0" ? 1f : -1f;
                    var seconds = clock.GetSecondsFromBeat(beat);
                    var callbackSeconds = TimeHelper.GetPreviewCallbackSeconds(clock.GetSecondsFromBeat(112f));
                    var angle = (seconds - callbackSeconds) * 200f * direction;
                    var reference = new GameObject("Native side-light rotation reference");
                    Quaternion expected;
                    try
                    {
                        reference.transform.localRotation = visual.StartRotation;
                        reference.transform.Rotate(visual.RotationVector, angle, Space.Self);
                        expected = reference.transform.localRotation;
                    }
                    finally
                    {
                        Object.DestroyImmediate(reference);
                    }
                    Assert.That(Quaternion.Angle(visual.Transform.localRotation, expected), Is.LessThan(0.05f),
                        visual.name + " must rotate about its native oblique axis rather than applying three Euler rotations.");
                }
            }
        }

        private static void AssertColumnPoses(GeometryContainer[] columns, float beat, string phase)
        {
            foreach (var column in columns)
            {
                var authored = column.EnvironmentEnhancement;
                var expected = Matrix4x4.TRS(authored.Position.Value,
                    Quaternion.Euler(authored.Rotation.Value), authored.Scale.Value);
                var actual = column.MpbController.Renderers.Single().localToWorldMatrix;
                var error = 0f;
                for (var index = 0; index < 16; index++)
                {
                    error = Mathf.Max(error, Mathf.Abs(expected[index] - actual[index]));
                }
                Assert.That(error, Is.LessThan(0.02f), authored.Track + " must restore its authored column pose at beat " + beat + " (" + phase + ")");
            }
        }

        private static void DumpGeometrySnapshot(GeometryContainer[] columns, string path)
        {
            var rows = new JSONArray();
            foreach (var column in columns)
            {
                var renderer = column.MpbController.Renderers.Single();
                var matrix = new JSONArray();
                for (var index = 0; index < 16; index++)
                {
                    matrix.Add(renderer.localToWorldMatrix[index]);
                }
                var hierarchy = new JSONArray();
                for (var transform = renderer.transform; transform != null; transform = transform.parent)
                {
                    hierarchy.Add(new JSONObject
                    {
                        ["name"] = transform.name,
                        ["position"] = Vector(transform.localPosition),
                        ["rotation"] = Vector(transform.localEulerAngles),
                        ["scale"] = Vector(transform.localScale)
                    });
                }
                var color = column.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
                rows.Add(new JSONObject
                {
                    ["track"] = column.EnvironmentEnhancement.Track,
                    ["geometry"] = JSON.Parse(column.EnvironmentEnhancement.Geometry.ToString()),
                    ["worldMatrix"] = matrix,
                    ["color"] = new JSONArray { [0] = color.r, [1] = color.g, [2] = color.b, [3] = color.a },
                    ["hierarchy"] = hierarchy
                });
            }
            File.WriteAllText(path, rows.ToString());
        }

        [UnityEditor.MenuItem("Tools/Chroma/Export Megamix columns at current pose")]
        private static void DumpCurrentColumnPose()
        {
            var columns = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Where(item => item.EnvironmentEnhancement.Geometry != null
                    && item.EnvironmentEnhancement.Track != null
                    && item.EnvironmentEnhancement.Track.StartsWith("eA")).ToArray();
            if (columns.Length == 0)
            {
                Debug.LogWarning("No loaded Megamix eA column geometry to export.");
                return;
            }
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-interactive-snapshot");
            Directory.CreateDirectory(directory);
            DumpGeometrySnapshot(columns, PathUtils.Combine(directory, "columns.json"));
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            LinearRenderCapture.Capture(cameraManager.SelectedCameraController.Camera, PathUtils.Combine(directory, "actual.png"));
            Debug.Log("Saved current column matrices and render to " + directory);
        }

        private static JSONArray Vector(Vector3 vector)
        {
            return new JSONArray { [0] = vector.x, [1] = vector.y, [2] = vector.z };
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
            }
            if (cameras != null)
            {
                cameras.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
            {
                if (clock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(clock);
                }
                clock.enabled = previousClockEnabled;
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChroma;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
