using UnityEngine;

public class TimelineLegacyAnimation : MonoBehaviour
{
    public AnimationClip Clip;
    public float PhaseSeconds;
    public float PlaybackSpeed = 1f;
    private AudioTimeSyncController clock;

    public void Initialize(AudioTimeSyncController source)
    {
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
