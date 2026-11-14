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
    public class MegamixWallGraphicsSettingsTest : TestBase
    {
        private bool previousAnimations;
        private bool previousScreenDisplacement;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private Camera captureCamera;
        private GameObject background;
        private Material backgroundMaterial;
        private Texture2D backgroundTexture;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousScreenDisplacement = Settings.Instance.ScreenDisplacement;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [SetUp]
        public void CaptureClockState() =>
            previousClockEnabled = Object.FindAnyObjectByType<AudioTimeSyncController>().enabled;

        [UnityTest, Explicit("Requires the original Megamix map copied to workspace Scratch/MegamixWallDistortion482.")]
        public IEnumerator OriginalMapDisablesWallDistortionAtBeat482()
        {
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "MegamixWallDistortion482");
            var info = JSON.Parse(File.ReadAllText(PathUtils.Combine(directory, "Info.dat")));
            var metadata = info["_difficultyBeatmapSets"].Children.Single(set => set["_beatmapCharacteristicName"] == "Lawless")
                ["_difficultyBeatmaps"].Children.Single(diff => diff["_beatmapFilename"] == "ExpertPlusLawless.dat");
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 18f,
                NoteStartBeatOffset = -0.75f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                CustomData = metadata["_customData"].Clone().AsObject
            };
            Assert.That(difficulty.CustomData["_settings"]["_graphics"]["_screenDisplacementEffectsEnabled"].AsBool, Is.False);
            yield return LoadWallMap(
                JSON.Parse(File.ReadAllText(PathUtils.Combine(directory, "ExpertPlusLawless.dat"))), difficulty);
            LinearRenderCapture.Capture(cameras.SelectedCameraController.Camera,
                PathUtils.Combine(directory, "full-map.png"));
            AssertFlatWallRoute();
        }

        [UnityTest]
        public IEnumerator MapSettingDisablesDistortionForOneOriginalWall()
        {
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 18f,
                NoteStartBeatOffset = -0.75f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                CustomData = JSON.Parse(@"{""_settings"":{""_graphics"":{""_screenDisplacementEffectsEnabled"":false}}}").AsObject
            };
            yield return LoadWallMap(JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath, "Tests", "Fixtures", "MegamixWallGraphicsFixture.json"))), difficulty);
            Assert.That(Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Count, Is.EqualTo(1));
            AssertFlatWallRoute();
        }

        [UnityTest]
        public IEnumerator MaterialSwapUsesDifficultyOverrideAndLivePreference(
            [Values(-1, 0, 1)] int mapOverride, [Values(false, true)] bool preference)
        {
            yield return LoadWallMap(ReadWallFixture(), CreateDifficulty(mapOverride), preference);
            var wall = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values
                .OfType<ObstacleContainer>().Single();
            foreach (var currentPreference in new[] { preference, !preference })
            {
                Settings.ApplyOptionByName(nameof(Settings.ScreenDisplacement), currentPreference);
                var enabled = mapOverride < 0 ? currentPreference : mapOverride == 1;
                foreach (var view in new[] { UIModeType.Normal, UIModeType.Preview, UIModeType.Playing })
                {
                    mode.SetUIMode(view, false);
                    yield return null;
                    wall = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values
                        .OfType<ObstacleContainer>().Single();
                    var preview = view is UIModeType.Preview or UIModeType.Playing;
                    var expectedShader = !preview ? "ChroMapper/Object/Obstacle Simple"
                        : enabled ? "ChroMapper/Object/Obstacle Distortion" : "ChroMapper/Object/Obstacle Core LW";
                    Assert.That(wall.CoreRenderer.sharedMaterial.shader.name, Is.EqualTo(expectedShader));
                    Assert.That(wall.CoreRenderer.GetComponent<ScreenDisplacementRenderer>().IsReady,
                        Is.EqualTo(preview && enabled));
                    Assert.That(Settings.Instance.ScreenDisplacement, Is.EqualTo(currentPreference));
                }
            }
        }

        [UnityTest]
        public IEnumerator LoadingAnotherDifficultyClearsThePreviousGraphicsOverride()
        {
            foreach (var value in new[] { 0, -1, 1, 0 })
            {
                yield return LoadWallMap(ReadWallFixture(), CreateDifficulty(value));
                Assert.That(Object.FindAnyObjectByType<BeatmapRuntimeContext>().ScreenDisplacementEnabled,
                    Is.EqualTo(value != 0));
                var wall = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values
                    .OfType<ObstacleContainer>().Single();
                Assert.That(wall.CoreRenderer.GetComponent<ScreenDisplacementRenderer>().IsReady, Is.EqualTo(value != 0));
            }
        }

        [UnityTest]
        public IEnumerator FlatCorePreservesTheStraightFramePixels() => CheckFramePixels(false);

        [UnityTest]
        public IEnumerator DistortionCoreRetainsItsScreenCopy() => CheckFramePixels(true);

        private IEnumerator CheckFramePixels(bool displacement)
        {
            yield return LoadWallMap(ReadWallFixture(), CreateDifficulty(displacement ? 1 : 0));
            var wall = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values
                .OfType<ObstacleContainer>().Single();
            wall.SetColor(Color.white);
            captureCamera = new GameObject("Wall material comparison").AddComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = 4f;
            captureCamera.aspect = 16f / 9f;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.black;
            captureCamera.cullingMask = 1 << LayerMask.NameToLayer("Beatmap Object");
            var center = wall.CoreRenderer.bounds.center;
            captureCamera.transform.position = center + wall.CoreTransform.right * 20f;
            captureCamera.transform.LookAt(center, wall.CoreTransform.up);
            background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.layer = LayerMask.NameToLayer("Beatmap Object");
            background.transform.SetPositionAndRotation(center - wall.CoreTransform.right * 10f, captureCamera.transform.rotation);
            background.transform.localScale = Vector3.one * 30f;
            backgroundTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false, true);
            for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
                backgroundTexture.SetPixel(x, y, new Color(0.05f + x / 60f, 0.03f, 0.05f + y / 60f));
            backgroundTexture.Apply();
            backgroundMaterial = new Material(Shader.Find("Unlit/Texture"));
            backgroundMaterial.mainTexture = backgroundTexture;
            background.GetComponent<Renderer>().sharedMaterial = backgroundMaterial;
            var controller = cameras.PlayerCameraController;
            var originalCamera = controller.Camera;
            controller.Camera = captureCamera;
            Object.FindAnyObjectByType<ScreenDisplacementRenderingController>().AssignToCamera(controller);
            controller.Camera = originalCamera;
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "MegamixWallDistortion482");
            Directory.CreateDirectory(directory);
            var label = displacement ? "distorted" : "flat";
            var actual = LinearRenderCapture.Capture(captureCamera, PathUtils.Combine(directory, label + "-reduced.png"), 640, 360);
            var originalMaterial = wall.CoreRenderer.sharedMaterial;
            wall.CoreRenderer.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Object/ObstacleCoreLW.mat");
            wall.CoreRenderer.GetComponent<ScreenDisplacementRenderer>().RefreshMaterial();
            var flat = LinearRenderCapture.Capture(captureCamera, PathUtils.Combine(directory, label + "-flat-reference.png"), 640, 360);
            wall.CoreRenderer.enabled = false;
            var reference = LinearRenderCapture.Capture(captureCamera, PathUtils.Combine(directory, label + "-frame-reference.png"), 640, 360);
            wall.CoreRenderer.enabled = true;
            wall.CoreRenderer.sharedMaterial = originalMaterial;
            wall.CoreRenderer.GetComponent<ScreenDisplacementRenderer>().RefreshMaterial();
            var samples = 0;
            var totalError = 0f;
            var coreSamples = 0;
            var coreError = 0f;
            for (var index = 0; index < reference.Length; index++)
            {
                var coreMask = flat[index] - reference[index];
                if (Mathf.Max(Mathf.Abs(coreMask.r), Mathf.Abs(coreMask.g), Mathf.Abs(coreMask.b)) > 0.05f)
                {
                    coreSamples++;
                    var coreDifference = actual[index] - flat[index];
                    coreError += Mathf.Max(Mathf.Abs(coreDifference.r), Mathf.Abs(coreDifference.g), Mathf.Abs(coreDifference.b));
                }
                if (reference[index].maxColorComponent < 0.5f)
                    continue;
                samples++;
                var difference = actual[index] - reference[index];
                totalError += Mathf.Max(Mathf.Abs(difference.r), Mathf.Abs(difference.g), Mathf.Abs(difference.b));
            }
            Assert.That(samples, Is.GreaterThan(80), "The control must render enough of the wall frame to measure distortion.");
            Assert.That(coreSamples, Is.GreaterThan(80));
            var error = totalError / samples;
            File.WriteAllText(PathUtils.Combine(directory, label + "-pixels.json"), new JSONObject
            {
                ["framePixels"] = samples, ["meanMaximumRgbError"] = error,
                ["corePixels"] = coreSamples, ["meanCoreRgbError"] = coreError / coreSamples,
                ["width"] = 640, ["height"] = 360
            }.ToString());
            if (displacement)
                Assert.That(coreError / coreSamples, Is.GreaterThan(0.01f),
                    "The displacement-enabled control must retain its screen-copy appearance.");
            else
            {
                Assert.That(coreError / coreSamples, Is.LessThan(0.001f));
                Assert.That(error, Is.LessThan(0.01f),
                    "A flat core must preserve the original frame pixels rather than warping them through a screen copy.");
            }
        }

        private static JSONNode ReadWallFixture() => JSON.Parse(File.ReadAllText(PathUtils.Combine(
            Application.dataPath, "Tests", "Fixtures", "MegamixWallGraphicsFixture.json")));

        private static InfoDifficulty CreateDifficulty(int mapOverride)
        {
            var data = new JSONObject();
            if (mapOverride >= 0)
                data["_settings"]["_graphics"]["_screenDisplacementEffectsEnabled"] = mapOverride == 1;
            return new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 18f, NoteStartBeatOffset = -0.75f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" }, CustomData = data
            };
        }

        private IEnumerator LoadWallMap(JSONNode map, InfoDifficulty difficulty, bool preference = true)
        {
            Settings.Instance.Animations = true;
            Settings.ApplyOptionByName(nameof(Settings.ScreenDisplacement), preference);
            yield return TestUtils.ReloadMap(
                3,
                map,
                beatsPerMinute: 160f,
                environmentName: "BigMirrorEnvironment",
                songLengthSeconds: 300,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.enabled = false;
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(482.5f);
            yield return null;
            yield return null;
        }

        private static void AssertFlatWallRoute()
        {
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>().LoadedContainers.Values
                .OfType<ObstacleContainer>().ToArray();
            Assert.That(walls, Is.Not.Empty);
            Assert.That(walls.Any(wall => wall.CoreRenderer.GetComponent<ScreenDisplacementRenderer>().IsReady), Is.False,
                "Megamix requests screen displacement off; its walls must use the flat core rather than the distortion pass.");
            Assert.That(Settings.Instance.ScreenDisplacement, Is.True,
                "Map graphics overrides must not overwrite the editor's saved preference.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);
            if (captureCamera != null)
                Object.DestroyImmediate(captureCamera.gameObject);
            if (background != null)
                Object.DestroyImmediate(background);
            if (backgroundMaterial != null)
                Object.DestroyImmediate(backgroundMaterial);
            if (backgroundTexture != null)
                Object.DestroyImmediate(backgroundTexture);
            if (clock != null)
                clock.enabled = previousClockEnabled;
            Settings.Instance.Animations = previousAnimations;
            Settings.ApplyOptionByName(nameof(Settings.ScreenDisplacement), previousScreenDisplacement);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
