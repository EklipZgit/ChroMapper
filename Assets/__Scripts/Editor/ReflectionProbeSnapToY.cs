using UnityEngine;

public class ReflectionProbeSnapToY : MonoBehaviour
{
    private BeatmapRuntimeContext context;
    private Camera mainCamera;

    private void Start()
    {
        context = FindAnyObjectByType<BeatmapRuntimeContext>();
        mainCamera = Camera.main;
    }

    //Thanks to Guidev on YouTube for the original code for planar reflections, which works just fine with Reflection Probes.
    private void Update()
    {
        if (context.Descriptor is null || !Settings.Instance.Reflections) return;
        var camDirWorld = mainCamera.transform.forward;
        var camUpWorld = mainCamera.transform.up;
        var camPosWorld = mainCamera.transform.position;

        var camDirPlane = context.Descriptor.transform.InverseTransformDirection(camDirWorld);
        var camUpPlane = context.Descriptor.transform.InverseTransformDirection(camUpWorld);
        var camPosPlane = context.Descriptor.transform.InverseTransformPoint(camPosWorld);

        camDirPlane.y *= -1f;
        camUpPlane.y *= -1f;
        camPosPlane.y *= -1f;

        camDirWorld = context.Descriptor.transform.TransformDirection(camDirPlane);
        camUpWorld = context.Descriptor.transform.TransformDirection(camUpWorld);
        camPosWorld = context.Descriptor.transform.TransformPoint(camPosPlane);

        transform.position = camPosWorld;
        transform.LookAt(camPosWorld + camDirWorld, camUpWorld);
    }
}
