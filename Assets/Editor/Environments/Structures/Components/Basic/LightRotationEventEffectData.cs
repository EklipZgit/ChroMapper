using UnityEngine;

public class LightRotationEventEffectData : EnvironmentComponentData<LightRotation>
{
    public string EventType;
    public Vector3 RotationVector;
    public float RotationSpeedMultiplier;

    public override void SearchAndFillComponents(GameObject self, LightRotation comp, CreateContainer container)
    {
        comp.Transform = self.transform;
        comp.StartRotation = self.transform.rotation;
    }

    public override void CopyTo(LightRotation comp)
    {
        comp.RotationVector = RotationVector;
        comp.SpeedMultiplier = RotationSpeedMultiplier;
    }
}
