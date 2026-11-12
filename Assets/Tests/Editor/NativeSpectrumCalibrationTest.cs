using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    [Explicit("Local Unity DSP calibration, excluded from automated tests. AudioRenderer drives silent offline output.")]
    public class NativeSpectrumCalibrationTest
    {
        [UnityTest]
        public IEnumerator CaptureUnitySpectrum()
        {
            return CaptureUnitySpectrum(48000);
        }

        [UnityTest]
        public IEnumerator CaptureResampledUnitySpectrum()
        {
            return CaptureUnitySpectrum(44100);
        }

        private IEnumerator CaptureUnitySpectrum(int frequency)
        {
            var previousCaptureRate = Time.captureFramerate;
            var host = new GameObject("Offline audio calibration");
            var source = host.AddComponent<AudioSource>();
            host.AddComponent<AudioListener>();
            var clip = AudioClip.Create("Stereo calibration", frequency * 2, 2, frequency, false);
            var pcm = new float[clip.samples * 2];
            for (var index = 0; index < clip.samples; index++)
            {
                pcm[index * 2] = 0.8f * Mathf.Sin(2f * Mathf.PI * 3000f * index / clip.frequency);
                pcm[index * 2 + 1] = -pcm[index * 2];
            }
            clip.SetData(pcm, 0);
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            var spectrum = new float[64];
            var sourceOutput = new float[4096];
            var frames = new System.Collections.Generic.List<Frame>();
            try
            {
                Time.captureFramerate = 60;
                Assert.That(AudioRenderer.Start(), Is.True);
                source.Play();
                source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
                for (var frame = 0; frame < 20; frame++)
                {
                    yield return null;
                    var count = AudioRenderer.GetSampleCountForCaptureFrame();
                    var outputPeak = 0f;
                    using (var output = new NativeArray<float>(count * 2, Allocator.Temp))
                    {
                        Assert.That(AudioRenderer.Render(output), Is.True);
                        for (var index = 0; index < output.Length; index++)
                        {
                            outputPeak = Mathf.Max(outputPeak, Mathf.Abs(output[index]));
                        }
                    }
                    source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
                    source.GetOutputData(sourceOutput, 0);
                    frames.Add(new Frame { Samples = (float[])spectrum.Clone(), Output = (float[])sourceOutput.Clone(), Position = source.timeSamples, RenderedSamples = count, OutputPeak = outputPeak });
                }
                var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "NativeSpectrumParity");
                Directory.CreateDirectory(directory);
                File.WriteAllText(PathUtils.Combine(directory, $"unity-calibration-{frequency}.json"), JsonUtility.ToJson(
                    new Capture { OutputRate = AudioSettings.outputSampleRate, Frames = frames.ToArray() }, true));
                Assert.That(frames.Max(value => value.OutputPeak), Is.GreaterThan(0.1f));
                Assert.That(frames.Max(value => value.Samples.Max()), Is.GreaterThan(0.01f));
            }
            finally
            {
                source.Stop();
                AudioRenderer.Stop();
                Time.captureFramerate = previousCaptureRate;
                Object.Destroy(host);
                Object.Destroy(clip);
            }
        }

        [System.Serializable]
        private class Frame
        {
            public int Position;
            public int RenderedSamples;
            public float OutputPeak;
            public float[] Samples;
            public float[] Output;
        }

        [System.Serializable]
        private class Capture
        {
            public int OutputRate;
            public Frame[] Frames;
        }
    }
}
