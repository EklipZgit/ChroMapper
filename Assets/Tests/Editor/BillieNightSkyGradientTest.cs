using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // BillieEnvironment's inactive Night group lost its serialized light-intensity inputs
    // in export, leaving the AfterBlur sky gradient tint black. This test drives the
    // production light path and asserts the gradient actually renders red bands.
    public class BillieNightSkyGradientTest : TestBase
    {
        private const string NightId =
            "BillieEnvironment.[0]Environment.[27]DayAndNight.[1]Night";
        private const int Width = 256;
        private const int Height = 144;

        private bool previousAnimations;
        private UIMode mode;

        protected override void CleanupTestObjects()
        {
        }

        [UnitySetUp]
        public IEnumerator SaveSettings()
        {
            previousAnimations = Settings.Instance.Animations;
            mode = Object.FindAnyObjectByType<UIMode>();
            yield break;
        }

        [UnityTest]
        public IEnumerator NightSkyGradientRendersRedBandsFromEventLights()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, Difficulty(), environmentName: "BillieEnvironment");

            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(6.5f);
            yield return null;
            yield return null;
            yield return null;

            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var nightMarker = descriptor.ChromaIDMarkers
                .FirstOrDefault(marker => marker.ChromaID == NightId);
            Assert.That(nightMarker, Is.Not.Null, "The environment must expose the Night marker.");

            var tintController = nightMarker
                .GetComponent<BloomPrePassBackgroundColorsGradientTintColorLightsController>();
            Assert.That(tintController, Is.Not.Null);
            var gradient = tintController.BloomPrePassBackgroundColorsGradient;
            Assert.That(gradient, Is.Not.Null);
            Assert.That(gradient.ExecutionTimeType, Is.EqualTo(BloomPrePassNonLightPass.ExecutionTime.AfterBlur));

            Color[] pixels;
            var cameraObject = new GameObject("SkyGradientCamera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.SetPositionAndRotation(
                    new Vector3(0f, 1.7f, 0f), Quaternion.identity);
                camera.fieldOfView = 90f;
                camera.aspect = (float)Width / Height;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.ResetProjectionMatrix();

                var target = new RenderTexture(
                    Width, Height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                var image = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true);
                var previousActive = RenderTexture.active;
                try
                {
                    target.Create();
                    RenderTexture.active = target;
                    GL.Clear(false, true, Color.clear);
                    gradient.Render(target, camera.worldToCameraMatrix, camera.projectionMatrix);
                    image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    image.Apply();
                    pixels = image.GetPixels();
                    SavePreview(image, PathUtils.Combine(
                        Application.dataPath, "..", "TestResults", "BillieNightSkyGradient",
                        "night-sky-gradient.png"));
                }
                finally
                {
                    RenderTexture.active = previousActive;
                    target.Release();
                    Object.DestroyImmediate(target);
                    Object.DestroyImmediate(image);
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }

            Color Sample(float xFraction, float yFraction) =>
                pixels[Mathf.FloorToInt(yFraction * (Height - 1)) * Width
                    + Mathf.FloorToInt(xFraction * (Width - 1))];

            var midRight = Sample(0.9f, 0.5f);
            var lowRight = Sample(0.9f, 0.2f);
            var highRight = Sample(0.9f, 0.8f);
            foreach (var (label, sample) in new[]
            {
                ("mid", midRight), ("low", lowRight), ("high", highRight)
            })
            {
                Assert.That(sample.r, Is.GreaterThan(0.001f),
                    $"Sky gradient pixel at {label} must carry red, got {sample}");
                Assert.That(sample.r, Is.GreaterThan(sample.g + sample.b),
                    $"Sky gradient pixel at {label} must be red-dominant, got {sample}");
            }

            Assert.That(Mathf.Abs(highRight.r - lowRight.r), Is.GreaterThan(0.0001f),
                "Sky gradient bands must vary with height, not fill flat");

            var tint = gradient.TintColor;
            Assert.That(tint.r, Is.GreaterThan(0.001f),
                $"Night gradient tint must pick up the lit moon's red, got {tint}");
            Assert.That(tint.r, Is.GreaterThan(tint.g + tint.b),
                $"Night gradient tint must be red-dominant, got {tint}");

            var directionalIds = new[]
            {
                NightId + ".[1]DirectionalLightFront",
                NightId + ".[2]DirectionalLightBack",
                NightId + ".[3]DirectionalLightLeft",
                NightId + ".[4]DirectionalLightRight"
            };
            foreach (var id in directionalIds)
            {
                var marker = descriptor.ChromaIDMarkers.FirstOrDefault(m => m.ChromaID == id);
                Assert.That(marker, Is.Not.Null, $"Missing marker for {id}");
                var directional = marker.GetComponent<DirectionalLightsController>();
                Assert.That(directional, Is.Not.Null, $"Missing controller for {id}");
                Assert.That(directional.Light, Is.Not.Null);
                Assert.That(directional.Light.Color.maxColorComponent, Is.GreaterThan(0.001f),
                    $"{id} directional light must receive event colors, got {directional.Light.Color}");
            }

            Assert.That(tintController.LightIntensityData, Has.Length.EqualTo(9),
                "Night's gradient tint must restore its nine serialized light-intensity inputs");
        }

        private static void SavePreview(Texture2D linear, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var displayPixels = new Color[linear.width * linear.height];
            var linearPixels = linear.GetPixels();
            for (var i = 0; i < linearPixels.Length; i++)
                displayPixels[i] = linearPixels[i].gamma;
            var display = new Texture2D(linear.width, linear.height, TextureFormat.RGB24, false);
            try
            {
                display.SetPixels(displayPixels);
                display.Apply();
                File.WriteAllBytes(path, display.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(display);
            }
        }

        private static JSONNode Difficulty() => JSON.Parse(@"{
            ""_version"": ""2.2.0"",
            ""_events"": [
                { ""_time"": 4, ""_type"": 5, ""_value"": 1 },
                { ""_time"": 6, ""_type"": 4, ""_value"": 5 }
            ],
            ""_notes"": [],
            ""_obstacles"": [],
            ""_waypoints"": [],
            ""_customData"": {
                ""_environment"": [
                    {
                        ""_id"": ""Moon"",
                        ""_lookupMethod"": ""Contains"",
                        ""_localPosition"": [0,1,325],
                        ""_scale"": [12.5,12.5,1]
                    }
                ],
                ""_customEvents"": []
            }
        }");

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            Settings.Instance.Animations = previousAnimations;
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);
            yield break;
        }
    }
}
