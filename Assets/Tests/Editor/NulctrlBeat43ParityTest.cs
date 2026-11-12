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
    public class NulctrlBeat43ParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChromaLite;
        private bool previousChromaAdvanced;
        private float previousFov;
        private float previousOffset;
        private bool previousClockEnabled;
        private bool previousAccurateSize;
        private UIMode mode;
        private CameraManager cameras;
        private AudioTimeSyncController clock;
        private JSONNode expectations;
        private static string FixtureRoot => PathUtils.Combine(Application.dataPath, "Tests", "Fixtures");

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            previousChromaAdvanced = Settings.Instance.EmulateChromaAdvanced;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            previousAccurateSize = Settings.Instance.AccurateNoteSize;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator CapturedNotePositions([Values(0, 1)] int capture)
        {
            yield return Load(capture);
            var notes = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values.OfType<NoteContainer>().ToArray();
            var failures = new List<string>();
            foreach (JSONNode expected in expectations[capture]["notes"].AsArray)
            {
                var note = notes.SingleOrDefault(candidate => candidate.NoteData.JsonTime == expected["beat"].AsFloat
                    && candidate.NoteData.CustomTrack?.Value == expected["track"].Value);
                if (note == null)
                {
                    failures.Add($"Missing {expected["track"]} at {expected["beat"]}");
                    continue;
                }
                var error = Vector3.Distance(note.Animator.LocalTarget.position, Vector(expected["position"]));
                // Native initialization samples jump gravity on its spawn frame, leaving millimetre differences between runs.
                if (error > 0.005f)
                    failures.Add($"{expected["track"]} beat {expected["beat"]}: expected {Vector(expected["position"]):F5}, actual {note.Animator.LocalTarget.position:F5}, error {error:F6}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator CapturedFrameAlignsStretchedDotsAndPrismColors(
            [Values(0, 1)] int capture, [Values(false, true)] bool accurateEditorSize)
        {
            Settings.Instance.AccurateNoteSize = accurateEditorSize;
            yield return Load(capture);
            var camera = cameras.SelectedCameraController.Camera;
            var notes = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values.OfType<NoteContainer>().ToArray();
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values.OfType<ObstacleContainer>().ToArray();
            var expectedNotes = expectations[capture]["notes"].AsArray;
            var expectedPrisms = expectations[capture]["prisms"].AsArray;
            var matrices = new Matrix4x4[expectedNotes.Count];
            var colors = new Color[expectedPrisms.Count];
            var rendered = false;
            var renderObject = new GameObject("NULCTRL render after animation");
            renderObject.AddComponent<CameraRenderAfterLateUpdate>().TargetCamera = camera;
            void Capture(Camera renderedCamera)
            {
                if (renderedCamera != camera)
                    return;
                for (var i = 0; i < matrices.Length; i++)
                {
                    var expected = expectedNotes[i];
                    var note = notes.Single(candidate => candidate.NoteData.JsonTime == expected["beat"].AsFloat
                        && candidate.NoteData.CustomTrack?.Value == expected["track"].Value);
                    matrices[i] = note.DotModelController.Renderers.Single().transform.localToWorldMatrix;
                }
                for (var i = 0; i < colors.Length; i++)
                    colors[i] = walls.Single(wall => wall.ObstacleData.CustomTrack?.Value == expectedPrisms[i]["track"].Value)
                        .MpbController.Mpb.GetColor("_Color");
                rendered = true;
            }
            Camera.onPostRender += Capture;
            try
            {
                for (var frame = 0; frame < 5 && !rendered; frame++)
                    yield return null;
                Assert.That(rendered, Is.True, "The comparison requires a normal camera render after LateUpdate.");
                var maximumPixelError = 0f;
                for (var index = 0; index < matrices.Length; index++)
                {
                    var expected = expectedNotes[index];
                    var note = notes.Single(candidate => candidate.NoteData.JsonTime == expected["beat"].AsFloat
                        && candidate.NoteData.CustomTrack?.Value == expected["track"].Value);
                    var middle = expected["middleRotation"];
                    var nativeMiddle = new Quaternion(middle[0].AsFloat, middle[1].AsFloat, middle[2].AsFloat, middle[3].AsFloat);
                    Assert.That(Quaternion.Angle(Quaternion.Euler(note.DirectionTargetEuler + note.JumpRotationOffset), nativeMiddle),
                        Is.LessThan(0.05f), "Native spawn-rotation inputs must match before comparing the stretched body.");
                    var native = new Matrix4x4();
                    for (var row = 0; row < 4; row++)
                        for (var column = 0; column < 4; column++)
                            native[row, column] = expected["dotMatrix"][row][column].AsFloat;
                    // The center and both ends of the native unit quad include its face offset, tilt, and stretch.
                    foreach (var x in new[] { -0.5f, 0f, 0.5f })
                    {
                        var point = new Vector3(x, 0f, 0f);
                        var actualPoint = camera.WorldToViewportPoint(matrices[index].MultiplyPoint3x4(point));
                        var nativePoint = camera.WorldToViewportPoint(native.MultiplyPoint3x4(point));
                        var error = new Vector2((actualPoint.x - nativePoint.x) * 2560f,
                            (actualPoint.y - nativePoint.y) * 1440f).magnitude;
                        maximumPixelError = Mathf.Max(maximumPixelError, error);
                        Assert.That(error, Is.LessThan(1f),
                            $"{expected["track"]} at {expected["beat"]}: dot x={x} differs from native by {error:F3} pixels.");
                    }
                }
                for (var index = 0; index < colors.Length; index++)
                {
                    var expected = expectedPrisms[index]["color"];
                    Assert.That(Vector4.Distance(colors[index],
                        new Vector4(expected[0].AsFloat, expected[1].AsFloat, expected[2].AsFloat, expected[3].AsFloat)),
                        Is.LessThan(0.001f), $"Prism {expectedPrisms[index]["track"]} must match in the same rendered frame as the notes.");
                }
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "NulctrlFrame");
                Directory.CreateDirectory(directory);
                File.WriteAllText(PathUtils.Combine(directory, $"alignment-{capture}-editor-size-{accurateEditorSize}.json"),
                    new JSONObject
                    {
                        ["songSeconds"] = clock.CurrentSeconds,
                        ["previousSongSeconds"] = clock.PreviousFrameSeconds,
                        ["noteCount"] = expectedNotes.Count,
                        ["maximumPixelError"] = maximumPixelError,
                        ["prismCount"] = expectedPrisms.Count
                    }.ToString(2));
            }
            finally
            {
                Camera.onPostRender -= Capture;
                Object.DestroyImmediate(renderObject);
            }
        }

        [UnityTest]
        public IEnumerator CapturedPrismColors([Values(0, 1)] int capture)
        {
            yield return Load(capture);
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values.OfType<ObstacleContainer>().ToArray();
            var failures = new List<string>();
            foreach (JSONNode expected in expectations[capture]["prisms"].AsArray)
            {
                var wall = walls.SingleOrDefault(candidate => candidate.ObstacleData.CustomTrack?.Value == expected["track"].Value);
                if (wall == null)
                {
                    failures.Add($"Missing {expected["track"]}");
                    continue;
                }
                var actual = wall.MpbController.Mpb.GetColor("_Color");
                var color = expected["color"];
                var error = Vector4.Distance(actual,
                    new Vector4(color[0].AsFloat, color[1].AsFloat, color[2].AsFloat, color[3].AsFloat));
                if (error > 0.001f)
                    failures.Add($"{expected["track"]}: expected {color}, actual {actual}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator CapturedPrismDepths([Values(0, 1)] int capture)
        {
            yield return Load(capture);
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values.OfType<ObstacleContainer>().ToArray();
            foreach (JSONNode expected in expectations[capture]["prisms"].AsArray)
            {
                var wall = walls.Single(candidate => candidate.ObstacleData.CustomTrack?.Value == expected["track"].Value);
                Assert.That(wall.Animator.LocalTarget.position.z, Is.EqualTo(expected["position"][2].AsFloat).Within(0.001f),
                    $"{expected["track"]} must retain the native definite-position depth, including its approach.");
            }
        }

        [UnityTest]
        public IEnumerator CapturedRingColors([Values(0, 1)] int capture)
        {
            yield return Load(capture);
            var lights = Object.FindObjectsByType<LightController>(FindObjectsSortMode.None)
                .Where(light => light.GetComponentInParent<TrackLaneRing>() != null).ToArray();
            var failures = new List<string>();
            foreach (JSONNode expected in expectations[capture]["ringLights"].AsArray)
            {
                var light = lights.SingleOrDefault(candidate => candidate.name == expected["name"].Value
                    && Mathf.Abs(candidate.GetComponentInParent<TrackLaneRing>().transform.position.z
                        - expected["ringZ"].AsFloat) < 0.001f);
                if (light == null)
                {
                    failures.Add($"Missing {expected["name"]} at Z={expected["ringZ"]}");
                    continue;
                }
                var color = expected["color"];
                var actual = light.Color;
                var error = Mathf.Abs(actual.a - color[3].AsFloat);
                if (color[3].AsFloat > 0f)
                    error = Mathf.Max(error, Mathf.Abs(actual.r - color[0].AsFloat),
                        Mathf.Abs(actual.g - color[1].AsFloat), Mathf.Abs(actual.b - color[2].AsFloat));
                if (error > 0.001f)
                    failures.Add($"{expected["name"]} Z={expected["ringZ"]}: expected {color}, actual {actual}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private IEnumerator Load(int capture)
        {
            // The reduced fixture retains native note identities, signed prism colors, and the legacy ring selectors.
            expectations = JSON.Parse(File.ReadAllText(PathUtils.Combine(FixtureRoot, "NulctrlBeat43Expectations.json")));
            var info = JSON.Parse(File.ReadAllText(PathUtils.Combine(FixtureRoot, "NulctrlBeat43Info.json")));
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
            Settings.Instance.PlayerCameraFOV = 100f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var mapPath = PathUtils.Combine(FixtureRoot, "NulctrlBeat43Fixture.json");
            yield return TestUtils.ReloadMap(2, JSON.Parse(File.ReadAllText(mapPath)),
                beatsPerMinute: 200f, environmentName: "OriginsEnvironment", songLengthSeconds: 240,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 19f, NoteStartBeatOffset = 0.37f,
                    CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                    CustomData = info["_difficultyBeatmapSets"][0]["_difficultyBeatmaps"][0]["_customData"].AsObject
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            cameras.SelectedCameraController.Camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
            clock.MoveToTimeInSeconds(expectations[capture]["previousRenderedSongSeconds"].AsFloat);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, clock.CurrentSongBpmTime);
            AdvanceCapture(capture);
            yield return null;
            yield return null;
        }

        private void AdvanceCapture(int capture)
        {
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock,
                clock.GetBeatFromSeconds(expectations[capture]["previousRenderedSongSeconds"].AsFloat));
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock,
                clock.GetBeatFromSeconds(expectations[capture]["songSeconds"].AsFloat));
        }

        private static Vector3 Vector(JSONNode value) => new(value[0].AsFloat, value[1].AsFloat, value[2].AsFloat);

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
                clock.enabled = previousClockEnabled;
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChromaLite;
            Settings.Instance.EmulateChromaAdvanced = previousChromaAdvanced;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            Settings.Instance.AccurateNoteSize = previousAccurateSize;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
