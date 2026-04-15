using UnityEngine;

public class TrackLaneRingsPositionStepEffectSpawnerData : EnvironmentComponentData<TrackLaneRingsPositionSpawner>
{
    public string EventType;
    public int TrackLaneRingsManager;
    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;

    public override void FillComponents(
        GameObject self,
        TrackLaneRingsPositionSpawner comp,
        CreateContainer container)
    {
        comp.EffectManager =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<TrackLaneRingsPositionEffect>(
                ConvertUtils.ToEventType(EventType));

        comp.RingManager = container
            .GetComponentOrNull<TrackLaneRingsManager>(TrackLaneRingsManager);
        comp.MinPositionStep = MinPositionStep;
        comp.MaxPositionStep = MaxPositionStep;
        comp.MoveSpeed = MoveSpeed;
    }
}
