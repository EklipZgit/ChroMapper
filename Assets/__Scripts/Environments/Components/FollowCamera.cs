using UnityEngine;

// World-space dust must follow the map camera before emission is simulated.
// Rendering also updates its pose when the camera moves without changing the song cursor.
public class FollowCamera : MonoBehaviour
{
    private Quaternion rotationOffset;
    private Vector3 positionOffset;
    private Transform tr;
    private AudioTimeSyncController clock;
    private CameraManager cameraManager;

    protected void Awake()
    {
        tr = transform;
        rotationOffset = tr.rotation;
        positionOffset = tr.position;
    }

    protected void OnEnable()
    {
        Camera.onPreRender -= HandleCameraPreRender;
        Camera.onPreRender += HandleCameraPreRender;
        if (clock != null)
            clock.OnTimeChanged += RefreshPose;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= HandleCameraPreRender;
        if (clock != null)
            clock.OnTimeChanged -= RefreshPose;
    }

    internal void Initialize(AudioTimeSyncController source)
    {
        if (clock != null)
            clock.OnTimeChanged -= RefreshPose;
        clock = source;
        cameraManager = FindAnyObjectByType<CameraManager>();
        if (isActiveAndEnabled)
        {
            clock.OnTimeChanged += RefreshPose;
            RefreshPose();
        }
    }

    // Head-track transitions can temporarily disable the camera, making Camera.main unavailable during a clock refresh.
    private void RefreshPose() => ApplyCameraPose(cameraManager.SelectedCameraController.Camera);

    private void HandleCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != Camera.main) return;

        ApplyCameraPose(renderingCamera);
    }

    private void ApplyCameraPose(Camera camera)
    {
        var camTransform = camera.transform;
        tr.SetPositionAndRotation(camTransform.position + positionOffset, camTransform.rotation * rotationOffset);
    }
}
