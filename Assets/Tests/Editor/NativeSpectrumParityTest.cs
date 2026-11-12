using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class NativeSpectrumParityTest : TestBase
    {
        private AudioClip clip;
        private AudioClip previousClip;
        private int previousAudioLatency;

        protected override IEnumerator OnMapLoaded()
        {
            previousAudioLatency = Settings.Instance.AudioLatencyCompensation;
            Settings.Instance.AudioLatencyCompensation = 0;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreAudio()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Settings.Instance.AudioLatencyCompensation = previousAudioLatency;
            context.Atsc.SongAudioSource.clip = previousClip;
            context.Descriptor.SpectrogramDataProvider.Initialize(context.Atsc);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            Object.Destroy(clip);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeRawSpectrumUsesLeftChannelRatherThanStereoAverage()
        {
            var context = InstallTone(44100, true, toneFrequency: 44100f / 16f);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(1f);
            Assert.That(context.Descriptor.SpectrogramDataProvider.Samples[8], Is.GreaterThan(0.3f),
                "Native channel 0 remains audible when the right channel has opposite phase.");
            yield break;
        }

        [UnityTest]
        public IEnumerator NativeRawSpectrumMatchesUnityWindowGain()
        {
            // Eight cycles per FFT window reproduce the fixture on Beat Saber's serialized 44.1 kHz output clock.
            var context = InstallTone(44100, false, toneFrequency: 44100f / 16f);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(1f);
            var fixture = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "NativeSpectrumToneFixture.json")));
            var expected = fixture["rawSamples"].AsArray;
            var actual = context.Descriptor.SpectrogramDataProvider.Samples;
            for (var index = 0; index < actual.Length; index++)
            {
                Assert.That(actual[index], Is.EqualTo(expected[index].AsFloat).Within(0.0001f),
                    $"Native Blackman-Harris spectrum bin {index} differs.");
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator SpectrumBinsUseGameOutputRateRatherThanClipOrEditorRate()
        {
            var context = InstallTone(24000, false);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(1f);
            var samples = context.Descriptor.SpectrogramDataProvider.Samples;
            var peak = Enumerable.Range(0, samples.Length).OrderByDescending(index => samples[index]).First();
            Assert.That(peak, Is.EqualTo(Mathf.RoundToInt(3000f * 128f / 44100f)),
                "Beat Saber's serialized output rate defines the bins, independently of clip and editor device rates.");
            yield break;
        }

        [UnityTest]
        public IEnumerator SpectrumReadsLastCompletedGameDspBlock()
        {
            var context = InstallTone(44100, false, 512f / 44100f, 44100f / 16f);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(2f / 90f);
            Assert.That(
                context.Descriptor.SpectrogramDataProvider.Samples[8],
                Is.GreaterThan(0.3f),
                "At the second 90 Hz frame, Unity's last completed 512-sample block still contains the tone.");
            context.Atsc.MoveToTimeInSeconds(3f / 90f);
            Assert.That(
                context.Descriptor.SpectrogramDataProvider.Samples[8],
                Is.LessThan(0.00001f),
                "The next completed DSP blocks contain silence.");
            yield break;
        }

        [UnityTest]
        public IEnumerator SpectrumRespectsAudioLatencyCompensation()
        {
            Settings.Instance.AudioLatencyCompensation = 12;
            var context = InstallTone(44100, false, 1024f / 44100f, 44100f / 16f, 512f / 44100f);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(2f / 90f);
            Assert.That(
                context.Descriptor.SpectrogramDataProvider.Samples[8],
                Is.GreaterThan(0.3f),
                "The compensated audio source has reached the tone while the visual song clock is earlier.");
            Settings.Instance.AudioLatencyCompensation = 0;
            context.Descriptor.SpectrogramDataProvider.Initialize(context.Atsc);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            Assert.That(
                context.Descriptor.SpectrogramDataProvider.Samples[8],
                Is.LessThan(0.00001f),
                "Changing compensation must rebuild the same clip's spectrum history.");
            yield break;
        }

        [UnityTest]
        public IEnumerator FallLerpMatchesOneNinetiethSecondNativeFrame()
        {
            var context = InstallTone(48000, false, 0.5f);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(0.5f);
            var before = context.Descriptor.SpectrogramDataProvider.ProcessedSamples[8];
            context.Atsc.MoveToTimeInSeconds(46f / 90f);
            var after = context.Descriptor.SpectrogramDataProvider.ProcessedSamples[8];
            Assert.That(
                after,
                Is.EqualTo(before * (1f - 4f / 90f)).Within(0.00001f),
                "BasicSpectrogramData uses deltaTime * 4 for falling values, once per rendered frame.");
            yield break;
        }

        [UnityTest]
        public IEnumerator NativeRawSpectrumMatchesRecordedMusicWindow()
        {
            var fixture = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "NativeSpectrumMusicFixture.json")));
            var recordedPcm = fixture["pcmChannelZero"].AsArray;
            var pcm = new float[fixture["outputRate"].AsInt];
            var windowEnd = fixture["dspBufferLength"].AsInt;
            for (var index = 0; index < recordedPcm.Count; index++)
                pcm[windowEnd - recordedPcm.Count + index] = recordedPcm[index].AsFloat;

            var context = InstallPcm(fixture["outputRate"].AsInt, 1, pcm);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            context.Atsc.MoveToTimeInSeconds(2f / 90f);
            var actual = context.Descriptor.SpectrogramDataProvider.Samples;
            var expected = fixture["rawSamples"].AsArray;
            for (var index = 0; index < actual.Length; index++)
            {
                Assert.That(
                    actual[index],
                    Is.EqualTo(expected[index].AsFloat).Within(0.0001f),
                    $"Recorded game music spectrum bin {index} differs for the same PCM window.");
            }
            yield break;
        }

        private BeatmapRuntimeContext InstallTone(
            int frequency,
            bool oppositeStereo,
            float stop = 2f,
            float toneFrequency = 3000f,
            float start = 0f)
        {
            var channels = oppositeStereo ? 2 : 1;
            var pcm = new float[frequency * 2 * channels];
            for (var index = 0; index < frequency * 2; index++)
            {
                var value = index / (float)frequency >= start && index / (float)frequency < stop
                    ? (float)(0.8 * System.Math.Sin(2.0 * System.Math.PI * toneFrequency * index / frequency)) : 0f;
                pcm[index * channels] = value;
                if (oppositeStereo)
                    pcm[index * channels + 1] = -value;
            }
            return InstallPcm(frequency, channels, pcm);
        }

        private BeatmapRuntimeContext InstallPcm(int frequency, int channels, float[] pcm)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            previousClip = context.Atsc.SongAudioSource.clip;
            clip = AudioClip.Create("Native spectrum regression", pcm.Length / channels, channels, frequency, false);
            clip.SetData(pcm, 0);
            context.Atsc.SongAudioSource.Stop();
            context.Atsc.SongAudioSource.clip = clip;
            context.Descriptor.SpectrogramDataProvider.Initialize(context.Atsc);
            return context;
        }
    }
}
