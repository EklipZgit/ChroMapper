using System.Collections;
using System.Linq;
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

        // BigMirror's smoke is environment content, so a minimal map reproduces the original
        // Bimbo Limbo regression: an explicit zero seed and identical particles after scrubs.
        [UnityTest]
        public IEnumerator SmokeUsesTheNativeSeedAndReturnsAfterScrubs()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(2,
                new JSONObject { ["_version"] = "2.2.0" },
                beatsPerMinute: 202f, environmentName: "BigMirrorEnvironment", songLengthSeconds: 300);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(20f);
            yield return null;
            yield return null;
            var smoke = Object.FindAnyObjectByType<EnvironmentDescriptor>()
                .GetComponentsInChildren<ParticleSystem>(true).Single(value => value.name == "BigSmokePS");
            var particles = ReadParticles(smoke);
            Assert.That(smoke.randomSeed, Is.EqualTo(0u));
            foreach (var beat in new[] { 20f + 1f / 64f, 20f, 20f - 1f / 64f, 20f, 25f, 20f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
            }
            Assert.That(ReadParticles(smoke).ToString(), Is.EqualTo(particles.ToString()));
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
