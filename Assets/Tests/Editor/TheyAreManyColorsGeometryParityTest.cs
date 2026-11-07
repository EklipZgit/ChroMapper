using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class TheyAreManyColorsGeometryParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChroma;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChroma = Settings.Instance.EmulateChromaLite;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator EyePixelsRemainSquareAfterParentFlattensTheCubes() => CheckEye("Eye", 337);

        [UnityTest]
        public IEnumerator SingleCubeKeepsItsRotationWhileItsParentHasZeroScale() => CheckEye("SingleCube", 1);

        [UnityTest]
        public IEnumerator PlayingAgainAfterTheEndRestoresTheOpeningSquare()
        {
            var fixture = JSON.Parse(
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Tests/Fixtures/TheyAreManyColorsRestartFixture.json").text);
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(
                3,
                fixture,
                beatsPerMinute: 180f,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 60);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(1f);
            yield return null;
            yield return null;
            var square = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .OrderBy(value => value.EnvironmentEnhancement.Track).ToArray();
            Assert.That(square.Length, Is.EqualTo(4));
            var fresh = square.Select(value => value.MpbController.Renderers.Single().localToWorldMatrix).ToArray();

            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 1f);
            for (var restart = 0; restart < 2; restart++)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(
                    clock,
                    clock.GetBeatFromSeconds(clock.SongAudioSource.clip.length));
                yield return null;
                yield return null;
                Assert.That(
                    square[0].MpbController.Renderers.Single().bounds.size.y,
                    Is.LessThan(0.0001f),
                    "The retained end event must collapse the square before the restart.");
                yield return clock.StopPlayingDelayed(0f);
                Assert.That(clock.IsPlaying, Is.False);
                Assert.That(clock.CurrentSeconds, Is.GreaterThanOrEqualTo(clock.SongAudioSource.clip.length - 0.1f));

                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 1f);
                yield return null;
                yield return null;
                for (var cube = 0; cube < square.Length; cube++)
                {
                    var actual = square[cube].MpbController.Renderers.Single().localToWorldMatrix;
                    for (var element = 0; element < 16; element++)
                        Assert.That(
                            actual[element],
                            Is.EqualTo(fresh[cube][element]).Within(0.0001f),
                            $"{square[cube].EnvironmentEnhancement.Track}, restart {restart}, matrix element {element}");
                }
            }
        }

        private IEnumerator CheckEye(string fixtureName, int cubeCount)
        {
            var fixturePath = $"Assets/Tests/Fixtures/TheyAreManyColors{fixtureName}Fixture.json";
            var fixture = JSON.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                fixturePath).text);
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(
                3,
                fixture,
                beatsPerMinute: 180f,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 300);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(357f);
            yield return null;
            yield return null;

            var cubes = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Where(value => value.EnvironmentEnhancement.Track.StartsWith("open eye_"))
                .ToArray();
            Assert.That(cubes.Length, Is.EqualTo(cubeCount));
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "TheyAreManyColors");
            Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Eye pixel regression camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.transform.position = new Vector3(-1.5f, 3.5f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0.025f, 0f);
            camera.cullingMask = 1 << 31;
            try
            {
                foreach (var cube in cubes)
                {
                    var renderer = cube.MpbController.Renderers.Single();
                    renderer.gameObject.layer = 31;
                }

                LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, fixtureName + "-actual.png"), 1280, 640);
                fixture = JSON.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(fixturePath).text);
                var parent = Matrix4x4.TRS(new Vector3(-10f, -14f, 15f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.01f));
                foreach (var beat in new[] { 357f, 351f, 350f, 0f, 357f, 357f + 1f / 64f, 357f, 357.125f })
                {
                    if (beat == 357.125f)
                    {
                        TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 357f);
                        TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                    }
                    else
                    {
                        clock.MoveToJsonTime(beat);
                    }
                    yield return null;
                    if (beat < 351f)
                        continue;

                    foreach (var cube in cubes)
                    {
                        var track = cube.EnvironmentEnhancement.Track;
                        var initialize = fixture["customData"]["customEvents"].Children
                            .Single(value => value["d"]["track"].Value == track);
                        var point = initialize["d"]["localPosition"][0];
                        var expected = parent * Matrix4x4.TRS(
                            new Vector3(point[0].AsFloat, point[1].AsFloat, point[2].AsFloat),
                            Quaternion.identity,
                            Vector3.one);
                        var actual = cube.MpbController.Renderers.Single().localToWorldMatrix;
                        for (var index = 0; index < 16; index++)
                            Assert.That(actual[index], Is.EqualTo(expected[index]).Within(0.0001f), $"{track} at {beat}, matrix element {index}");
                    }
                }

            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator BackgroundWallsKeepTheirFourBeatRotationsDuringSmallScrubs()
        {
            var fixture = JSON.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Tests/Fixtures/TheyAreManyColorsBackgroundFixture.json").text);
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(3, fixture, beatsPerMinute: 180f, songLengthSeconds: 300);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            foreach (var beat in new[] { 15f, 15f + 1f / 64f, 15f - 1f / 64f, 14f, 16f, 15f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var walls = Object.FindObjectsByType<ObstacleContainer>(FindObjectsSortMode.None)
                    .Where(value => value.ObstacleData.CustomTrack != null).ToArray();
                Assert.That(walls.Length, Is.EqualTo(3));
                var phase = Mathf.Repeat((beat - 2.5f) / 4f, 1f);
                foreach (var wall in walls)
                {
                    var axis = wall.ObstacleData.CustomTrack.Value switch
                    {
                        "AHHHH69" => Vector3.forward,
                        "AHHHH2" => Vector3.up,
                        _ => Vector3.right
                    };
                    var expected = phase < 0.5f
                        ? Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Euler(axis * 180f), phase * 2f)
                        : Quaternion.SlerpUnclamped(Quaternion.Euler(axis * 180f), Quaternion.Euler(axis * 360f), (phase - 0.5f) * 2f);
                    Assert.That(Quaternion.Angle(wall.Animator.WorldTarget.rotation, expected), Is.LessThan(0.01f),
                        $"{wall.ObstacleData.CustomTrack} at {beat}");
                }
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChroma;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
