using UnityEngine;

public class LightRotation : MonoBehaviour
{
    public Transform Transform;
    public Quaternion StartRotation;
    public Vector3 RotationVector;
    public float SpeedMultiplier;

    private bool initialized;

    private void Start() => Initialize();

    // LightRotationLateWiringIsFinalizedDuringEffectInitialization captures builder-assigned transforms before
    // cached rendering begins; the idempotent guard prevents manager reinitialization from changing the rest pose.
    public void Initialize()
    {
        if (initialized)
            return;

        if (Transform == null)
            Transform = transform;

        StartRotation = Transform.rotation;
        initialized = true;
    }

    // Apply the cached interpolated angle without running a live Time.deltaTime loop.
    // This keeps the laser still while the editor is paused and exact while scrubbing; Initialize
    // establishes the transform dependency before this render path can run.
    public void Apply(float angle)
    {
        Transform.localRotation = StartRotation * Quaternion.Euler(RotationVector * angle);
    }
}
