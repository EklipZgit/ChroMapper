using Beatmap.Base;

public class SkrillexPrimaryRingStateManager : TrackLaneRingsStateManager
{
    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(RingRotationStateData stateData, BaseEvent evt, int index)
    {
        // Do nothing
    }

    public override void HandleRotationEvent(RingRotationStateData stateData, BaseEvent evt, int index)
    {
        base.HandleRotationEvent(stateData, evt, index);
        base.HandlePositionEvent(stateData, evt, index);
    }
}
