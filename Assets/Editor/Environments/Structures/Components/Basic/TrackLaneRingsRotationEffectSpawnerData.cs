using UnityEngine;

public class TrackLaneRingsRotationEffectSpawnerData : EnvironmentComponentData<TrackLaneRingsRotationEffect>
{
    public string EventType;
    public int TrackLaneRingsRotationEffect;
    public float Rotation;
    public float RotationStep;
    public string RotationStepType;
    public int RotationPropagationSpeed;
    public float RotationFlexySpeed;

    public override void FillComponents(
        GameObject self,
        TrackLaneRingsRotationEffect comp,
        CreateContainer container)
    {
        container.Descriptor.BasicEventEffectManager.Register(ConvertUtils.ToEventType(EventType), comp);

        comp.Effect = container
            .GetComponentOrNull<TrackLaneRingsRotation>(TrackLaneRingsRotationEffect);
        comp.Rotation = Rotation;
        comp.Step = RotationStep;
        comp.StepType = ConvertUtils.ToRotationStepType(RotationStepType);
        comp.PropagationSpeed = RotationPropagationSpeed;
        comp.FlexySpeed = RotationFlexySpeed;
    }
}
