using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SpectrumPreparationTest : TestBase
    {
        private readonly List<AudioClip> clips = new();
        private GameObject host;
        private SpectrogramDataProvider provider;
        private AudioTimeSyncController clock;
        private AudioClip previousClip;
        private bool previousClockEnabled;
        private int previousLatency;
        private int notifications;

        [UnitySetUp]
        public IEnumerator SetUpProvider()
        {
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            previousClip = clock.SongAudioSource.clip;
            previousLatency = Settings.Instance.AudioLatencyCompensation;
            Settings.Instance.AudioLatencyCompensation = 0;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            host = new GameObject("Spectrum preparation regression");
            provider = host.AddComponent<SpectrogramDataProvider>();
            notifications = 0;
            provider.SpectrumChanged += CountNotification;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreProvider()
        {
            Object.Destroy(host);
            yield return null;
            clock.SongAudioSource.clip = previousClip;
            clock.enabled = previousClockEnabled;
            Settings.Instance.AudioLatencyCompensation = previousLatency;
            foreach (var clip in clips)
            {
                Object.Destroy(clip);
            }
            clips.Clear();
        }

        [UnityTest]
        public IEnumerator InitializationPublishesOnlyAfterPreparation()
        {
            clock.SongAudioSource.clip = CreateTone(3000f);
            provider.Initialize(clock);
            Assert.That(notifications, Is.Zero, "Preparation must yield to the UI before publishing its completed history.");
            clock.MoveToTimeInSeconds(1f);
            Assert.That(notifications, Is.Zero, "Scrubbing during preparation must not publish partial checkpoints.");
            yield return WaitForNotification();
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(provider.Samples.Max(), Is.GreaterThan(0.1f));
            var expected = provider.ProcessedSamples.ToArray();
            clock.MoveToTimeInSeconds(8f);
            clock.MoveToTimeInSeconds(1f);
            Assert.That(provider.ProcessedSamples, Is.EqualTo(expected), "The completed history must remain reversible.");
        }

        [UnityTest]
        public IEnumerator ReplacingPendingSongPublishesOnlyLatestSong()
        {
            clock.SongAudioSource.clip = CreateTone(3000f);
            provider.Initialize(clock);
            clock.SongAudioSource.clip = CreateTone(9000f);
            provider.Initialize(clock);
            Assert.That(notifications, Is.Zero, "Replacing a pending build must not publish either unfinished history.");
            clock.MoveToTimeInSeconds(1f);
            yield return WaitForNotification();
            Assert.That(notifications, Is.EqualTo(1));
            var peak = Enumerable.Range(0, provider.Samples.Length).OrderByDescending(index => provider.Samples[index]).First();
            Assert.That(peak, Is.EqualTo(Mathf.RoundToInt(9000f * 128f / 44100f)));
            yield return null;
            yield return null;
            Assert.That(notifications, Is.EqualTo(1), "A canceled build must never publish after its replacement.");
        }

        [UnityTest]
        public IEnumerator ReinitializingSameSongKeepsOneCompletion()
        {
            clock.SongAudioSource.clip = CreateTone(3000f);
            provider.Initialize(clock);
            provider.Initialize(clock);
            Assert.That(notifications, Is.Zero, "Reinitialization must retain the pending preparation.");
            clock.MoveToTimeInSeconds(1f);
            yield return WaitForNotification();
            Assert.That(notifications, Is.EqualTo(1));
            clock.MoveToTimeInSeconds(2f);
            Assert.That(notifications, Is.EqualTo(2), "The clock must have exactly one spectrum subscription.");
        }

        [UnityTest]
        public IEnumerator DestroyingProviderCancelsPendingPreparation()
        {
            clock.SongAudioSource.clip = CreateTone(3000f);
            provider.Initialize(clock);
            Assert.That(notifications, Is.Zero, "The provider must still be preparing when it is destroyed.");
            Object.Destroy(host);
            yield return null;
            clock.MoveToTimeInSeconds(1f);
            yield return null;
            Assert.That(notifications, Is.Zero, "An unloaded environment must not publish or retain a clock callback.");
        }

        private AudioClip CreateTone(float tone)
        {
            const int frequency = 24000;
            var clip = AudioClip.Create("Preparation tone", frequency * 12, 1, frequency, false);
            var pcm = new float[clip.samples];
            for (var index = 0; index < pcm.Length; index++)
            {
                pcm[index] = (float)(0.8 * System.Math.Sin(2.0 * System.Math.PI * tone * index / frequency));
            }
            clip.SetData(pcm, 0);
            clips.Add(clip);
            return clip;
        }

        private IEnumerator WaitForNotification()
        {
            // Batch mode can render 120 frames in less than a GPU readback's wall-clock duration.
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (notifications == 0 && elapsed.Elapsed.TotalSeconds < 10)
            {
                yield return null;
            }
            Assert.That(notifications, Is.GreaterThan(0), "Preparation did not publish its completed spectrum.");
        }

        private void CountNotification()
        {
            notifications++;
        }
    }
}
