using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CameraController editingCameraController;
    [SerializeField] private CameraController playingCameraController;
    [SerializeField] private BloomfogRenderingController bloomfogRenderingController;

    public CameraController SelectedCameraController;

    public CameraController[] CameraControllers { get; } = new CameraController[2];

    private void Start()
    {
        SelectedCameraController = editingCameraController;
        bloomfogRenderingController.AssignToCamera(SelectedCameraController);
        CameraControllers[0] = editingCameraController;
        CameraControllers[1] = playingCameraController;
    }

    public void SelectCamera(CameraType cameraType)
    {
        SelectedCameraController.Camera.enabled = false;
        SelectedCameraController = cameraType == CameraType.Editing ? editingCameraController : playingCameraController;
        SelectedCameraController.Camera.enabled = true;
        bloomfogRenderingController.AssignToCamera(SelectedCameraController);
    }
}

public enum CameraType
{
    Editing,
    Playing
}
