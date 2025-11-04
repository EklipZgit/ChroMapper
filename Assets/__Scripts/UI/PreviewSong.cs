using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreviewSong : MonoBehaviour
{
    private static readonly int songTimeSeconds = Shader.PropertyToID("_SongTimeSeconds");
    
    [SerializeField] private Image progressBar;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private TMP_InputField previewStartTime;
    [SerializeField] private TMP_InputField previewDuration;

    [SerializeField] private Image image;
    [SerializeField] private Sprite startSprite;
    [SerializeField] private Sprite stopSprite;

    /// <summary>
    /// Beat Saber seems to run the audio preview shorter than specified preview length
    /// </summary>
    private const float previewLengthOffset = -1.4f;

    private float length = 10f;
    private bool playing = true;
    private double startTime;

    public void Start() =>
        // Trigger stop action which resets the UI
        PlayClip();

    public void Update()
    {
        if (!playing)
        {
            Shader.SetGlobalFloat(songTimeSeconds, -100f);
            return;
        }

        var time = (float)(AudioSettings.dspTime - startTime);
        var timeRemaining = length - time;
        if (timeRemaining <= 0)
            PlayClip();
        else if (timeRemaining < 1.25)
            // Quadratic ease
            audioSource.volume = Settings.Instance.SongVolume * 0.64f * timeRemaining * timeRemaining;
        else if (time < 0.2)
            audioSource.volume = Settings.Instance.SongVolume * 5f * time;
        else
            audioSource.volume = Settings.Instance.SongVolume;

        Shader.SetGlobalFloat(songTimeSeconds, audioSource.time);
        
        var position = time > length ? 0 : time / length;
        progressBar.fillAmount = position;
    }

    public void PlayClip()
    {
        if (playing)
        {
            progressBar.fillAmount = 0f;
            image.sprite = startSprite;
            audioSource.Stop();
            playing = false;
            Shader.SetGlobalFloat(songTimeSeconds, -100f);
            return;
        }

        if (float.TryParse(previewDuration.text, out length))
        {
            length += previewLengthOffset;

            if (float.TryParse(previewStartTime.text, out var start))
            {
                if (audioSource.clip == null)
                {
                    PersistentUI.Instance.ShowDialogBox("SongEditMenu", "preview.valid", null,
                        PersistentUI.DialogBoxPresetType.Ok);
                    return;
                }

                if (length + start > audioSource.clip.length) length = audioSource.clip.length - start;
                StartAudioPlayback(start);
            }
        }
    }

    public void PlayFromStart()
    {
        if (!float.TryParse(previewDuration.text, out length)) return;
        if (!float.TryParse(previewStartTime.text, out var start)) return;
        
        length += previewLengthOffset;
        
        if (audioSource.clip == null)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "preview.valid", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        if (length + start > audioSource.clip.length) length = audioSource.clip.length - start;
        
        StartAudioPlayback(start);
    }

    public void PlayFromEnd()
    {
        if (!float.TryParse(previewDuration.text, out var previewLength)) return;
        if (!float.TryParse(previewStartTime.text, out var start)) return;
        
        const float endPreviewDuration = 3f;
        length = endPreviewDuration + previewLengthOffset;

        if (audioSource.clip == null)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "preview.valid", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        if (length + start > audioSource.clip.length) length = audioSource.clip.length - start;
                
        var startTimeOffsetFromEnd = System.Math.Max(start, start + previewLength - endPreviewDuration);
        StartAudioPlayback(startTimeOffsetFromEnd);
    }

    private void StartAudioPlayback(float playbackTime)
    {
        playing = true;
        startTime = AudioSettings.dspTime;

        audioSource.time = playbackTime;
        if (!audioSource.isPlaying)
            audioSource.Play();
        image.sprite = stopSprite;
    }
}
