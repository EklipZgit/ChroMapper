using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChangeOfSceneScaffoldingTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousFov;
        private float previousOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private GameObject captureObject;
        private bool useBuiltShader;
        private readonly List<Material> builtMaterials = new();
        private AssetBundle shaderBundle;
        private static readonly string[] braceTracks =
        {
            "modelScene0_Dirt_0", "modelScene0_Dirt_1", "modelScene0_Dirt_12",
            "modelScene0_Dirt_13", "modelScene0_Dirt_14", "modelScene0_Dirt_15"
        };

        [UnityTest]
        [PrebuildSetup(typeof(ChangeOfSceneShaderBundleSetup))]
        [Explicit("Compiles a shader-only AssetBundle to reproduce player variant stripping.")]
        public IEnumerator BuiltConcreteShaderRetainsAuthoredBraceVariant()
        {
            useBuiltShader = true;
            yield return AuthoredConcreteBracesRemainVisibleAfterSceneTransition();
        }

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
        public IEnumerator AuthoredConcreteBracesRemainVisibleAfterSceneTransition()
        {
            var path = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "ChangeOfSceneScaffoldingFixture.json");
            yield return CheckScaffolding(path, "minimal");
        }

        [UnityTest]
        [Explicit("Requires the local Change of scene map.")]
        public IEnumerator FullMapConcreteBracesRemainVisibleAfterSceneTransition()
        {
            yield return CheckScaffolding(
                @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\Chroma\3f19a (Change of scene - Loopymtb)\ExpertPlusStandard.dat",
                "full");
        }

        private IEnumerator CheckScaffolding(string path, string label)
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
                        ["currentJsonTime"] = new JSONObject { ["value"] = 79.546875f, ["gridMeasureSnapping"] = 64 }
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

            var rootDirectory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "ChangeOfSceneScaffolding");
            var directory = PathUtils.Combine(
                rootDirectory,
                System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + (useBuiltShader ? "built" : label));
            Directory.CreateDirectory(directory);
            LinearRenderCapture.Capture(cameras.SelectedCameraController.Camera, PathUtils.Combine(directory, label + "-scene.png"));
            Assert.That(clock.CurrentJsonTime, Is.EqualTo(79.546875f).Within(0.001f));

            var braces = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Where(value => braceTracks.Contains(value.EnvironmentEnhancement.Track))
                .ToArray();
            Assert.That(braces.Length, Is.EqualTo(braceTracks.Length));
            var upright = braces.Single(value => value.EnvironmentEnhancement.Track == "modelScene0_Dirt_0");
            var renderer = upright.MpbController.Renderers.Single();
            Assert.That(
                Vector3.Distance(renderer.bounds.center, new Vector3(21.0436153f, 61.0825195f, 244.4560852f) * 0.6f),
                Is.LessThan(0.01f),
                "The hidden brace did not receive its beat-78 placement.");

            if (useBuiltShader)
            {
                var output = PathUtils.Combine(rootDirectory, "shader-bundle");
                shaderBundle = AssetBundle.LoadFromFile(PathUtils.Combine(output, "concrete"));
                Assert.That(shaderBundle, Is.Not.Null);
                var compiled = shaderBundle.LoadAsset<Material>("Assets/_Graphics/Materials/Environments/Interscope/Concrete2.mat");
                Assert.That(
                    compiled.shader,
                    Is.Not.SameAs(renderer.sharedMaterial.shader),
                    "The bundle must use its compiled shader rather than the editor's on-demand compiler.");
                foreach (var brace in braces)
                {
                    foreach (var target in brace.MpbController.Renderers)
                    {
                        var material = new Material(compiled);
                        builtMaterials.Add(material);
                        material.shaderKeywords = target.sharedMaterial.shaderKeywords;
                        target.sharedMaterial = material;
                    }
                }
                LinearRenderCapture.Capture(
                    cameras.SelectedCameraController.Camera,
                    PathUtils.Combine(directory, "built-scene.png"));
            }

            captureObject = new GameObject("Scaffolding silhouette capture");
            var camera = captureObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.cullingMask = 1 << 31;
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            var right = braces.Single(value => value.EnvironmentEnhancement.Track == "modelScene0_Dirt_1")
                .MpbController.Renderers.Single();
            camera.transform.position = (renderer.bounds.center + right.bounds.center) * 0.5f + Vector3.back * 20f;
            foreach (var brace in braces)
            {
                foreach (var target in brace.MpbController.Renderers)
                    target.gameObject.layer = 31;

            }
            var pixels = LinearRenderCapture.Capture(
                camera,
                PathUtils.Combine(directory, useBuiltShader ? "built-actual.png" : label + "-actual.png"),
                256,
                256);
            var darkPixels = pixels.Count(pixel => pixel.maxColorComponent < 0.8f);
            Assert.That(darkPixels, Is.GreaterThan(1000), "The opaque concrete tower must occlude the bright background.");

            var previousFog = Shader.IsKeywordEnabled("BLOOM_FOG");
            var previousTexture = Shader.GetGlobalTexture("_BloomPrePassTexture");
            var previousAttenuation = Shader.GetGlobalFloat("_CustomFogAttenuation");
            var previousOffset = Shader.GetGlobalFloat("_CustomFogOffset");
            var originalMaterial = renderer.sharedMaterial;
            Material controlMaterial = null;
            try
            {
                Shader.DisableKeyword("BLOOM_FOG");
                var clear = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "fog-off.png"), 256, 256);
                Shader.EnableKeyword("BLOOM_FOG");
                Shader.SetGlobalTexture("_BloomPrePassTexture", Texture2D.whiteTexture);
                Shader.SetGlobalFloat("_CustomFogAttenuation", 0.1f);
                Shader.SetGlobalFloat("_CustomFogOffset", 0f);
                var fogged = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "fog-on.png"), 256, 256);
                var changed = CountChangedPixels(clear, fogged);
                Assert.That(
                    changed,
                    Is.LessThanOrEqualTo(4),
                    "The source map's concrete scaffolding stays opaque and does not acquire distance fog in-game.");

                controlMaterial = new Material(originalMaterial)
                {
                    shaderKeywords = new[] { "DIFFUSE", "FOG", "MULTIPLY_REFLECTIONS", "REFLECTION_PROBE_BOX_PROJECTION" }
                };
                renderer.sharedMaterial = controlMaterial;
                var control = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "valid-fog-control.png"), 256, 256);
                var controlChanged = CountChangedPixels(fogged, control);
                File.WriteAllText(
                    PathUtils.Combine(directory, "metrics.json"),
                    new JSONObject
                    {
                        ["scaffoldingPixels"] = darkPixels, ["fogChangedPixels"] = changed,
                        ["supportedFogControlChangedPixels"] = controlChanged,
                        ["linearRgbaDistanceThreshold"] = 0.01f
                    }.ToString(2));
                Assert.That(controlChanged, Is.GreaterThan(1000), "A retained fog-enabled variant must still respond to distance fog.");
            }
            finally
            {
                renderer.sharedMaterial = originalMaterial;
                if (controlMaterial != null)
                    Object.DestroyImmediate(controlMaterial);

                if (previousFog)
                    Shader.EnableKeyword("BLOOM_FOG");
                else
                    Shader.DisableKeyword("BLOOM_FOG");

                Shader.SetGlobalTexture("_BloomPrePassTexture", previousTexture);
                Shader.SetGlobalFloat("_CustomFogAttenuation", previousAttenuation);
                Shader.SetGlobalFloat("_CustomFogOffset", previousOffset);
            }
        }

        private static int CountChangedPixels(Color[] first, Color[] second)
        {
            var changed = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Vector4.Distance(first[index], second[index]) > 0.01f)
                    changed++;

            }
            return changed;
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

            if (captureObject != null)
                Object.DestroyImmediate(captureObject);

            foreach (var material in builtMaterials)
                Object.DestroyImmediate(material);

            builtMaterials.Clear();
            if (shaderBundle != null)
                shaderBundle.Unload(true);

            useBuiltShader = false;
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }

    public class ChangeOfSceneShaderBundleSetup : IPrebuildSetup
    {
        public void Setup()
        {
            var output = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "ChangeOfSceneScaffolding", "shader-bundle");
            Directory.CreateDirectory(output);
            NativeSimpleLitVariantBuilder.UpdateVariants();
            BuildPipeline.BuildAssetBundles(
                output,
                new[]
                {
                    new AssetBundleBuild
                    {
                        assetBundleName = "concrete",
                        assetNames = new[] { "Assets/_Graphics/Materials/Environments/Interscope/Concrete2.mat", NativeSimpleLitVariantBuilder.VariantsPath }
                    }
                },
                BuildAssetBundleOptions.ForceRebuildAssetBundle,
                EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
