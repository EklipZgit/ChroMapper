using System;
using System.Collections;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;

// One-second checkpoints retain the smoothing history without storing every frame of a song.
internal sealed class TimelineSpectrum
{
    // Beat Saber 1.44.1's AudioManager requests 44.1 kHz. CM's output device must not move the game's bins.
    public const int OutputSampleRate = 44100;
    public const int DspBufferLength = 512;
    public const int FramesPerSecond = 90;
    private const int Size = SpectrogramDataProvider.NumberOfSamples * 2;
    private readonly AudioClip clip;
    private readonly float[] monoSamples;
    private readonly float[] audioWindow;
    private readonly float[] window = WindowCoefficients.GetWindowForSize(Size);
    private readonly float[] real = new float[Size];
    private readonly float[] imaginary = new float[Size];
    private readonly float[] spectrum = new float[Size / 2];
    private readonly float[] state = new float[Size / 2];
    private readonly float[] checkpoints;
    private readonly double sourceSamplesPerOutputSample;
    private readonly long sourceStartOutputSample;
    private readonly int sourceStartSample;
    private readonly int lastFrame;
    private int currentFrame = -1;

    internal AudioClip Clip => clip;
    internal int LastFrame => lastFrame;
    internal int SourceStartSample => sourceStartSample;
    internal double SourceSamplesPerOutputSample => sourceSamplesPerOutputSample;

    public TimelineSpectrum(AudioClip clip, float sourceTimeOffsetSeconds = 0f)
        : this(clip, sourceTimeOffsetSeconds, true)
    {
    }

    private TimelineSpectrum(AudioClip clip, float sourceTimeOffsetSeconds, bool prepareCheckpoints)
    {
        this.clip = clip;
        // The 2D spectrogram averages stereo channels, but GetSpectrumData reads channel zero.
        monoSamples = clip.channels == 1 && SampleBufferManager.SourceClip == clip
            ? SampleBufferManager.MonoSamples
            : null;
        sourceSamplesPerOutputSample = clip.frequency / (double)OutputSampleRate;
        sourceStartOutputSample = (long)(sourceTimeOffsetSeconds * OutputSampleRate);
        sourceStartSample = (int)(sourceStartOutputSample * sourceSamplesPerOutputSample);
        audioWindow = new float[((int)Math.Ceiling(Size * sourceSamplesPerOutputSample) + 2) * clip.channels];
        lastFrame = Mathf.CeilToInt(clip.length * FramesPerSecond);
        checkpoints = new float[((lastFrame / FramesPerSecond) + 1) * state.Length];
        if (prepareCheckpoints)
        {
            for (var frame = 0; frame <= lastFrame; frame++)
            {
                PrepareFrame(frame);
            }
        }
    }

    internal static TimelineSpectrum CreateUnprepared(AudioClip clip, float sourceTimeOffsetSeconds)
    {
        return new TimelineSpectrum(clip, sourceTimeOffsetSeconds, false);
    }

    internal IEnumerator PrepareCpu()
    {
        // A failed GPU readback may have populated part of the smoothing history.
        Array.Clear(state, 0, state.Length);
        Array.Clear(spectrum, 0, spectrum.Length);
        currentFrame = -1;
        var sliceStart = Stopwatch.GetTimestamp();
        for (var frame = 0; frame <= lastFrame; frame++)
        {
            PrepareFrame(frame);
            if (Stopwatch.GetTimestamp() - sliceStart >= Stopwatch.Frequency / 500)
            {
                yield return null;
                sliceStart = Stopwatch.GetTimestamp();
            }
        }
    }

    internal void GetWindowStart(int frame, out int sourceIndex, out float fraction)
    {
        var position = GetFirstOutputSample(frame) * sourceSamplesPerOutputSample;
        sourceIndex = (int)Math.Floor(position);
        fraction = (float)(position - sourceIndex);
    }

    internal void AccumulateGpuFrame(int frame, NativeArray<float> raw, int offset)
    {
        for (var index = 0; index < spectrum.Length; index++)
        {
            spectrum[index] = raw[offset + index];
        }
        Smooth();
        SaveCheckpoint(frame);
    }

    private void PrepareFrame(int frame)
    {
        CalculateSpectrum(frame);
        Smooth();
        SaveCheckpoint(frame);
    }

    private void SaveCheckpoint(int frame)
    {
        if (frame % FramesPerSecond == 0)
        {
            Array.Copy(state, 0, checkpoints, frame / FramesPerSecond * state.Length, state.Length);
        }
    }

    public void Evaluate(float seconds, float[] samples, float[] processedSamples)
    {
        var frame = Mathf.Clamp(Mathf.FloorToInt(seconds * FramesPerSecond), 0, lastFrame);
        // GPU checkpoints are authoritative at each second. Rebase playback there too,
        // otherwise CPU rounding accumulates differently from a direct seek to the same time.
        if (currentFrame < 0 || frame < currentFrame || frame / FramesPerSecond != currentFrame / FramesPerSecond)
        {
            currentFrame = frame / FramesPerSecond * FramesPerSecond;
            Array.Copy(checkpoints, currentFrame / FramesPerSecond * state.Length, state, 0, state.Length);
        }
        while (currentFrame < frame)
        {
            CalculateSpectrum(++currentFrame);
            Smooth();
        }

        CalculateSpectrum(frame);
        Array.Copy(spectrum, samples, spectrum.Length);
        Array.Copy(state, processedSamples, state.Length);
    }

    private void Smooth()
    {
        for (var index = 0; index < state.Length; index++)
        {
            var value = Mathf.Log(spectrum[index] + 1f) * (index + 1);
            if (value - state[index] > 0.1f)
                state[index] = value;
            else
                state[index] = Mathf.Lerp(state[index], value, (value > state[index] ? 8f : 4f) / FramesPerSecond);

        }
    }

    private void CalculateSpectrum(int frame)
    {
        // Unity's spectrum bins use the output sample rate even when the clip has a different rate.
        // Native GetSpectrumData holds the last completed DSP block between mixer updates.
        var firstOutputSample = GetFirstOutputSample(frame);
        var firstSourceSample = (int)Math.Floor(firstOutputSample * sourceSamplesPerOutputSample);
        var readStart = Mathf.Clamp(firstSourceSample, 0, clip.samples - 1);
        if (monoSamples == null)
            clip.GetData(audioWindow, readStart);

        for (var index = 0; index < Size; index++)
        {
            var sourcePosition = (firstOutputSample + index) * sourceSamplesPerOutputSample;
            var sourceIndex = (int)Math.Floor(sourcePosition);
            var value = Mathf.LerpUnclamped(
                ReadSample(sourceIndex, readStart),
                ReadSample(sourceIndex + 1, readStart),
                (float)(sourcePosition - sourceIndex));
            real[index] = value * window[index];
            imaginary[index] = 0f;
        }
        for (int index = 1, reversed = 0; index < Size; index++)
        {
            var bit = Size >> 1;
            while ((reversed & bit) != 0)
            {
                reversed ^= bit;
                bit >>= 1;
            }
            reversed ^= bit;
            if (index < reversed)
            {
                (real[index], real[reversed]) = (real[reversed], real[index]);
            }
        }
        for (var length = 2; length <= Size; length <<= 1)
        {
            var angle = -2f * Mathf.PI / length;
            var stepReal = Mathf.Cos(angle);
            var stepImaginary = Mathf.Sin(angle);
            for (var start = 0; start < Size; start += length)
            {
                var twiddleReal = 1f;
                var twiddleImaginary = 0f;
                for (var offset = 0; offset < length / 2; offset++)
                {
                    var top = start + offset;
                    var bottom = top + length / 2;
                    var bottomReal = real[bottom] * twiddleReal - imaginary[bottom] * twiddleImaginary;
                    var bottomImaginary = real[bottom] * twiddleImaginary + imaginary[bottom] * twiddleReal;
                    real[bottom] = real[top] - bottomReal;
                    imaginary[bottom] = imaginary[top] - bottomImaginary;
                    real[top] += bottomReal;
                    imaginary[top] += bottomImaginary;
                    var nextReal = twiddleReal * stepReal - twiddleImaginary * stepImaginary;
                    twiddleImaginary = twiddleReal * stepImaginary + twiddleImaginary * stepReal;
                    twiddleReal = nextReal;
                }
            }
        }
        // GetSpectrumData leaves its final output slot at zero rather than returning that DFT bin.
        for (var index = 0; index < spectrum.Length - 1; index++)
        {
            // NativeSpectrumToneFixture records Unity's FFT gain, which does not compensate window loss.
            spectrum[index] = Mathf.Sqrt(real[index] * real[index] + imaginary[index] * imaginary[index]) * (2.5f / Size);
        }
    }

    private long GetFirstOutputSample(int frame)
    {
        var completedSamples = (long)frame * OutputSampleRate / FramesPerSecond / DspBufferLength * DspBufferLength;
        return sourceStartOutputSample + completedSamples - Size;
    }

    private float ReadSample(int sourceIndex, int readStart)
    {
        if (sourceIndex < 0 || sourceIndex < sourceStartSample || sourceIndex >= clip.samples)
            return 0f;

        // The editor spectrogram stores mono PCM with a historical 1/1.5 amplitude adjustment.
        return monoSamples != null
            ? monoSamples[sourceIndex] * 1.5f
            : audioWindow[(sourceIndex - readStart) * clip.channels];
    }
}
