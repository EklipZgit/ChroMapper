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
    public class HeliovDistantNoteTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private string previousNoteModels;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousNoteModels = Settings.Instance.NoteModels;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator IncomingNoteBodyRemainsVisibleDuringPreviewScrubs() => CheckIncomingNote(false);

        [UnityTest]
        public IEnumerator IncomingNoteBodyRemainsVisibleDuringPlayback() => CheckIncomingNote(true);

        private IEnumerator CheckIncomingNote(bool playback)
        {
            // Heliov Expert+ Lawless's b14 note uses a 30-beat offset and an 800-metre incoming path.
            // Keep its player parent stationary so only the note's distance and animation affect this capture.
            Settings.Instance.Animations = true;
            Settings.ApplyOptionByName(nameof(Settings.NoteModels), "Standard");
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 14f,
                NoteStartBeatOffset = -0.5f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                CustomColorLeft = new Color(0.465f, 0.465f, 0.465f),
                CustomColorRight = new Color(0.552f, 0.694f, 0.377f),
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(PathUtils.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    "HeliovDistantNoteFixture.json"))),
                beatsPerMinute: 150f,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 230,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Playing);
            var failures = new List<string>();
            var directory = CaptureDirectory();
            mode.SetUIMode(playback ? UIModeType.Playing : UIModeType.Preview, false);
            if (playback)
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 9.5f);
            }
            var beats = playback
                ? new[] { 10f, 10.5f, 11f, 11.5f, 12f, 13f }
                : new[] { 10f, 10.5f, 11f, 13f, 10f, 10f - 1f / 64f, 10f + 1f / 64f };
            foreach (var beat in beats)
            {
                if (playback)
                {
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                }
                else
                {
                    clock.MoveToJsonTime(beat);
                }
                yield return null;
                yield return null;
                var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single();
                note.Animator.ApplyAnimation();
                var body = Capture(note.ModelController.Renderers, PathUtils.Combine(directory, $"{playback}-{beat}-body.png"));
                var arrow = Capture(note.ArrowMpbController.Renderers, PathUtils.Combine(directory, $"{playback}-{beat}-arrow.png"));
                Assert.That(arrow, Is.GreaterThan(0.01f), $"The fixture's arrow must be visible at b{beat}.");
                if (body < 0.01f)
                {
                    failures.Add($"b{beat}: body disappeared while arrow remains visible, body={body}, arrow={arrow}");
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [TestCase("CM_Note")]
        [TestCase("CM_Note_Chain")]
        public void BuiltinBodyRetainsVisiblePixelsAtDistance(string modelName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_Prefabs/MapEditor/Visuals/{modelName}.prefab");
            var model = Object.Instantiate(prefab);
            try
            {
                var renderers = model.GetComponentsInChildren<Renderer>().ToList();
                foreach (var renderer in renderers)
                {
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_Color", Color.white);
                    block.SetFloat("_AnimationSpawned", 1f);
                    renderer.SetPropertyBlock(block);
                }
                foreach (var distance in new[] { 5f, 60f })
                {
                    model.transform.position = new Vector3(0f, 0f, distance);
                    Assert.That(
                        Capture(renderers, PathUtils.Combine(CaptureDirectory(), $"{modelName}-{distance}.png")),
                        Is.GreaterThan(0.01f),
                        $"{modelName} body must remain visible at {distance} metres.");
                }
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        private static string CaptureDirectory()
        {
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "HeliovDistantNote");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static float Capture(List<Renderer> renderers, string path)
        {
            var layers = renderers.Select(renderer => renderer.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Distant note capture camera");
            var camera = cameraObject.AddComponent<Camera>();
            try
            {
                // Rendering cloned meshes would bypass the LODGroup and miss this distance-culling regression.
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    renderer.gameObject.layer = 30;
                }
                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.fieldOfView = 90f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
                var pixels = LinearRenderCapture.Capture(camera, path, 1280, 720);
                return pixels.Max(pixel => pixel.maxColorComponent);
            }
            finally
            {
                for (var index = 0; index < renderers.Count; index++)
                {
                    renderers[index].gameObject.layer = layers[index];
                }
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(clock);
                }
                clock.enabled = previousClockEnabled;
            }
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
            }
            if (cameras != null)
            {
                cameras.SelectCamera(CameraType.Editing);
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.ApplyOptionByName(nameof(Settings.NoteModels), previousNoteModels);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
