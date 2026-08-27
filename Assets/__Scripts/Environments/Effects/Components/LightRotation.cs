using UnityEngine;

public class LightRotation : MonoBehaviour
{
    // Existing generated environment scenes serialize the legacy manager reference;
    // BasicEventEffectManager uses it to migrate those scenes to per-visual state.
    public LightRotationEffect Effect;

    public Transform Transform;
    public Quaternion StartRotation;
    public Vector3 RotationVector;
    public float SpeedMultiplier;

    private void Start()
    {
        if (Transform == null)
            Transform = transform;
        StartRotation = Transform.rotation;
    }

    // Apply the cached interpolated angle without running a live Time.deltaTime loop.
    // This keeps the laser still while the editor is paused and exact while scrubbing.
    public void Apply(float angle)
    {
        if (Transform == null)
            return;

        Transform.localRotation = StartRotation * Quaternion.Euler(RotationVector * angle);
    }
}
