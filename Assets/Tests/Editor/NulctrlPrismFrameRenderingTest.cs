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
    public class NulctrlPrismFrameRenderingTest : TestBase
    {
        private bool previousAnimations;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator NegativeAlphaPreservesBrightFrameRgbAndWaterBaseRgb()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2,
                JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath,
                    "Tests/Fixtures/NulctrlPrismFrameFixture.json"))),
                beatsPerMinute: 200f, environmentName: "OriginsEnvironment", songLengthSeconds: 100,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 19f, NoteStartBeatOffset = 0.37f,
                    CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(43.3293648f);
            yield return null;
            yield return null;
            var wall = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values
                .OfType<ObstacleContainer>().Single();
            var frame = wall.OutlineTransform.GetComponentInChildren<MeshRenderer>();
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "NulctrlFrame");
            Directory.CreateDirectory(directory);
            wall.SetColor(new Color(10f, 10f, 10f, -5f));
            var actual = ObstacleFrameRenderCapture.Capture(frame, PathUtils.Combine(directory, "negative.png"));
            frame.enabled = true;
            var native = ObstacleFrameRenderCapture.Capture(frame, PathUtils.Combine(directory, "native-frame.png"));
            wall.SetColor(new Color(0.069f, 0f, 0f, -60f));
            var water = ObstacleFrameRenderCapture.Capture(frame, PathUtils.Combine(directory, "water.png"));
            var expectedEnergy = native.Sum(pixel => pixel.r + pixel.g + pixel.b);
            var actualEnergy = actual.Sum(pixel => pixel.r + pixel.g + pixel.b);
            Assert.That(expectedEnergy, Is.GreaterThan(1f), "The native bright-frame control must render visibly.");
            Assert.That(water.Max(pixel => pixel.r), Is.LessThan(0.1f), "The water's base frame RGB must remain dim before composition.");
            Assert.That(water.Max(pixel => Mathf.Max(pixel.g, pixel.b)), Is.LessThan(0.001f));
            Assert.That(actualEnergy, Is.EqualTo(expectedEnergy).Within(expectedEnergy * 0.01f),
                "Negative bloom alpha must preserve the native prism's bright frame RGB.");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
