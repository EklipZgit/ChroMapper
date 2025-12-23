using Beatmap.Base;

public class SkrillexPrimaryRingManager : BaseTrackLaneRingsManager
{
    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(RingRotationStateData stateData, BaseEvent data, int index)
    {
        // Do nothing
    }

    public override void HandleRotationEvent(RingRotationStateData stateData, BaseEvent data, int index)
    {
        base.HandleRotationEvent(stateData, data, index);
        base.HandlePositionEvent(stateData, data, index);
    }
}
