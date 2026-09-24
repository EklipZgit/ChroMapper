using UnityEngine;
using UnityEngine.Serialization;

public class WaveformGenerator : MonoBehaviour
{
    [FormerlySerializedAs("audioManager")] public AudioManager AudioManager;

    [FormerlySerializedAs("spectrogramGradient2d")] [GradientUsage(true)] public Gradient SpectrogramGradient2d;
    
    private void Start()
    {
        if (BeatSaberSongContainer.Instance.LoadedSong == null) return;

        // The fixture-heavy test suites reload 03_Mapper hundreds of times and no headless run can display
        // the spectrogram, so batchmode skips the full clip decode + multi-hundred-MB FFT buffer churn.
        if (Application.isBatchMode) return;

        ColorBufferManager.GenerateBuffersForGradient(SpectrogramGradient2d);
        SampleBufferManager.GenerateSamplesBuffer(BeatSaberSongContainer.Instance.LoadedSong);
        
        AudioManager.GenerateFFT(BeatSaberSongContainer.Instance.LoadedSong,
            Settings.Instance.SpectrogramSampleSize,
            Settings.Instance.SpectrogramEditorQuality,
            true);
    }
}
