using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class LizzoBalloonParticlesTest : TestBase
    {
        private bool previousAnimations;
        private bool previousLoadEvents;
        private LightshowMode previousLightshowMode;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousLoadEvents = Settings.Instance.Load_Events;
            previousLightshowMode = Object.FindAnyObjectByType<LightshowController>().Mode;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator LizzoBasicEventsSpawnBalloonsFromBothEmitters()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.Load_Events = true;
            yield return TestUtils.ReloadMap(3, JSON.Parse(@"{
                ""version"":""3.3.0"",
                ""basicBeatmapEvents"":[{""b"":2,""et"":16,""i"":1,""f"":1},{""b"":2,""et"":17,""i"":1,""f"":1}]
            }"), beatsPerMinute: 60, environmentName: "LizzoEnvironment", songLengthSeconds: 90);

            var effects = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BasicEventEffectManager;
            var left = effects.EventTypeToEffects[16].OfType<ParticleSystemEmitEventEffect>().Single();
            var right = effects.EventTypeToEffects[17].OfType<ParticleSystemEmitEventEffect>().Single();
            Assert.That(left.Prefab != null, Is.True, "Lizzo's left emitter needs the balloon controller prefab.");
            Assert.That(right.Prefab, Is.SameAs(left.Prefab), "Both emitters use the native installer's shared prefab.");
            Assert.That(left.ParticleSystemParentTransform, Is.Not.SameAs(right.ParticleSystemParentTransform));
            Assert.That(left.MaxSpawnedParticleSystems, Is.EqualTo(4));
            Assert.That(right.MaxSpawnedParticleSystems, Is.EqualTo(4));

            Object.FindAnyObjectByType<LightshowController>().SetMode(LightshowMode.Full);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            using (var playback = new DeterministicPlaybackClock(clock))
            {
                playback.StartAtJsonTime(1.99f);
                playback.AdvanceToJsonTime(2.01f);
                yield return null;
                playback.AdvanceToJsonTime(2.25f);
                yield return null;
                VerifyBalloons(left);
                VerifyBalloons(right);
                playback.Pause();
            }

            clock.MoveToJsonTime(1f);
            yield return null;
            Assert.That(left.ParticleSystemParentTransform.GetComponentsInChildren<ParticleSystemEventController>(), Is.Empty);
            Assert.That(right.ParticleSystemParentTransform.GetComponentsInChildren<ParticleSystemEventController>(), Is.Empty);

            clock.MoveToJsonTime(2.25f);
            yield return null;
            VerifyBalloons(left);
            VerifyBalloons(right);
        }

        private static void VerifyBalloons(ParticleSystemEmitEventEffect effect)
        {
            var controller = effect.ParticleSystemParentTransform.GetComponentsInChildren<ParticleSystemEventController>().Single();
            Assert.That(controller.StartTime, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(controller.FullDuration, Is.EqualTo(60f));
            Assert.That(controller.ParticleSystem.particleCount, Is.GreaterThan(0));
            var renderer = controller.ParticleSystem.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.renderMode, Is.EqualTo(ParticleSystemRenderMode.Mesh));
            Assert.That(renderer.mesh != null, Is.True);
            Assert.That(renderer.sharedMaterial != null, Is.True);
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Object.FindAnyObjectByType<LightshowController>().SetMode(previousLightshowMode);
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.Load_Events = previousLoadEvents;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
