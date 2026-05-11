using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    private static readonly int rotationId = Shader.PropertyToID("_Rotation");

    public RotationCallbackController RotationCallback;

    [SerializeField] private bool rotateTransform = true;

    private float targetRotation;
    private float currentRotation;

    private void Start()
    {
        Shader.SetGlobalFloat(rotationId, 0);
        if (RotationCallback != null) Init();
    }

    private void LateUpdate()
    {
        if (!Settings.Instance.RotateTrack) return;
        ChangeRotation(Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime / 0.15f));
    }

    private void OnDestroy()
    {
        RotationCallback.OnRotationChanged -= HandleRotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    public void Init()
    {
        RotationCallback.OnRotationChanged += HandleRotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void UpdateRotateTrack(object obj)
    {
        var rotating = (bool)obj;
        if (rotating)
            ChangeRotation(RotationCallback.Rotation);
        else
            ChangeRotation(0);
    }

    private void HandleRotationChanged(bool natural, float rotation)
    {
        if ((BeatSaberSongContainer.Instance.Map.MajorVersion != 4 && !RotationCallback.IsActive) || !Settings.Instance.RotateTrack) return;
        targetRotation = rotation;
        if (!natural) ChangeRotation(rotation);
    }

    private void ChangeRotation(float rotation)
    {
        if (rotateTransform) transform.RotateAround(Vector3.zero, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        Shader.SetGlobalFloat(rotationId, rotation);
    }
}
