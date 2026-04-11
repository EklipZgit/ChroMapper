using UnityEngine;

public class TrackLaneRingsPositionStepEffectSpawnerData : EnvironmentComponentData<TrackLaneRingsPositionSpawner>
{
    public string EventType;
    public string TrackLaneRingsManager;
    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;

    public override void SearchAndFillComponents(
        GameObject self,
        TrackLaneRingsPositionSpawner comp,
        CreateContainer container)
    {
        comp.RingManager = container
            .GetGameObjectOrNull(TrackLaneRingsManager, self)
            .GetComponent<TrackLaneRingsManager>();
    }

    public override void CopyTo(TrackLaneRingsPositionSpawner comp)
    {
        comp.MinPositionStep = MinPositionStep;
        comp.MaxPositionStep = MaxPositionStep;
        comp.MoveSpeed = MoveSpeed;
    }
}
