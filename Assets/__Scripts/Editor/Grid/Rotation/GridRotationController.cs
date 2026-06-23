using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    private static readonly int rotationId = Shader.PropertyToID("_Rotation");

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private LaneRotationProvider laneRotationProvider;

    private void Start()
    {
        Shader.SetGlobalFloat(rotationId, 0);
        atsc.OnPlayToggled += HandlePlayToggled;
        laneRotationProvider.OnEditChanged += HandleEditRotationChanged;
        laneRotationProvider.OnPlaybackChanged += HandleRotationChanged;
        laneRotationProvider.OnSmoothedPlaybackChanged += HandleSmoothedRotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void OnDestroy()
    {
        laneRotationProvider.OnEditChanged -= HandleEditRotationChanged;
        laneRotationProvider.OnPlaybackChanged -= HandleRotationChanged;
        laneRotationProvider.OnSmoothedPlaybackChanged -= HandleSmoothedRotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    private void HandlePlayToggled(bool toggle)
    {
        if (toggle)
            SetRotation(laneRotationProvider.SmoothRotation);
        else
        {
            SetRotation(
                BeatSaberSongContainer.Instance.Map.MajorVersion < 4
                    ? laneRotationProvider.PlaybackRotation
                    : laneRotationProvider.EditRotation);
        }
    }

    private void UpdateRotateTrack(object obj)
    {
        var rotating = (bool)obj;
        if (rotating)
            SetRotation(laneRotationProvider.PlaybackRotation);
        else
            SetRotation(0);
    }

    private void HandleEditRotationChanged(float rotation)
    {
        if (atsc.IsPlaying || !Settings.Instance.RotateTrack || BeatSaberSongContainer.Instance.Map.MajorVersion < 4)
            return;
        SetRotation(rotation);
    }

    private void HandleRotationChanged(float rotation)
    {
        if (atsc.IsPlaying || !Settings.Instance.RotateTrack || BeatSaberSongContainer.Instance.Map.MajorVersion >= 4)
            return;
        SetRotation(rotation);
    }

    private void HandleSmoothedRotationChanged(float rotation)
    {
        if (!atsc.IsPlaying || !Settings.Instance.RotateTrack) return;
        SetRotation(rotation);
    }

    private void SetRotation(float rotation)
    {
        transform.localEulerAngles = new Vector3(0, rotation, 0);
        Shader.SetGlobalFloat(rotationId, rotation);
    }
}
