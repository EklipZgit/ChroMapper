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
    public class ChangeOfSceneMountainsTest : TestBase
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
        public IEnumerator FullMapMountainsOccludeTheSkyAtBeat194()
        {
            yield return CheckMountains(
                @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\Chroma\3f19a (Change of scene - Loopymtb)\ExpertPlusStandard.dat");
        }

        [UnityTest]
        public IEnumerator AuthoredDistantMountainsRemainOpaque()
        {
            yield return CheckMountains(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "ChangeOfSceneMountainsFixture.json"));
        }

        private IEnumerator CheckMountains(string path)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(path)),
                editorState: new JSONObject
                {
                    ["components"] = new JSONObject
                    {
                        ["currentJsonTime"] = new JSONObject { ["value"] = 194.015625f, ["gridMeasureSnapping"] = 64 }
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
            cameras.SelectedCameraController.Camera.fieldOfView = 90f;
            yield return null;
            yield return null;

            Assert.That(clock.CurrentJsonTime, Is.EqualTo(194.015625f).Within(0.001f));
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "ChangeOfSceneMountains",
                System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            Directory.CreateDirectory(directory);
            var camera = cameras.SelectedCameraController.Camera;
            var actual = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "actual.png"));
            var mountains = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Where(value => value.EnvironmentEnhancement?.Track?.StartsWith("modelScene0_Dirt_") == true)
                .SelectMany(value => value.MpbController.Renderers)
                .Where(value => value.bounds.center.z > 1800f && value.bounds.center.y > 0f)
                .ToArray();
            Assert.That(mountains.Length, Is.GreaterThan(10));
            foreach (var renderer in mountains)
                renderer.enabled = false;

            var absent = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "without-mountains.png"));
            foreach (var renderer in mountains)
                renderer.enabled = true;

            // Count visible silhouette pixels in display space so native HDR bloom corrections do not change the contrast threshold.
            var changed = actual.Zip(absent, (first, second) => Vector4.Distance(first.gamma, second.gamma))
                .Count(value => value > 0.01f);
            File.WriteAllText(PathUtils.Combine(directory, "metrics.json"), new JSONObject
            {
                ["mountainRenderers"] = mountains.Length,
                ["mountainChangedPixels"] = changed,
                ["farClip"] = camera.farClipPlane,
                ["cameraPosition"] = camera.transform.position.ToString(),
                ["keywords"] = string.Join(",", mountains[0].sharedMaterial.shaderKeywords)
            }.ToString(2));
            Assert.That(changed, Is.GreaterThan(5000), "The distant mountain silhouettes must occlude the bright sky.");
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (clock != null)
                clock.enabled = previousClockEnabled;

            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

