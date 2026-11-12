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
    public class BimboLimboSmokeParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChroma;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChroma = Settings.Instance.EmulateChromaLite;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator OriginalMapSmokeUsesTheNativeSeedAndReturnsAfterScrubs()
        {
            const string sourcePath = @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\Chroma\35742 (Bimbo Limbo - Jabob & Elecast)\ExpertPlusStandard.dat";
            if (!File.Exists(sourcePath))
            {
                Assert.Ignore("Requires the original local Bimbo Limbo map.");
            }
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(2, JSON.Parse(File.ReadAllText(sourcePath)),
                beatsPerMinute: 202f, environmentName: "BigMirrorEnvironment", songLengthSeconds: 300,
                forceSceneReload: true, difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 19f, NoteStartBeatOffset = -0.4f,
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
            var camera = cameras.SelectedCameraController.Camera;
            clock.MoveToJsonTime(20f);
            yield return null;
            yield return null;
            var smoke = Object.FindAnyObjectByType<EnvironmentDescriptor>()
                .GetComponentsInChildren<ParticleSystem>(true).Single(value => value.name == "BigSmokePS");
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "BimboLimboSmoke", "cm-original-map");
            Directory.CreateDirectory(directory);
            var previousRandom = Shader.GetGlobalFloat("_GlobalRandomValue");
            Camera.CameraCallback stabilizeNoise = _ => Shader.SetGlobalFloat("_GlobalRandomValue", 0f);
            Camera.onPreRender += stabilizeNoise;
            try
            {
                LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "beat20.png"));
                var particles = ReadParticles(smoke);
                var position = camera.transform.position;
                var rotation = camera.transform.rotation;
                var fov = camera.fieldOfView;
                // Native snapshot 20261022T232814007 records this song time and main-camera pose.
                clock.MoveToTimeInSeconds(5.74396467f);
                camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
                camera.fieldOfView = 100f;
                LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "native-camera-720p.png"));
                LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "native-camera.png"), 2560, 1440);
                File.WriteAllText(PathUtils.Combine(directory, "native-time-smoke.json"),
                    ReadSnapshot(smoke, clock, camera).ToString());
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.fieldOfView = fov;
                clock.MoveToJsonTime(20f);
                File.WriteAllText(PathUtils.Combine(directory, "smoke.json"), ReadSnapshot(smoke, clock, camera).ToString());
                Assert.That(smoke.randomSeed, Is.EqualTo(0u));
                foreach (var beat in new[] { 20f + 1f / 64f, 20f, 20f - 1f / 64f, 20f, 25f, 20f })
                {
                    clock.MoveToJsonTime(beat);
                    yield return null;
                }
                Assert.That(ReadParticles(smoke).ToString(), Is.EqualTo(particles.ToString()));

            }
            finally
            {
                Camera.onPreRender -= stabilizeNoise;
                Shader.SetGlobalFloat("_GlobalRandomValue", previousRandom);
            }
        }

        private static JSONObject ReadSnapshot(ParticleSystem system, AudioTimeSyncController time, Camera camera)
        {
            return new JSONObject
            {
                ["songTime"] = time.CurrentSeconds,
                ["beat"] = time.CurrentJsonTime,
                ["systemTime"] = system.time,
                ["seed"] = system.randomSeed.ToString(),
                ["cameraPosition"] = camera.transform.position.ToString("R"),
                ["cameraRotation"] = camera.transform.eulerAngles.ToString("R"),
                ["verticalFov"] = camera.fieldOfView,
                ["particles"] = ReadParticles(system)
            };
        }

        private static JSONArray ReadParticles(ParticleSystem system)
        {
            var particles = new ParticleSystem.Particle[system.particleCount];
            system.GetParticles(particles);
            var result = new JSONArray();
            foreach (var particle in particles)
            {
                result.Add(new JSONObject
                {
                    ["seed"] = particle.randomSeed.ToString(),
                    ["position"] = particle.position.ToString("R"),
                    ["rotation"] = particle.rotation3D.ToString("R"),
                    ["size"] = particle.startSize3D.ToString("R"),
                    ["remainingLifetime"] = particle.remainingLifetime
                });
            }
            return result;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
            {
                clock.enabled = previousClockEnabled;
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChroma;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
