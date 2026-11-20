using UnityEngine;

public class TimelineLegacyAnimation : MonoBehaviour
{
    public AnimationClip Clip;
    [System.NonSerialized] public float PhaseSeconds;
    public float PlaybackSpeed = 1f;
    private AudioTimeSyncController clock;
    private bool phaseInitialized;

    public void Initialize(AudioTimeSyncController source)
    {
        // Choose once per instance so rewinding or rebinding the clock cannot change the animation's starting pose.
        if (!phaseInitialized)
        {
            PhaseSeconds = Random.value * Clip.length;
            phaseInitialized = true;
        }

        if (clock != null)
            clock.OnTimeChanged -= Refresh;

        clock = source;
        clock.OnTimeChanged += Refresh;
        Refresh();
    }

    public void SampleAtSongSeconds(float seconds) =>
        Clip.SampleAnimation(gameObject, PhaseSeconds + (Mathf.Max(0f, seconds) * PlaybackSpeed));

    private void Refresh() => SampleAtSongSeconds(clock.CurrentSeconds);

    private void OnDestroy()
    {
        if (clock != null)
            clock.OnTimeChanged -= Refresh;
    }
}
