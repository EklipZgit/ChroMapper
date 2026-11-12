using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class AngstOpeningColorParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChroma;
        private bool previousAdvancedChroma;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChroma = Settings.Instance.EmulateChromaLite;
            previousAdvancedChroma = Settings.Instance.EmulateChromaAdvanced;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest, Explicit("Requires the complete local Angst difficulty and writes a comparison frame.")]
        public IEnumerator FullMapOpeningKeepsAuthoredPurpleGradient()
        {
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "AngstOpeningColor");
            var sourcePath = PathUtils.Combine(directory, "ExpertPlusStandard.dat");
            if (!File.Exists(sourcePath))
                Assert.Ignore("Requires the complete Angst ExpertPlus Standard map in Scratch/AngstOpeningColor.");

            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(sourcePath)),
                beatsPerMinute: 150f,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 240,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 19f,
                    NoteStartBeatOffset = -0.5f,
                    CustomSuggestions = new List<string> { "Chroma" },
                    CustomRequirements = new List<string> { "Noodle Extensions" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(10.125f);
            yield return null;
            yield return null;

            var lights = Object.FindObjectsByType<LightController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(light => light.Kind == LightController.LightKind.Basic && light is not LightSink).ToArray();
            Assert.That(lights.Any(light => light.Type == 4), Is.True);
            File.WriteAllLines(
                PathUtils.Combine(directory, "lights.txt"),
                lights.Select(
                    light => $"{light.name} {light.GetType().Name} type={light.Type} id={light.ID} color={light.Color.ToString("R")}"));
            LinearRenderCapture.Capture(
                cameras.SelectedCameraController.Camera,
                PathUtils.Combine(directory, "beat10.125.png"));

            foreach (var light in lights.Where(light => light.Type == 4))
            {
                Assert.That(light.Color.r, Is.EqualTo(0.736999989f).Within(0.0001f), light.name);
                Assert.That(light.Color.g, Is.EqualTo(0.0270000007f).Within(0.0001f), light.name);
                Assert.That(light.Color.b, Is.EqualTo(0.917999983f).Within(0.0001f), light.name);
                var alpha = (1f - Mathf.Cos(Mathf.PI * (1.125f / 42f))) * 0.5f * 0.7490196f;
                Assert.That(light.Color.a, Is.EqualTo(alpha).Within(0.00001f), light.name);
            }

            // Chroma's Off callback uses the first channel, even when the preceding event was red.
            var effect = Object.FindAnyObjectByType<EnvironmentDescriptor>()
                .BasicEventEffectManager.GetEffect<BasicLightEffect>(0);
            var offColor = effect.ColorSchemeProvider.ColorScheme.GetColorFrom(
                Beatmap.Enums.LightColor.Blue,
                effect.InvertColorScheme);
            foreach (var light in GetOffLights())
            {
                Assert.That(light.Color.r, Is.EqualTo(offColor.r).Within(0.0001f), light.name);
                Assert.That(light.Color.g, Is.EqualTo(offColor.g).Within(0.0001f), light.name);
                Assert.That(light.Color.b, Is.EqualTo(offColor.b).Within(0.0001f), light.name);
                Assert.That(light.Color.a, Is.EqualTo(0.6f).Within(0.0001f), light.name);
            }
        }

        [UnityTest]
        public IEnumerator OpeningOffLightsUseChromaFirstChannelAcrossSeeks()
        {
            yield return LoadOpeningFixture(true);
            foreach (var beat in new[] { 0.375f, 10.125f, 78.25f, 10.125f })
            {
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                yield return null;
                if (beat < 78f)
                    AssertOffPalette(LightColor.Blue, beat);
            }
        }

        [UnityTest]
        public IEnumerator OpeningOffBrightnessHoldsUntilNextEvent()
        {
            yield return LoadOpeningFixture(true);
            foreach (var beat in new[] { 0.375f, 10.125f, 50f, 77.984375f, 78.25f, 10.125f })
            {
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                yield return null;
                if (beat >= 78f)
                    continue;

                foreach (var light in GetOffLights())
                    Assert.That(light.Color.a, Is.EqualTo(0.6f).Within(0.00001f), $"{light.name} at beat {beat}");
            }
        }

        [UnityTest]
        public IEnumerator VanillaOffRetainsPreviousChannel()
        {
            yield return LoadOpeningFixture(false);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(10.125f);
            yield return null;
            AssertOffPalette(LightColor.Red, 10.125f);
        }

        [UnityTest]
        public IEnumerator ChromaOffRetainsItsExplicitColorAndReplacesItsAlpha()
        {
            var fixture = ReadOpeningFixture();
            var color = new Color(0.1f, 0.8f, 0.3f, 0.2f);
            fixture["_events"][3]["_customData"] = new JSONObject { ["_color"] = color };
            yield return LoadOpeningFixture(true, fixture);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(10.125f);
            yield return null;
            foreach (var light in GetOffLights())
            {
                Assert.That(light.Color.r, Is.EqualTo(color.r).Within(0.0001f), light.name);
                Assert.That(light.Color.g, Is.EqualTo(color.g).Within(0.0001f), light.name);
                Assert.That(light.Color.b, Is.EqualTo(color.b).Within(0.0001f), light.name);
                Assert.That(light.Color.a, Is.EqualTo(0.6f).Within(0.00001f), light.name);
            }
        }

        private static JSONNode ReadOpeningFixture()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "AngstOpeningColorFixture.json");
            return JSON.Parse(File.ReadAllText(path));
        }

        private static IEnumerator LoadOpeningFixture(bool chroma, JSONNode fixture = null)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = chroma;
            Settings.Instance.EmulateChromaAdvanced = chroma;
            yield return TestUtils.ReloadMap(
                2,
                fixture ?? ReadOpeningFixture(),
                beatsPerMinute: 150f,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 40,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    CustomSuggestions = new List<string> { "Chroma" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static LightController[] GetOffLights()
        {
            // These are the native type-0 fog lights that make the opening background visible.
            var lights = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(light => light.Kind == LightController.LightKind.Basic && light.Type == 0)
                .ToArray();
            Assert.That(lights, Is.Not.Empty);
            return lights;
        }

        private static void AssertOffPalette(LightColor channel, float beat)
        {
            var effect = Object.FindAnyObjectByType<EnvironmentDescriptor>()
                .BasicEventEffectManager.GetEffect<BasicLightEffect>(0);
            var expected = effect.ColorSchemeProvider.ColorScheme.GetColorFrom(channel, effect.InvertColorScheme);
            foreach (var light in GetOffLights())
            {
                Assert.That(light.Color.r, Is.EqualTo(expected.r).Within(0.0001f), $"{light.name} at beat {beat}");
                Assert.That(light.Color.g, Is.EqualTo(expected.g).Within(0.0001f), $"{light.name} at beat {beat}");
                Assert.That(light.Color.b, Is.EqualTo(expected.b).Within(0.0001f), $"{light.name} at beat {beat}");
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChroma;
            Settings.Instance.EmulateChromaAdvanced = previousAdvancedChroma;
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
