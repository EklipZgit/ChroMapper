using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class GpuSpectrumPreparationTest
    {
        private readonly List<GpuSpectrumPreparation> preparations = new();
        private readonly List<ComputeBuffer> buffers = new();
        private AudioClip clip;

        [UnityTearDown]
        public IEnumerator ReleaseResources()
        {
            foreach (var preparation in preparations)
            {
                preparation.Dispose();
            }
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (preparations.Exists(value => value.HasPendingReadback) && elapsed.Elapsed.TotalSeconds < 10)
            {
                yield return null;
            }
            foreach (var preparation in preparations)
            {
                Assert.That(preparation.HasReleasedBuffers, Is.True);
            }
            preparations.Clear();
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
            buffers.Clear();
            UnityEngine.Object.Destroy(clip);
        }

        [UnityTest]
        public IEnumerator GpuCheckpointsMatchCpuAndRemainExactlyScrubbable()
        {
            RequireGpu();
            CreateClip();
            var expected = new TimelineSpectrum(clip);
            var actual = TimelineSpectrum.CreateUnprepared(clip, 0f);
            var preparation = CreatePreparation(actual);
            yield return preparation.Prepare();
            Assert.That(preparation.Succeeded, Is.True, "Exercise the production compute shader, not the CPU fallback.");
            var expectedRaw = new float[64];
            var expectedProcessed = new float[64];
            var actualRaw = new float[64];
            var actualProcessed = new float[64];
            foreach (var seconds in new[] { 0f, 1f, 1.2f, 2.75f, 8f, 1f })
            {
                expected.Evaluate(seconds, expectedRaw, expectedProcessed);
                actual.Evaluate(seconds, actualRaw, actualProcessed);
                for (var bin = 0; bin < 64; bin++)
                {
                    Assert.That(actualRaw[bin], Is.EqualTo(expectedRaw[bin]).Within(0.000001f));
                    Assert.That(actualProcessed[bin], Is.EqualTo(expectedProcessed[bin]).Within(0.00005f));
                }
            }
            actual.Evaluate(1f, actualRaw, actualProcessed);
            var returned = (float[])actualProcessed.Clone();
            actual.Evaluate(1.2f, actualRaw, actualProcessed);
            actual.Evaluate(1f, actualRaw, actualProcessed);
            Assert.That(actualProcessed, Is.EqualTo(returned));
        }

        [UnityTest]
        public IEnumerator PendingReadbackReleasesBuffersAfterCancellation()
        {
            RequireGpu();
            CreateClip();
            var preparation = CreatePreparation(TimelineSpectrum.CreateUnprepared(clip, 0f));
            var routine = preparation.Prepare();
            for (var frame = 0; frame < 120 && !preparation.HasPendingReadback; frame++)
            {
                Assert.That(routine.MoveNext(), Is.True, "Cancel while a production readback is pending.");
                if (!preparation.HasPendingReadback)
                {
                    yield return routine.Current;
                }
            }
            Assert.That(preparation.HasPendingReadback, Is.True);
            preparation.Dispose();
            Assert.That(preparation.HasReleasedBuffers, Is.False, "Pending readback must retain its GPU buffers.");
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (!preparation.HasReleasedBuffers && elapsed.Elapsed.TotalSeconds < 10)
            {
                yield return null;
            }
            Assert.That(preparation.HasReleasedBuffers, Is.True);
            Assert.That(preparation.Succeeded, Is.False);
        }

        [UnityTest]
        public IEnumerator CpuFallbackMatchesSynchronousPreparation()
        {
            CreateClip();
            var expected = new TimelineSpectrum(clip);
            var actual = TimelineSpectrum.CreateUnprepared(clip, 0f);
            yield return actual.PrepareCpu();
            var expectedRaw = new float[64];
            var expectedProcessed = new float[64];
            var actualRaw = new float[64];
            var actualProcessed = new float[64];
            foreach (var seconds in new[] { 0f, 1.25f, 8f, 1.25f })
            {
                expected.Evaluate(seconds, expectedRaw, expectedProcessed);
                actual.Evaluate(seconds, actualRaw, actualProcessed);
                Assert.That(actualRaw, Is.EqualTo(expectedRaw));
                Assert.That(actualProcessed, Is.EqualTo(expectedProcessed));
            }
        }

        [UnityTest]
        public IEnumerator SharedButterflyPreservesEditorPackedOutput()
        {
            RequireGpu();
            const int size = 512;
            const int count = 4;
            var input = new float[size * count];
            var window = WindowCoefficients.GetWindowForSize(size);
            for (var index = 0; index < input.Length; index++)
            {
                input[index] = (float)((0.4 * Math.Sin(index * 0.037) + 0.3 * Math.Cos(index * 0.231)) * window[index % size]);
            }
            uint[] expected = null;
            uint[] actual = null;
            yield return CaptureEditorOutput(
                "Assets/Tests/Editor/Experiments/NativeSpectrumGpu/OriginalEditorFFT.compute", input, size, count,
                values => expected = values);
            yield return CaptureEditorOutput(
                "Assets/__Scripts/Editor/Audio/GPU/FFT.compute", input, size, count,
                values => actual = values);
            for (var frame = 0; frame < count; frame++)
            {
                // The existing mirrored half leaves its midpoint unwritten. Compare defined frequency bins.
                for (var index = 0; index < size / 8; index++)
                {
                    var offset = frame * size / 4 + index;
                    Assert.That(actual[offset], Is.EqualTo(expected[offset]));
                }
            }
        }

        private IEnumerator CaptureEditorOutput(string path, float[] input, int size, int count, Action<uint[]> completed)
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            var real = CreateBuffer(input.Length);
            var imaginary = CreateBuffer(input.Length);
            var packed = CreateBuffer(count * size / 4);
            real.SetData(input);
            imaginary.SetData(new float[input.Length]);
            var kernel = shader.FindKernel("FFT");
            shader.SetInt("SampleSize", size);
            shader.SetInt("FFTSize", size / 2);
            shader.SetInt("ProcessingOffset", 0);
            shader.SetInt("ChunkOffset", 0);
            shader.SetFloat("FFTScaleFactor", WindowCoefficients.Signal(WindowCoefficients.GetWindowForSize(size)));
            shader.SetBuffer(kernel, "Real", real);
            shader.SetBuffer(kernel, "Imaginary", imaginary);
            shader.SetBuffer(kernel, "FFTResults", packed);
            shader.Dispatch(kernel, count, 1, 1);
            var request = AsyncGPUReadback.Request(packed);
            while (!request.done)
            {
                yield return null;
            }
            Assert.That(request.hasError, Is.False);
            completed(request.GetData<uint>().ToArray());
        }

        private GpuSpectrumPreparation CreatePreparation(TimelineSpectrum timeline)
        {
            var shader = Resources.Load<ComputeShader>("EnvironmentSpectrum");
            Assert.That(shader, Is.Not.Null);
            var preparation = new GpuSpectrumPreparation(timeline, shader);
            preparations.Add(preparation);
            return preparation;
        }

        private ComputeBuffer CreateBuffer(int count)
        {
            var buffer = new ComputeBuffer(count, sizeof(float));
            buffers.Add(buffer);
            return buffer;
        }

        private void CreateClip()
        {
            const int frequency = 44100;
            clip = AudioClip.Create("GPU checkpoint regression", frequency * 12, 2, frequency, false);
            var pcm = new float[clip.samples * 2];
            for (var index = 0; index < clip.samples; index++)
            {
                var seconds = index / (double)frequency;
                var tone = seconds < 1.5 ? 3000 : 9000;
                var value = (float)((seconds < 6 ? 0.8 : 0.2) * Math.Sin(2 * Math.PI * tone * seconds));
                pcm[index * 2] = value;
                pcm[index * 2 + 1] = -value;
            }
            clip.SetData(pcm, 0);
        }

        private static void RequireGpu()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
            {
                Assert.Ignore("Requires a compute/readback-capable graphics device.");
            }
        }
    }
}
