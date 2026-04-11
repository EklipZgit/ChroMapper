using UnityEngine;

public class TrackLaneRingsRotationEffectSpawnerData : EnvironmentComponentData<TrackLaneRingsRotationEffect>
{
    public string EventType;
    public string TrackLaneRingsRotationEffect;
    public float Rotation;
    public float RotationStep;
    public string RotationStepType;
    public int RotationPropagationSpeed;
    public float RotationFlexySpeed;

    public override void SearchAndFillComponents(
        GameObject self,
        TrackLaneRingsRotationEffect comp,
        CreateContainer container)
    {
        comp.Effect = container
            .GetGameObjectOrNull(TrackLaneRingsRotationEffect, self)
            .GetComponent<TrackLaneRingsRotation>();
    }

    public override void CopyTo(TrackLaneRingsRotationEffect comp)
    {
        comp.Rotation = Rotation;
        comp.Step = RotationStep;
        comp.StepType = ConvertUtils.ToRotationStepType(RotationStepType);
        comp.PropagationSpeed = RotationPropagationSpeed;
        comp.FlexySpeed = RotationFlexySpeed;
    }
}
