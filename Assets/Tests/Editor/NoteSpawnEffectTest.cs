using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEditor;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class NoteSpawnEffectTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private Camera captureCamera;
        private Camera originalMapCamera;
        private Vector3 previousCameraPosition;
        private Quaternion previousCameraRotation;
        private float previousCameraFov;
        private bool previousHideSpawnEffect;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousHideSpawnEffect = Settings.Instance.HideNoteSpawnEffect;
            Settings.Instance.HideNoteSpawnEffect = false;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator SpawnEffectsOnlyAppearInPlayingAndPreviewAndRespectTheLiveSetting()
        {
            Settings.ApplyOptionByName(nameof(Settings.HideNoteSpawnEffect), false);
            yield return LoadFixture();
            foreach (var uiMode in new[] { UIModeType.Preview, UIModeType.Normal, UIModeType.Playing, UIModeType.HideUI, UIModeType.Preview, UIModeType.HideGrids })
            {
                mode.SetUIMode(uiMode, false);
                clock.MoveToJsonTime(29.812f);
                yield return null;
                yield return null;
                var expected = uiMode is UIModeType.Preview or UIModeType.Playing;
                var effects = Object.FindObjectsByType<NoteSpawnEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(effects.Any(effect => effect.SpriteRenderer.enabled), Is.EqualTo(expected), $"Sprite in {uiMode}");
                Assert.That(effects.Any(effect => effect.BloomFog.gameObject.activeInHierarchy), Is.EqualTo(expected), $"Fog in {uiMode}");
            }
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.812f);
            yield return null;
            foreach (var hide in new[] { true, false, true })
            {
                Settings.ApplyOptionByName(nameof(Settings.HideNoteSpawnEffect), hide);
                yield return null;
                yield return null;
                var effects = Object.FindObjectsByType<NoteSpawnEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(effects.Any(effect => effect.SpriteRenderer.enabled), Is.EqualTo(!hide));
                Assert.That(effects.Any(effect => effect.BloomFog.gameObject.activeInHierarchy), Is.EqualTo(!hide));
            }
        }

        [UnityTest]
        public IEnumerator VoidwalkersSpawnLightRendersAboveTheNotesAtBeat29812()
        {
            yield return LoadFixture();
            var failures = new List<string>();
            foreach (var previewMode in new[] { UIModeType.Preview, UIModeType.Playing })
            {
                mode.SetUIMode(previewMode, false);
                clock.MoveToJsonTime(29.812f);
                yield return null;
                yield return null;
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "NoteSpawnEffect");
                Directory.CreateDirectory(directory);
                var pixels = LinearRenderCapture.Capture(
                    captureCamera,
                    PathUtils.Combine(directory, $"Voidwalkers-{previewMode}.png"),
                    1280,
                    720);
                foreach (NoteContainer note in Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values)
                    note.SpawnEffect.Hide();

                var baseline = LinearRenderCapture.Capture(
                    captureCamera,
                    PathUtils.Combine(directory, $"Voidwalkers-{previewMode}-disabled.png"),
                    1280,
                    720);
                var litPixels = 0;
                for (var y = 280; y < 650; y++)
                {
                    for (var x = 540; x < 740; x++)
                    {
                        var difference = pixels[y * 1280 + x] - baseline[y * 1280 + x];
                        if (Mathf.Abs(difference.r) + Mathf.Abs(difference.g) + Mathf.Abs(difference.b) > 0.003f)
                            litPixels++;
                    }
                }
                if (litPixels < 20)
                    failures.Add($"{previewMode}: the native spawn light above the notes rendered only {litPixels} pixels.");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [TestCase("spawnEffect", true, true)]
        [TestCase("spawnEffect", false, false)]
        [TestCase("_disableSpawnEffect", true, false)]
        [TestCase("_disableSpawnEffect", false, true)]
        public void PerNoteFlagsOverrideTheLevelHideSetting(string key, bool flag, bool expected)
        {
            var note = new BaseNote { CustomData = new JSONObject { [key] = flag } };
            foreach (var hide in new[] { false, true })
            {
                var settings = new JSONObject
                {
                    ["_settings"] = new JSONObject
                    {
                        ["_playerOptions"] = new JSONObject { ["_hideNoteSpawnEffect"] = hide }
                    }
                };
                Assert.That(NoteSpawnEffect.IsEnabled(note, settings, null), Is.EqualTo(expected));
                Assert.That(NoteSpawnEffect.IsEnabled(note, null, settings), Is.EqualTo(expected));
            }
        }

        [Test]
        public void DefaultsAndModernFlagPrecedenceMatchChroma()
        {
            var note = new BaseNote();
            Assert.That(NoteSpawnEffect.IsEnabled(note, null, null), Is.True);
            var hidden = JSON.Parse("{\"_settings\":{\"_playerOptions\":{\"_hideNoteSpawnEffect\":true}}}");
            Assert.That(NoteSpawnEffect.IsEnabled(note, null, hidden), Is.False);
            Assert.That(NoteSpawnEffect.IsEnabled(note, hidden, null), Is.False);
            var shown = JSON.Parse("{\"_settings\":{\"_playerOptions\":{\"_hideNoteSpawnEffect\":false}}}");
            Assert.That(NoteSpawnEffect.IsEnabled(note, shown, hidden), Is.True);
            Settings.Instance.HideNoteSpawnEffect = true;
            Assert.That(NoteSpawnEffect.IsEnabled(note, null, null), Is.False);
            Assert.That(NoteSpawnEffect.IsEnabled(note, shown, null), Is.True);
            Assert.That(NoteSpawnEffect.IsEnabled(note, null, shown), Is.True);
            note.CustomData = new JSONObject { ["spawnEffect"] = true, ["_disableSpawnEffect"] = true };
            Assert.That(NoteSpawnEffect.IsEnabled(note, hidden, hidden), Is.True);
            note.CustomData = new JSONObject { ["spawnEffect"] = false, ["_disableSpawnEffect"] = false };
            Assert.That(NoteSpawnEffect.IsEnabled(note, shown, shown), Is.False);
        }

        [UnityTest]
        public IEnumerator CurvesAndWorldAnchorMatchNativeDuringSeeksAndPlayback()
        {
            yield return LoadFixture();
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.812f);
            yield return null;
            yield return null;
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var note = (NoteContainer)grid.LoadedContainers.Single(pair => pair.Key.JsonTime == 31f).Value;
            var effect = note.SpawnEffect;
            var reference = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "NoteSpawnEffectNativeReference.json")));
            Assert.That(note.Animator.Context.Descriptor.BloomFogParams.NoteSpawnIntensity, Is.EqualTo(0.6f));
            Assert.That(effect.BloomFog.StartWidth, Is.EqualTo(reference["fogStartWidth"].AsFloat));
            Assert.That(effect.BloomFog.EndWidth, Is.EqualTo(reference["fogEndWidth"].AsFloat));
            var anchor = effect.transform.position;
            var birth = effect.SpawnSeconds;
            var phases = reference["samples"].Children.ToArray();
            foreach (var playback in new[] { false, true })
            {
                if (playback)
                    TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, clock.GetBeatFromSeconds(birth));

                foreach (var sample in phases)
                {
                    var beat = clock.GetBeatFromSeconds(birth + sample["progress"].AsFloat * 0.4f);
                    if (playback)
                        TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                    else
                        clock.MoveToSongBpmTime(beat);

                    yield return null;
                    yield return null;
                    Assert.That(effect.SpriteRenderer.enabled, Is.True);
                    Assert.That(effect.BloomFog.gameObject.activeInHierarchy, Is.True);
                    Assert.That(Vector3.Distance(effect.transform.position, anchor), Is.LessThan(0.0001f));
                    Assert.That(effect.SpriteRenderer.color.r, Is.EqualTo(0f).Within(0.0001f));
                    Assert.That(effect.SpriteRenderer.color.g, Is.EqualTo(0.529f * 0.6f).Within(0.0001f));
                    Assert.That(effect.SpriteRenderer.color.b, Is.EqualTo(0.6f).Within(0.0001f));
                    Assert.That(effect.SpriteRenderer.color.a, Is.EqualTo(sample["transparencyCurve"].AsFloat).Within(0.0001f));
                    Assert.That(effect.SpriteRenderer.transform.localScale.x, Is.EqualTo(sample["spriteXScaleCurve"].AsFloat).Within(0.0001f));
                    Assert.That(effect.SpriteRenderer.transform.localScale.y, Is.EqualTo(sample["spriteYScaleCurve"].AsFloat).Within(0.0001f));
                    var quads = new BloomfogQuad[1];
                    var count = 0;
                    effect.BloomFog.ApplyToQuad(
                        ref count,
                        quads,
                        captureCamera.worldToCameraMatrix,
                        Matrix4x4.Perspective(130f, 1f, 0.01f, 100f),
                        0.02f);
                    Assert.That(count, Is.EqualTo(1));
                    var intensity = sample["lightIntensityCurve"].AsFloat * reference["fogIntensityMultiplier"].AsFloat;
                    Assert.That(quads[0].Vertex0Color.a, Is.EqualTo(Mathf.LinearToGammaSpace(intensity)).Within(0.0001f));
                }
                if (playback)
                    TestUtils.PauseDeterministicPlayback(clock);

                foreach (var elapsed in new[] { -0.01f, 0.41f, 0.12f, -0.01f, 0.12f })
                {
                    clock.MoveToSongBpmTime(clock.GetBeatFromSeconds(birth + elapsed));
                    yield return null;
                    yield return null;
                    Assert.That(effect.SpriteRenderer.enabled, Is.EqualTo(elapsed >= 0f && elapsed <= 0.4f));
                    Assert.That(effect.BloomFog.gameObject.activeInHierarchy, Is.EqualTo(effect.SpriteRenderer.enabled));
                }
            }
        }

        [UnityTest]
        public IEnumerator SpawnFogProducesASurroundingGlowInThePlayerCamera()
        {
            yield return LoadFixture();
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.812f);
            yield return null;
            yield return null;
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var effects = grid.LoadedContainers.Values.OfType<NoteContainer>()
                .Select(note => note.SpawnEffect).Where(effect => effect.BloomFog.gameObject.activeInHierarchy).ToArray();
            Assert.That(effects, Is.Not.Empty);
            foreach (var light in BloomFogObject.AllBloomFogLights.ToArray())
            {
                if (!effects.Any(effect => effect.BloomFog == light))
                    light.SetColor(Color.clear);
            }
            foreach (var effect in effects)
            {
                Assert.That(BloomFogObject.AllBloomFogLights, Does.Contain(effect.BloomFog));
                effect.SpriteRenderer.enabled = false;
            }
            var camera = cameras.PlayerCameraController.Camera;
            originalMapCamera = camera;
            previousCameraPosition = camera.transform.position;
            previousCameraRotation = camera.transform.rotation;
            previousCameraFov = camera.fieldOfView;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
            camera.fieldOfView = 90f;
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "NoteSpawnEffect");
            Directory.CreateDirectory(directory);
            var enabled = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "fog-composite-enabled.png"));
            foreach (var effect in effects)
                effect.BloomFog.gameObject.SetActive(false);

            var disabled = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "fog-composite-disabled.png"));
            var glowPixels = 0;
            var maximumDifference = 0f;
            for (var y = 300; y < 550; y++)
            {
                for (var x = 560; x < 720; x++)
                {
                    var difference = enabled[y * 1280 + x] - disabled[y * 1280 + x];
                    var brightness = Mathf.Max(difference.r, difference.g, difference.b);
                    maximumDifference = Mathf.Max(maximumDifference, brightness);
                    if (brightness > 0.003f && Mathf.Abs(x - 640) > 10)
                        glowPixels++;
                }
            }
            Assert.That(glowPixels, Is.GreaterThan(100),
                $"The spawn fog must light a broad area around the thin sprite in the final player image. Maximum RGB difference: {maximumDifference}.");
        }

        [UnityTest]
        public IEnumerator NativeFogLightProducesPrepassPixels()
        {
            yield return LoadFixture();
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.812f);
            yield return null;
            yield return null;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single(value => value.NoteData.JsonTime == 31f);
            var controller = Object.FindAnyObjectByType<BloomfogRenderingController>();
            var globals = controller.CaptureGlobalState();
            var previousLights = BloomFogObject.AllBloomFogLights;
            var renderer = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BloomfogRendererSO>(
                "Assets/__Scripts/Graphics/Bloomfog/BloomfogRendererSO.asset"));
            var target = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(512, 512, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            try
            {
                BloomFogObject.AllBloomFogLights = new List<BloomFogObject> { note.SpawnEffect.BloomFog };
                renderer.Initialize();
                renderer.RenderToTexture(captureCamera, target, out _);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels().Count(value => value.maxColorComponent > 0.003f), Is.GreaterThan(20));
            }
            finally
            {
                BloomFogObject.AllBloomFogLights = previousLights;
                RenderTexture.active = previousTarget;
                controller.RestoreGlobalState(globals);
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(renderer);
            }
        }

        [UnityTest]
        [Explicit("Requires the original local Voidwalkers ExpertPlusStandard difficulty.")]
        public IEnumerator CaptureOriginalVoidwalkersAtBeat29812()
        {
            yield return LoadFixture(true);
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.812f);
            yield return null;
            yield return null;
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "NoteSpawnEffect");
            Directory.CreateDirectory(directory);
            var camera = cameras.PlayerCameraController.Camera;
            cameras.SelectCamera(CameraType.Playing);
            originalMapCamera = camera;
            previousCameraPosition = camera.transform.position;
            previousCameraRotation = camera.transform.rotation;
            previousCameraFov = camera.fieldOfView;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
            camera.fieldOfView = 90f;
            var enabledPixels = LinearRenderCapture.Capture(
                camera,
                PathUtils.Combine(directory, "Voidwalkers-full-map-enabled.png"),
                2560,
                1440);
            foreach (NoteContainer note in Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values)
                note.SpawnEffect.Hide();

            var disabledPixels = LinearRenderCapture.Capture(
                camera,
                PathUtils.Combine(directory, "Voidwalkers-full-map-disabled.png"),
                2560,
                1440);
            var changedPixels = 0;
            for (var index = 0; index < enabledPixels.Length; index++)
            {
                var difference = enabledPixels[index] - disabledPixels[index];
                if (Mathf.Abs(difference.r) + Mathf.Abs(difference.g) + Mathf.Abs(difference.b) > 0.003f)
                    changedPixels++;
            }
            File.WriteAllText(PathUtils.Combine(directory, "full-map-comparison.json"), $"{{\"changedPixels\":{changedPixels}}}");
            Assert.That(changedPixels, Is.GreaterThan(20));
        }

        [UnityTest]
        public IEnumerator NoodleEffectKeepsTheJumpStartPoseWhileItsParentMoves()
        {
            yield return LoadFixture(noodle: true);
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.5f);
            yield return null;
            yield return null;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single(value => value.NoteData.JsonTime == 31f);
            var birth = clock.GetSecondsFromBeat(note.NoteData.SpawnSongBpmTime);
            clock.MoveToTimeInSeconds(birth);
            yield return null;
            yield return null;
            // AnimationTarget is CM's moving note transform. The container above it excludes Noodle offsets.
            var anchor = note.Animator.LocalTarget.position - Vector3.up * 0.15f;
            var rotation = note.Animator.LocalTarget.rotation;
            Assert.That(Vector3.Distance(note.SpawnEffect.transform.position, anchor), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(note.SpawnEffect.transform.rotation, rotation), Is.LessThan(0.01f));
            clock.MoveToTimeInSeconds(birth + 0.12f);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(note.SpawnEffect.transform.position, anchor), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(note.Animator.LocalTarget.position, anchor), Is.GreaterThan(0.5f));
        }

        [UnityTest]
        public IEnumerator NormalCeilingSelectsTheNativeTallEffect()
        {
            yield return LoadFixture();
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToJsonTime(29.812f);
            yield return null;
            yield return null;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single(value => value.NoteData.JsonTime == 31f);
            clock.MoveToTimeInSeconds(note.SpawnEffect.SpawnSeconds + 0.12f);
            yield return null;
            yield return null;
            var sample = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "NoteSpawnEffectNativeReference.json")))["normalSample"];
            var context = note.Animator.Context;
            var previousCeiling = context.Descriptor.SizeData.CeilingType;
            try
            {
                context.Descriptor.SizeData.CeilingType = CeilingType.NoCeiling;
                note.SpawnEffect.Bind(note, context);
                note.SpawnEffect.Evaluate();
                Assert.That(
                    note.SpawnEffect.SpriteRenderer.transform.localScale.y,
                    Is.EqualTo(sample["spriteYScaleCurve"].AsFloat).Within(0.0001f));
                Assert.That(
                    note.SpawnEffect.SpriteRenderer.color.a,
                    Is.EqualTo(sample["transparencyCurve"].AsFloat).Within(0.0001f));
                Assert.That(note.SpawnEffect.BloomFog.IntensityMultiplier, Is.EqualTo(0.5f));
                Assert.That(note.SpawnEffect.BloomFog.LightType.RenderingPriority, Is.EqualTo(1));
            }
            finally
            {
                context.Descriptor.SizeData.CeilingType = previousCeiling;
                note.SpawnEffect.Bind(note, context);
            }
        }

        private IEnumerator LoadFixture(bool originalMap = false, bool noodle = false)
        {
            Settings.Instance.Animations = true;
            var path = originalMap
                ? @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\CustomLevels\50033 (Voidwalkers - Nallak, Kinz, Uzi, Sonoluminn)\ExpertPlusStandard.dat"
                : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "VoidwalkersNoteSpawnFixture.json");
            if (!File.Exists(path))
                Assert.Ignore("Requires the original local Voidwalkers map.");

            var map = JSON.Parse(File.ReadAllText(path));
            if (noodle)
            {
                map["colorNotes"][0]["customData"] = JSON.Parse(
                    "{\"track\":\"child\",\"animation\":{\"offsetPosition\":[2,1,5],\"offsetWorldRotation\":[0,30,0]}}");
                map["customData"] = JSON.Parse(
                    "{\"customEvents\":[{\"b\":0,\"t\":\"AssignTrackParent\",\"d\":{\"childrenTracks\":[\"child\"],\"parentTrack\":\"parent\"}},{\"b\":0,\"t\":\"AnimateTrack\",\"d\":{\"track\":\"parent\",\"duration\":100,\"position\":[[10,0,0,0],[30,0,0,1]]}}]}");
            }

            yield return TestUtils.ReloadMap(
                3,
                map,
                beatsPerMinute: 142f,
                songLengthSeconds: originalMap ? 300 : 40,
                environmentName: "Monstercat2Environment",
                difficultyInfo: new Beatmap.Info.InfoDifficulty(new Beatmap.Info.InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 18f,
                    NoteStartBeatOffset = -0.5f,
                    CustomSuggestions = new List<string> { "ChromaGLS" },
                    CustomRequirements = noodle ? new List<string> { "Noodle Extensions" } : new List<string>(),
                    CustomColorLeft = new Color(1f, 0.498f, 0f),
                    CustomColorRight = new Color(0f, 0.529f, 1f),
                    CustomEnvColorLeft = new Color(0.902f, 0.486f, 0.325f),
                    CustomEnvColorRight = new Color(0.4f, 0.718f, 1f),
                    CustomEnvColorBoostLeft = new Color(1f, 0.235f, 0.596f),
                    CustomEnvColorBoostRight = new Color(0.298f, 0.51f, 1f)
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Playing);
            captureCamera = new GameObject("Note spawn test camera").AddComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.cullingMask = 1 << LayerMask.NameToLayer("Beatmap Object");
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.black;
            captureCamera.fieldOfView = 90f;
            captureCamera.transform.position = new Vector3(0f, 1.7f, 0f);
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (captureCamera != null)
                Object.DestroyImmediate(captureCamera.gameObject);

            if (originalMapCamera != null)
            {
                originalMapCamera.transform.SetPositionAndRotation(previousCameraPosition, previousCameraRotation);
                originalMapCamera.fieldOfView = previousCameraFov;
                originalMapCamera = null;
            }

            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.ApplyOptionByName(nameof(Settings.HideNoteSpawnEffect), previousHideSpawnEffect);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
