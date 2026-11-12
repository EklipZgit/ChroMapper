using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class GpuSpectrumPreparation : IDisposable
{
    private const int ChunkWindows = 512;
    private const int Size = SpectrogramDataProvider.NumberOfSamples * 2;
    private const int Bins = SpectrogramDataProvider.NumberOfSamples;
    private readonly TimelineSpectrum timeline;
    private readonly ComputeShader shader;
    private readonly Action<AsyncGPUReadbackRequest> onReadbackCompleted;
    private ComputeBuffer pcmBuffer;
    private ComputeBuffer startsBuffer;
    private ComputeBuffer windowBuffer;
    private ComputeBuffer realBuffer;
    private ComputeBuffer imaginaryBuffer;
    private ComputeBuffer rawBuffer;
    private bool disposed;
    private bool readbackPending;

    internal bool Succeeded { get; private set; }
    internal bool HasPendingReadback => readbackPending;
    internal bool HasReleasedBuffers => pcmBuffer == null;

    internal GpuSpectrumPreparation(TimelineSpectrum timeline, ComputeShader shader)
    {
        this.timeline = timeline;
        this.shader = shader;
        onReadbackCompleted = OnReadbackCompleted;
    }

    internal IEnumerator Prepare()
    {
        var clip = timeline.Clip;
        var ratio = timeline.SourceSamplesPerOutputSample;
        var readFrames = Math.Min(clip.samples, (int)Math.Ceiling((ChunkWindows * TimelineSpectrum.OutputSampleRate /
            (double)TimelineSpectrum.FramesPerSecond + Size) * ratio) + 8);
        var pcm = new float[readFrames * clip.channels];
        var starts = new WindowStart[ChunkWindows];
        pcmBuffer = new ComputeBuffer(pcm.Length, sizeof(float));
        yield return null;
        startsBuffer = new ComputeBuffer(ChunkWindows, sizeof(int) + sizeof(float));
        windowBuffer = new ComputeBuffer(Size, sizeof(float));
        windowBuffer.SetData(WindowCoefficients.GetWindowForSize(Size));
        yield return null;
        realBuffer = new ComputeBuffer(ChunkWindows * Size, sizeof(float));
        imaginaryBuffer = new ComputeBuffer(ChunkWindows * Size, sizeof(float));
        rawBuffer = new ComputeBuffer(ChunkWindows * Bins, sizeof(float));
        var prepareKernel = shader.FindKernel("PrepareWindows");
        var fftKernel = shader.FindKernel("NativeFft");
        yield return null;

        for (var firstFrame = 0; firstFrame <= timeline.LastFrame; firstFrame += ChunkWindows)
        {
            var count = Math.Min(ChunkWindows, timeline.LastFrame - firstFrame + 1);
            for (var index = 0; index < count; index++)
            {
                timeline.GetWindowStart(firstFrame + index, out starts[index].Index, out starts[index].Fraction);
            }
            var readStart = Mathf.Clamp(starts[0].Index, 0, clip.samples - 1);
            clip.GetData(pcm, readStart);
            pcmBuffer.SetData(pcm);
            startsBuffer.SetData(starts);
            // Different environment loads can overlap until an older readback completes.
            // Bind every dispatch to this preparation's buffers and source clock.
            shader.SetInt("SampleSize", Size);
            shader.SetInt("WindowCount", count);
            shader.SetInt("Channels", clip.channels);
            shader.SetInt("PcmStart", readStart);
            shader.SetInt("ClipSamples", clip.samples);
            shader.SetInt("SourceStart", timeline.SourceStartSample);
            shader.SetFloat("SourceStep", (float)ratio);
            shader.SetBuffer(prepareKernel, "Pcm", pcmBuffer);
            shader.SetBuffer(prepareKernel, "Starts", startsBuffer);
            shader.SetBuffer(prepareKernel, "Window", windowBuffer);
            shader.SetBuffer(prepareKernel, "Real", realBuffer);
            shader.SetBuffer(prepareKernel, "Imaginary", imaginaryBuffer);
            shader.SetBuffer(fftKernel, "Real", realBuffer);
            shader.SetBuffer(fftKernel, "Imaginary", imaginaryBuffer);
            shader.SetBuffer(fftKernel, "RawResults", rawBuffer);
            shader.Dispatch(prepareKernel, (count * Size + 63) / 64, 1, 1);
            shader.Dispatch(fftKernel, count, 1, 1);
            readbackPending = true;
            var request = AsyncGPUReadback.Request(rawBuffer, count * Bins * sizeof(float), 0, onReadbackCompleted);
            while (readbackPending)
            {
                yield return null;
            }
            if (request.hasError)
            {
                yield break;
            }
            var raw = request.GetData<float>();
            for (var frame = 0; frame < count; frame++)
            {
                timeline.AccumulateGpuFrame(firstFrame + frame, raw, frame * Bins);
            }
        }
        Succeeded = true;
    }

    public void Dispose()
    {
        disposed = true;
        // Stopping a Unity coroutine does not guarantee iterator cleanup. A pending readback
        // owns these buffers until its callback, even if the environment has already unloaded.
        if (!readbackPending)
        {
            ReleaseBuffers();
        }
    }

    private void OnReadbackCompleted(AsyncGPUReadbackRequest request)
    {
        readbackPending = false;
        if (disposed)
        {
            ReleaseBuffers();
        }
    }

    private void ReleaseBuffers()
    {
        pcmBuffer?.Dispose();
        startsBuffer?.Dispose();
        windowBuffer?.Dispose();
        realBuffer?.Dispose();
        imaginaryBuffer?.Dispose();
        rawBuffer?.Dispose();
        pcmBuffer = null;
        startsBuffer = null;
        windowBuffer = null;
        realBuffer = null;
        imaginaryBuffer = null;
        rawBuffer = null;
    }

    private struct WindowStart
    {
        public int Index;
        public float Fraction;
    }
}
