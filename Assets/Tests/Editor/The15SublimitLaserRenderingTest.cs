using System.Collections;
using System.Collections.Generic;
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
    public class The15SublimitLaserRenderingTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousPlayerOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private Camera editingCamera;
        private Vector3 previousEditingPosition;
        private Quaternion previousEditingRotation;
        private float previousEditingFov;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousPlayerOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator MatchedCameraKeepsCenterLaserColorAcrossPlayingAndPreview()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            // Sublimit (1c8c0) overlays four BTS SideLasers at beat 281.25. Keep their
            // enhancement, light events and fog track without the unrelated notes and walls.
            var fixturePath = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "The15SublimitLaser281Fixture.json");
            yield return TestUtils.ReloadMap(2, JSON.Parse(File.ReadAllText(fixturePath)),
                beatsPerMinute: 120f, songLengthSeconds: 320, environmentName: "BTSEnvironment",
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 20f, NoteStartBeatOffset = 0f,
                    CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                    CustomColorLeft = new Color(0.666f, 0f, 0f),
                    CustomColorRight = new Color(0.269f, 0.269f, 0.269f),
                    CustomEnvColorLeft = new Color(0.924f, 0f, 0f),
                    CustomEnvColorRight = new Color(0.042f, 0.512f, 0.898f)
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(281.25f);
            yield return null;
            yield return null;
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "SublimitLaser281");
            Directory.CreateDirectory(directory);
            var playingCamera = cameras.SelectedCameraController.Camera;
            LinearRenderCapture.Capture(playingCamera, PathUtils.Combine(directory, "full-playing.png"), 1920, 1080, 4, true);
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var lasers = context.Descriptor.ChromaIDMarkers
                .Where(marker => marker.name == "SideLaser")
                .Select(marker => marker.GetComponent<ParametricBloomFogLightController>()).ToArray();
            Assert.That(lasers.Length, Is.EqualTo(4));
            var renderers = lasers.SelectMany(laser => laser.GetComponentsInChildren<Renderer>()).ToArray();
            var layers = renderers.Select(renderer => renderer.gameObject.layer).ToArray();
            var playingMask = playingCamera.cullingMask;
            var playingFov = playingCamera.fieldOfView;
            var lights = BloomFogObject.AllBloomFogLights;
            var randomValue = Shader.GetGlobalFloat("_GlobalRandomValue");
            // Dithering changes each frame. Pin its sample while comparing camera targets.
            Camera.onPreRender += FreezeDithering;
            var report = new JSONObject();
            try
            {
                foreach (var renderer in renderers)
                    renderer.gameObject.layer = 30;
                BloomFogObject.AllBloomFogLights = lasers.Select(laser => laser.BloomFog).ToList();
                playingCamera.cullingMask = 1 << 30;
                playingCamera.fieldOfView = 100f;
                var position = playingCamera.transform.position;
                var rotation = playingCamera.transform.rotation;
                report["playing"] = State(lasers, playingCamera);
                var playing = LinearRenderCapture.Capture(playingCamera, PathUtils.Combine(directory, "isolated-playing.png"), 1920, 1080, 4, true);
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
                mode.SetUIMode(UIModeType.Preview, false);
                cameras.SelectCamera(CameraType.Editing);
                clock.MoveToJsonTime(281.25f);
                yield return null;
                yield return null;
                editingCamera = cameras.SelectedCameraController.Camera;
                previousEditingPosition = editingCamera.transform.position;
                previousEditingRotation = editingCamera.transform.rotation;
                previousEditingFov = editingCamera.fieldOfView;
                var editingMask = editingCamera.cullingMask;
                try
                {
                    editingCamera.transform.SetPositionAndRotation(position, rotation);
                    editingCamera.fieldOfView = 100f;
                    editingCamera.cullingMask = 1 << 30;
                    report["preview"] = State(lasers, editingCamera);
                    var preview = LinearRenderCapture.Capture(editingCamera, PathUtils.Combine(directory, "isolated-preview.png"), 1920, 1080, 4, true);
                    var error = 0f;
                    var lit = 0;
                    for (var y = 700; y < 950; y++)
                    {
                        for (var x = 930; x < 990; x++)
                        {
                            var index = y * 1920 + x;
                            var a = playing[index];
                            var b = preview[index];
                            if (a.r > 0.03f || b.r > 0.03f)
                                lit++;
                            error = Mathf.Max(error, Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));
                        }
                    }
                    report["maximumError"] = error;
                    report["litPixels"] = lit;
                    File.WriteAllText(PathUtils.Combine(directory, "state.json"), report.ToString(2));
                    Assert.That(lit, Is.GreaterThan(100));
                    Assert.That(playingCamera.allowHDR, Is.False);
                    Assert.That(editingCamera.allowHDR, Is.False,
                        "Native gameplay, smooth and recorder cameras use LDR; bloom alpha must clamp before MSAA resolve in both preview modes.");
                    Assert.That(error, Is.LessThan(0.003f), "The same center beam must retain its color with a matched camera after changing preview modes.");
                }
                finally
                {
                    editingCamera.cullingMask = editingMask;
                }
            }
            finally
            {
                Camera.onPreRender -= FreezeDithering;
                Shader.SetGlobalFloat("_GlobalRandomValue", randomValue);
                BloomFogObject.AllBloomFogLights = lights;
                playingCamera.cullingMask = playingMask;
                playingCamera.fieldOfView = playingFov;
                for (var index = 0; index < renderers.Length; index++)
                    renderers[index].gameObject.layer = layers[index];
            }
        }

        private static void FreezeDithering(Camera camera) => Shader.SetGlobalFloat("_GlobalRandomValue", 0f);

        private static JSONObject State(ParametricBloomFogLightController[] lasers, Camera camera)
        {
            var states = new JSONArray();
            foreach (var laser in lasers)
            {
                var block = new MaterialPropertyBlock();
                laser.BoxLight.Renderer.GetPropertyBlock(block);
                var mesh = laser.BoxLight.GetComponent<MeshFilter>().sharedMesh;
                states.Add(new JSONObject
                {
                    ["name"] = laser.GetComponent<ChromaIDMarker>().ChromaID,
                    ["color"] = laser.Color.ToString("F6"),
                    ["coreColor"] = block.GetColor("_Color").ToString("F6"),
                    ["meshBounds"] = mesh.bounds.ToString("F6"),
                    ["position"] = laser.transform.position.ToString("F6"),
                    ["scale"] = laser.BoxLight.transform.localScale.ToString("F6"),
                    ["coreShader"] = laser.BoxLight.Renderer.sharedMaterial.shader.name,
                    ["glowShader"] = laser.SpriteLight.Renderer.sharedMaterial.shader.name
                });
            }
            return new JSONObject
            {
                ["position"] = camera.transform.position.ToString("F6"),
                ["rotation"] = camera.transform.rotation.eulerAngles.ToString("F6"),
                ["fov"] = camera.fieldOfView,
                ["msaa"] = QualitySettings.antiAliasing,
                ["hdr"] = camera.allowHDR,
                ["lasers"] = states
            };
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);
                clock.enabled = previousClockEnabled;
            }
            if (editingCamera != null)
            {
                editingCamera.transform.SetPositionAndRotation(previousEditingPosition, previousEditingRotation);
                editingCamera.fieldOfView = previousEditingFov;
            }
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
