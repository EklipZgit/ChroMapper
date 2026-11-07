using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class EnvironmentTimelineEffectsTest : TestBase
    {
        private AudioClip previousSong;
        private AudioClip testSong;
        private bool previousAnimations;
        private bool previousClockEnabled;
        private LightshowMode previousLightshowMode;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousClockEnabled = Object.FindAnyObjectByType<AudioTimeSyncController>().enabled;
            previousLightshowMode = Object.FindAnyObjectByType<LightshowController>().Mode;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeline()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock.IsPlaying)
            {
                TestUtils.PauseDeterministicPlayback(clock);
            }
            clock.enabled = previousClockEnabled;
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            Object.FindAnyObjectByType<LightshowController>().Mode = previousLightshowMode;
            if (testSong != null)
            {
                BeatSaberSongContainer.Instance.LoadedSong = previousSong;
                clock.SongAudioSource.clip = previousSong;
                Object.Destroy(testSong);
            }
            yield return TestUtils.ReloadMap(3, null);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityTest]
        public IEnumerator SmokeAdvancesAndReturnsWithinOneSimulationStep()
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: "BigMirrorEnvironment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var smoke = context.Descriptor.GetComponentsInChildren<ParticleSystem>(true)
                .Single(value => value.name == "BigSmokePS");
            context.Atsc.MoveToTimeInSeconds(0f);
            var startup = ReadParticles(smoke);
            context.Atsc.MoveToTimeInSeconds(1f / 300f);
            var fraction = ReadParticles(smoke);
            Assert.That(fraction[0].remainingLifetime,
                Is.EqualTo(startup[0].remainingLifetime - 1f / 300f).Within(0.00001f),
                "Tiny cursor changes must advance smoke instead of rounding down to 60 Hz.");
            context.Atsc.MoveToTimeInSeconds(0.1f);
            context.Atsc.MoveToTimeInSeconds(1f / 300f);
            AssertParticlesEqual(fraction, ReadParticles(smoke));
            context.Atsc.MoveToTimeInSeconds(0f);
            AssertParticlesEqual(startup, ReadParticles(smoke));
        }

        [UnityTest]
        public IEnumerator SmokePrewarmMatchesNormalEngineStartup()
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: "BigMirrorEnvironment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var smoke = context.Descriptor.GetComponentsInChildren<ParticleSystem>(true)
                .Single(value => value.name == "BigSmokePS");
            context.Atsc.MoveToTimeInSeconds(0f);
            var controlObject = Object.Instantiate(smoke.gameObject);
            try
            {
                Object.DestroyImmediate(controlObject.GetComponent<TimelineParticleSystem>());
                var control = controlObject.GetComponent<ParticleSystem>();
                controlObject.GetComponent<ParticleSystemRenderer>().enabled = false;
                control.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                control.useAutoRandomSeed = false;
                control.randomSeed = 0u;
                control.Play(false);
                control.Pause(false);
                AssertParticlesEqual(ReadParticles(control), ReadParticles(smoke));
            }
            finally
            {
                Object.Destroy(controlObject);
            }
        }

        [UnityTest]
        public IEnumerator ExplicitZeroSmokeSeedIsPreserved()
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: "BigMirrorEnvironment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var smoke = context.Descriptor.GetComponentsInChildren<ParticleSystem>(true)
                .Single(value => value.name == "BigSmokePS");
            Assert.That(smoke.useAutoRandomSeed, Is.False);
            Assert.That(smoke.randomSeed, Is.EqualTo(0u),
                "BigMirror's explicit zero seed must not be replaced by a hierarchy hash.");
        }

        [UnityTest]
        public IEnumerator SmokeDoesNotDriftWhileTheTimelineIsPaused()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var smoke = context.Descriptor.GetComponentsInChildren<ParticleSystem>(true)
                .Single(value => value.name == "BigSmokePS");
            context.Atsc.MoveToTimeInSeconds(8f);
            yield return null;
            var expected = ReadParticles(smoke);
            for (var frame = 0; frame < 4; frame++)
            {
                yield return null;
            }
            AssertParticlesEqual(expected, ReadParticles(smoke));
        }

        [UnityTest]
        public IEnumerator SmokeReturnsToTheSameParticlesAfterReverseSeek()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var smoke = context.Descriptor.GetComponentsInChildren<ParticleSystem>(true)
                .Single(value => value.name == "BigSmokePS");
            context.Atsc.MoveToTimeInSeconds(8f);
            yield return null;
            var expected = ReadParticles(smoke);
            context.Atsc.MoveToTimeInSeconds(12f);
            yield return null;
            var later = ReadParticles(smoke);
            Assert.That(later[0].position, Is.Not.EqualTo(expected[0].position),
                "Fog must move with song time rather than remaining frozen at its load-time state.");
            context.Atsc.MoveToTimeInSeconds(8f);
            yield return null;
            AssertParticlesEqual(expected, ReadParticles(smoke));
        }

        [UnityTest]
        public IEnumerator AudioSpectrumAndMonstercatScreensFollowSilentTimelineSeeks()
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: "Monstercat2Environment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            InstallToneClip(context);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            var provider = context.Descriptor.SpectrogramDataProvider;
            var screens = context.Descriptor.GetComponentsInChildren<SpectrogramRowPropertyAnimator>(true);
            context.Atsc.MoveToTimeInSeconds(1f);
            foreach (var screen in screens)
            {
                screen.SetMultiplier(1f);
            }
            yield return null;
            var first = provider.ProcessedSamples.ToArray();
            Assert.That(first.Max(), Is.GreaterThan(0.01f), "A stopped audio device must still provide the spectrum at the scrub cursor.");
            Assert.That(screens, Is.Not.Empty, "Exercise the real Monstercat 2 screen consumers.");
            Assert.That(screens.Where(value => value.isActiveAndEnabled)
                .Any(value => value.MpbController.Mpb.GetFloat(value.PropertyName) > 0.01f), Is.True,
                "The fixture must exercise displaced screens, not their default zero multiplier.");
            foreach (var screen in screens.Where(value => value.isActiveAndEnabled))
            {
                Assert.That(screen.MpbController.Mpb.GetFloat(screen.PropertyName),
                    Is.EqualTo(screen.Multiplier * screen.AnimationCurve.Evaluate(first[screen.DataIndex])).Within(0.00001f));
            }
            context.Atsc.MoveToTimeInSeconds(2f);
            var second = provider.ProcessedSamples.ToArray();
            Assert.That(second, Is.Not.EqualTo(first), "Different tones must produce different screen data.");
            foreach (var screen in screens.Where(value => value.isActiveAndEnabled))
            {
                Assert.That(screen.MpbController.Mpb.GetFloat(screen.PropertyName),
                    Is.EqualTo(screen.Multiplier * screen.AnimationCurve.Evaluate(second[screen.DataIndex])).Within(0.00001f),
                    "Screen material data must be current before the next render, without waiting for Update.");
            }
            yield return null;
            context.Atsc.MoveToTimeInSeconds(1f);
            yield return null;
            Assert.That(provider.ProcessedSamples, Is.EqualTo(first), "Reverse scrubs must recover the same smoothed spectrum.");
            for (var frame = 0; frame < 4; frame++)
            {
                yield return null;
            }
            Assert.That(provider.ProcessedSamples, Is.EqualTo(first), "Paused spectra must not decay with wall-clock time.");

            previousClockEnabled = context.Atsc.enabled;
            context.Atsc.enabled = false;
            context.Atsc.MoveToTimeInSeconds(0f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(context.Atsc, context.Atsc.GetBeatFromSeconds(0f));
            for (var frame = 1; frame <= 90; frame++)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(context.Atsc, context.Atsc.GetBeatFromSeconds(frame / 90f));
            }
            Assert.That(provider.ProcessedSamples, Is.EqualTo(first), "Forward playback and direct seeks must share the same spectrum history.");
        }

        [UnityTest]
        public IEnumerator Panic2AudioTransformsReturnAfterReverseSeek()
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: "Panic2Environment");
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            InstallToneClip(context);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            var visualizers = context.Descriptor.GetComponentsInChildren<TransformSpectrogram>()
                .Where(value => value.isActiveAndEnabled).ToArray();
            Assert.That(visualizers, Is.Not.Empty);
            var transforms = visualizers.SelectMany(value => value.Transforms).ToArray();
            context.Atsc.MoveToTimeInSeconds(1f);
            yield return null;
            var expected = transforms.Select(value => value.localPosition).ToArray();
            context.Atsc.MoveToTimeInSeconds(2f);
            Assert.That(transforms.Where((value, index) => Vector3.Distance(value.localPosition, expected[index]) > 0.001f),
                Is.Not.Empty, "A different tone must move the bars immediately, without waiting for Update.");
            yield return null;
            context.Atsc.MoveToTimeInSeconds(1f);
            yield return null;
            for (var index = 0; index < transforms.Length; index++)
            {
                Assert.That(transforms[index].localPosition, Is.EqualTo(expected[index]));
            }
        }

        private void InstallToneClip(BeatmapRuntimeContext context)
        {
            previousSong = BeatSaberSongContainer.Instance.LoadedSong;
            const int frequency = 48000;
            testSong = AudioClip.Create("Timeline tones", frequency * 4, 1, frequency, false);
            var pcm = new float[testSong.samples];
            for (var index = 0; index < pcm.Length; index++)
            {
                var seconds = index / (float)frequency;
                var tone = seconds < 1.5f ? 3000f : 9000f;
                pcm[index] = seconds < 2.5f ? 0.8f * Mathf.Sin(2f * Mathf.PI * tone * seconds) : 0f;
            }
            testSong.SetData(pcm, 0);
            BeatSaberSongContainer.Instance.LoadedSong = testSong;
            context.Atsc.SongAudioSource.clip = testSong;
            context.Atsc.SongAudioSource.Stop();
            context.Descriptor.Initialize(context);
        }

        [UnityTest]
        public IEnumerator BillieSmokeAndCloudsReturnAfterSmallScrubs()
        {
            yield return CheckEnvironment("BillieEnvironment");
        }

        [UnityTest]
        public IEnumerator Panic2SmokeAndCloudsReturnAfterSmallScrubs()
        {
            yield return CheckEnvironment("Panic2Environment");
        }

        [UnityTest]
        public IEnumerator BritneySmokeAndCloudsReturnAfterSmallScrubs()
        {
            yield return CheckEnvironment("BritneyEnvironment");
        }

        private IEnumerator CheckEnvironment(string environmentName)
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: environmentName);
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var smoke = context.Descriptor.GetComponentsInChildren<WorldParticleVisibility>(true)
                .Where(value => value.IsSmoke && value.gameObject.activeInHierarchy)
                .Select(value => value.GetComponent<ParticleSystem>()).ToArray();
            Assert.That(smoke, Is.Not.Empty);
            var lightshow = Object.FindAnyObjectByType<LightshowController>();
            lightshow.Mode = LightshowMode.Static;
            lightshow.PopulateLightshow();
            var mode = Object.FindAnyObjectByType<UIMode>();
            Settings.Instance.Animations = true;
            mode.SetUIMode(UIModeType.Preview, false);
            var cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Playing);
            context.Atsc.MoveToTimeInSeconds(20f);
            yield return null;
            var expected = smoke.Select(ReadParticles).ToArray();
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "EnvironmentTimelineEffects", environmentName);
            Directory.CreateDirectory(directory);
            var first = Capture(cameras.SelectedCameraController.Camera,
                PathUtils.Combine(directory, "direct.png"), 640, 360);
            foreach (var time in new[] { 20f + 1f / 64f, 20f, 20f - 1f / 64f, 20f, 25f, 20f })
            {
                context.Atsc.MoveToTimeInSeconds(time);
                yield return null;
            }
            for (var index = 0; index < smoke.Length; index++)
            {
                AssertParticlesEqual(expected[index], ReadParticles(smoke[index]));
            }
            var returned = Capture(cameras.SelectedCameraController.Camera,
                PathUtils.Combine(directory, "returned.png"), 640, 360);
            var changed = first.Zip(returned, (before, after) =>
                Mathf.Abs(before.r - after.r) + Mathf.Abs(before.g - after.g) + Mathf.Abs(before.b - after.b)).ToArray();
            TestContext.Progress.WriteLine($"{environmentName}: mean render difference after scrubs = {changed.Average()}, maximum = {changed.Max()}");
            Assert.That(changed.Average(), Is.LessThan(0.0001f), "Cloud and fog pixels must recover after the cursor returns.");

            previousClockEnabled = context.Atsc.enabled;
            context.Atsc.enabled = false;
            context.Atsc.MoveToTimeInSeconds(19f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(context.Atsc, context.Atsc.GetBeatFromSeconds(19f));
            for (var frame = 1; frame <= 90; frame++)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(context.Atsc, context.Atsc.GetBeatFromSeconds(19f + frame / 90f));
            }
            for (var index = 0; index < smoke.Length; index++)
            {
                AssertParticlesEqual(expected[index], ReadParticles(smoke[index]));
            }
        }

        private static Color[] Capture(Camera camera, string path, int width, int height)
        {
            // Per-frame blue noise is independent of fog motion and must not change the comparison.
            var previousRandom = Shader.GetGlobalFloat("_GlobalRandomValue");
            Camera.CameraCallback stabilizeNoise = _ => Shader.SetGlobalFloat("_GlobalRandomValue", 0f);
            Camera.onPreRender += stabilizeNoise;
            try
            {
                return LinearRenderCapture.Capture(camera, path, width, height);
            }
            finally
            {
                Camera.onPreRender -= stabilizeNoise;
                Shader.SetGlobalFloat("_GlobalRandomValue", previousRandom);
            }
        }


        private static ParticleSystem.Particle[] ReadParticles(ParticleSystem system)
        {
            var particles = new ParticleSystem.Particle[system.particleCount];
            system.GetParticles(particles);
            Assert.That(particles, Is.Not.Empty);
            return particles;
        }

        private static void AssertParticlesEqual(ParticleSystem.Particle[] expected, ParticleSystem.Particle[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (var index = 0; index < actual.Length; index++)
            {
                Assert.That(actual[index].randomSeed, Is.EqualTo(expected[index].randomSeed));
                Assert.That(Vector3.Distance(actual[index].position, expected[index].position), Is.LessThan(0.00001f));
                Assert.That(actual[index].remainingLifetime, Is.EqualTo(expected[index].remainingLifetime).Within(0.00001f));
                Assert.That(Vector3.Distance(actual[index].rotation3D, expected[index].rotation3D), Is.LessThan(0.001f));
                Assert.That(Vector3.Distance(actual[index].startSize3D, expected[index].startSize3D), Is.LessThan(0.00001f));
            }
        }
    }
}
