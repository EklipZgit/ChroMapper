using System;
using System.Collections;
using UnityEngine;

public class SpectrogramDataProvider : MonoBehaviour
{
    private static ComputeShader gpuShader;
    private AudioLink.AudioLink audioLink;
    private AudioTimeSyncController clock;
    private AudioClip sourceClip;
    private int sourceLatencyMilliseconds;
    private TimelineSpectrum timeline;
    private Coroutine preparation;
    private GpuSpectrumPreparation gpuPreparation;
    public bool HasInitialized;
    public const int NumberOfSamples = 64;
    private readonly float[] samples = new float[NumberOfSamples];
    private readonly float[] processedSamples = new float[NumberOfSamples];

    public float[] Samples => samples;
    public float[] ProcessedSamples => processedSamples;
    public event Action SpectrumChanged;

    public AudioLink.AudioLink AudioLink
    {
        get => audioLink;
        set
        {
            audioLink = value;
            HasInitialized = audioLink != null;

            if (!HasInitialized)
            {
                return;
            }

            // Environment spectra use song PCM, so AudioLink's expensive CPU texture copy is unnecessary.
            AudioLink.DisableReadback();
        }
    }

    public void Initialize(AudioTimeSyncController atsc)
    {
        if (clock != null)
        {
            clock.OnTimeChanged -= UpdateSpectrum;
            clock.OnPlayToggled -= OnPlayToggled;
        }
        clock = atsc;
        var clip = clock.SongAudioSource.clip;
        var latencyMilliseconds = Settings.Instance.AudioLatencyCompensation;
        if (sourceClip != clip || sourceLatencyMilliseconds != latencyMilliseconds)
        {
            CancelPreparation();
            sourceClip = clip;
            sourceLatencyMilliseconds = latencyMilliseconds;
            timeline = null;
            preparation = StartCoroutine(PrepareSpectrum());
        }
        clock.OnPlayToggled += OnPlayToggled;
        if (preparation == null)
        {
            clock.OnTimeChanged += UpdateSpectrum;
            UpdateSpectrum();
        }
    }

    public IEnumerator WaitForPreparation()
    {
        while (preparation != null)
        {
            yield return null;
        }
    }

    private IEnumerator PrepareSpectrum()
    {
        // Return to the loading UI before allocating preparation buffers or compiling a GPU kernel.
        yield return null;
        var candidate = TimelineSpectrum.CreateUnprepared(sourceClip, sourceLatencyMilliseconds / 1000f);
        var prepared = false;
        if (SystemInfo.supportsComputeShaders && SystemInfo.supportsAsyncGPUReadback)
        {
            if (gpuShader == null)
            {
                gpuShader = Resources.Load<ComputeShader>("EnvironmentSpectrum");
            }
            gpuPreparation = new GpuSpectrumPreparation(candidate, gpuShader);
            yield return gpuPreparation.Prepare();
            prepared = gpuPreparation.Succeeded;
            gpuPreparation.Dispose();
            gpuPreparation = null;
            if (!prepared)
            {
                Debug.LogWarning("Environment spectrum GPU readback failed. Preparing checkpoints on the CPU.");
            }
        }
        if (!prepared)
        {
            yield return candidate.PrepareCpu();
        }
        timeline = candidate;
        preparation = null;
        // Only completed history may consume cursor updates. Track transforms use OnTimeChangedEarly.
        clock.OnTimeChanged += UpdateSpectrum;
        UpdateSpectrum();
    }

    private void CancelPreparation()
    {
        if (preparation != null)
        {
            StopCoroutine(preparation);
            preparation = null;
        }
        if (gpuPreparation != null)
        {
            gpuPreparation.Dispose();
            gpuPreparation = null;
        }
    }

    private void OnDestroy()
    {
        CancelPreparation();
        if (clock != null)
        {
            clock.OnTimeChanged -= UpdateSpectrum;
            clock.OnPlayToggled -= OnPlayToggled;
        }
    }

    private void OnPlayToggled(bool playing)
    {
        // Playback reads compensation at its start, so rebuild only when that same setting changed.
        if (playing && sourceLatencyMilliseconds != Settings.Instance.AudioLatencyCompensation)
        {
            Initialize(clock);
        }
    }

    private void UpdateSpectrum()
    {
        timeline.Evaluate(clock.CurrentSeconds, samples, processedSamples);
        SpectrumChanged?.Invoke();
    }
}
