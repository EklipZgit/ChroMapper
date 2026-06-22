using Beatmap.Base;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    private static readonly int rotationId = Shader.PropertyToID("_Rotation");

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private LaneRotationProvider laneRotationProvider;

    private float targetRotation;
    private float currentRotation;

    private void Start()
    {
        Shader.SetGlobalFloat(rotationId, 0);
        laneRotationProvider.OnPlaybackChanged += HandleRotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void LateUpdate()
    {
        if (!Settings.Instance.RotateTrack) return;
        ChangeRotation(Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime / 0.15f));
    }

    private void OnDestroy()
    {
        laneRotationProvider.OnPlaybackChanged -= HandleRotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    private void UpdateRotateTrack(object obj)
    {
        var rotating = (bool)obj;
        if (rotating)
            ChangeRotation(laneRotationProvider.PlaybackRotation);
        else
            ChangeRotation(0);
    }

    private void HandleRotationChanged(float rotation)
    {
        if (!Settings.Instance.RotateTrack) return;
        targetRotation = rotation;
        if (!atsc.IsPlaying) ChangeRotation(rotation);
    }

    private void ChangeRotation(float rotation)
    {
        transform.RotateAround(Vector3.zero, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        Shader.SetGlobalFloat(rotationId, rotation);
    }
}
