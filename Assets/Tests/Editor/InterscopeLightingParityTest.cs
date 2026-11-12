using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class InterscopeLightingParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousBloom;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private Camera captureCamera;
        private float previousFov;
        private Vector3 previousPosition;
        private Quaternion previousRotation;

        private static JSONNode LoadNativeFrame()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "ReprogrammedPrincessNativeLighting.json");
            return JSON.Parse(File.ReadAllText(path));
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnitySetUp]
        public IEnumerator SaveSettings()
        {
            previousAnimations = Settings.Instance.Animations;
            previousBloom = Settings.Instance.Bloom;
            yield break;
        }

        private IEnumerator LoadEnvironment(string environment, JSONNode nativeFrame = null)
        {
            Settings.Instance.Animations = true;
            Settings.ApplyOptionByName(nameof(Settings.Bloom), true);
            yield return TestUtils.ReloadMap(
                3,
                nativeFrame == null ? new JSONObject { ["version"] = "3.3.0" } : nativeFrame["map"],
                beatsPerMinute: 160f,
                environmentName: environment,
                songLengthSeconds: 210,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 20f,
                    NoteStartBeatOffset = -0.485f,
                    CustomColorLeft = new Color(0.78431374f, 0.078431375f, 0.078431375f),
                    CustomColorRight = new Color(0.21960784f, 0.59607846f, 1f),
                    CustomEnvColorLeft = new Color(0.05882353f, 0.8039216f, 0.18431373f),
                    CustomEnvColorRight = new Color(0.22745098f, 0.3882353f, 1f),
                    CustomEnvColorBoostLeft = new Color(0.7529412f, 0.36078432f, 0.21960784f),
                    CustomEnvColorBoostRight = new Color(0.92156863f, 0.59607846f, 0.25490198f)
                },
                forceSceneReload: true);
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            if (nativeFrame == null)
                yield break;

            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToTimeInSeconds(nativeFrame["songTime"].AsFloat);
            captureCamera = cameras.SelectedCameraController.Camera;
            previousFov = captureCamera.fieldOfView;
            previousPosition = captureCamera.transform.position;
            previousRotation = captureCamera.transform.rotation;
            captureCamera.fieldOfView = 100f;
            captureCamera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LightmapChannelsMatchNativeFrame()
        {
            var native = LoadNativeFrame();
            yield return LoadEnvironment("InterscopeEnvironment", native);
            foreach (var channel in "ABCDEF")
            {
                var actual = Shader.GetGlobalColor("_LightmapLightBakeId" + channel);
                var expected = native["lightmapColors"][channel.ToString()];
                for (var component = 0; component < 3; component++)
                    Assert.That(actual[component], Is.EqualTo(expected[component].AsFloat).Within(0.002f), channel.ToString());
            }
        }

        [UnityTest]
        public IEnumerator LaserColorMatchesNativeFrame()
        {
            var native = LoadNativeFrame();
            yield return LoadEnvironment("InterscopeEnvironment", native);
            var light = Object.FindObjectsByType<ParametricBloomFogLightController>(FindObjectsSortMode.None)
                .First(controller => controller.GetComponent<ChromaIDMarker>().ChromaID.Contains(".[51]SinMoveLasersPair.[0]LaserL"));
            for (var component = 0; component < 4; component++)
                Assert.That(light.Color[component], Is.EqualTo(native["laserColor"][component].AsFloat).Within(0.002f));

        }

        [UnityTest]
        public IEnumerator FloorBrightnessMatchesNativeFrame()
        {
            var native = LoadNativeFrame();
            yield return LoadEnvironment("InterscopeEnvironment", native);
            captureCamera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
            captureCamera.fieldOfView = 100f;
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "InterscopeLightingParity");
            Directory.CreateDirectory(directory);
            var path = PathUtils.Combine(directory, "matched-camera.png");
            var pixels = LinearRenderCapture.Capture(captureCamera, path);
            var average = FloorAverage(pixels);
            for (var component = 0; component < 3; component++)
                Assert.That(average[component], Is.EqualTo(native["floorPatch"]["rgb"][component].AsFloat).Within(0.015f));
        }

        private static Color FloorAverage(Color[] pixels)
        {
            var average = Color.black;
            for (var y = 98; y < 108; y++)
            {
                for (var x = 896; x < 906; x++)
                    average += pixels[y * 1280 + x].gamma / 100f;
            }

            return average;
        }

        [UnityTest]
        public IEnumerator InterscopeFloorLogoKeepsNativeTint()
        {
            yield return LoadEnvironment("InterscopeEnvironment");
            var logo = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Single(renderer => renderer.name == "Logo");
            Assert.That(logo.color, Is.EqualTo(new Color(0f, 0f, 0f, 178f / 255f)));
        }

        [UnityTest]
        public IEnumerator LinkinParkFloorDecalsKeepNativeTint()
        {
            yield return LoadEnvironment("LinkinParkEnvironment");
            var decals = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(renderer => renderer.name.StartsWith("LinkinPark")).ToArray();
            Assert.That(decals.Length, Is.EqualTo(3));
            foreach (var decal in decals)
                Assert.That(decal.color, Is.EqualTo(new Color(0f, 0f, 0f, 192f / 255f)), decal.name);
        }

        [UnityTest]
        public IEnumerator LaserCoreReceivesNativeTubeAlpha()
        {
            yield return LoadEnvironment("InterscopeEnvironment");
            var light = Object.FindObjectsByType<ParametricBloomFogLightController>(FindObjectsSortMode.None)
                .First(controller => controller.GetComponent<ChromaIDMarker>().ChromaID.Contains(".[51]SinMoveLasersPair.[0]LaserL"));
            light.SetColor(new Color(0.2f, 0.4f, 0.6f, 0.25f));
            var block = new MaterialPropertyBlock();
            light.BoxLight.Renderer.GetPropertyBlock(block);
            Assert.That(
                block.GetColor("_Color").a,
                Is.EqualTo(0.225f).Within(0.00001f),
                "The native tube's 0.9 alpha multiplier must reach its physical core.");
            Assert.That(light.BoxLight.Width, Is.EqualTo(0.02f).Within(0.00001f));
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (captureCamera != null)
            {
                captureCamera.fieldOfView = previousFov;
                captureCamera.transform.SetPositionAndRotation(previousPosition, previousRotation);
                captureCamera = null;
            }

            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            if (clock != null)
                clock.enabled = previousClockEnabled;

            Settings.Instance.Animations = previousAnimations;
            Settings.ApplyOptionByName(nameof(Settings.Bloom), previousBloom);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
        }
    }
}
