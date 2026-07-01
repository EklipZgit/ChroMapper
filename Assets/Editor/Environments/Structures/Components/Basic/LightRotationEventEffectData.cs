using UnityEngine;

public class LightRotationEventEffectData : EnvironmentComponentData<LightRotation>
{
    public string EventType;
    public Vector3 RotationVector;
    public float RotationSpeedMultiplier;

    public override void FillComponents(GameObject self, LightRotation comp, CreateContainer container)
    {
        comp.enabled = true;
        comp.Effect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<LightRotationEffect>(
                ConvertUtils.ToEventType(EventType));

        comp.Transform = self.transform;
        comp.StartRotation = self.transform.rotation;
        comp.RotationVector = RotationVector;
        comp.SpeedMultiplier = RotationSpeedMultiplier;
    }
}
