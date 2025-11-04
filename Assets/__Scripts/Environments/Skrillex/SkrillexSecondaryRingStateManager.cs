using Beatmap.Base;
using UnityEngine;

public class SkrillexSecondaryRingStateManager : TrackLaneRingsStateManager
{
    [SerializeField] private InterscopeRingLaserStateManager[] laserManagers;

    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(RingRotationStateData stateData, BaseEvent evt, int index)
    {
        base.HandlePositionEvent(stateData, evt, index);
        base.HandleRotationEvent(stateData, evt, index);
        foreach (var isRingLaserManager in laserManagers) isRingLaserManager.HandlePositionEvent(stateData, evt, index);
    }

    public override void HandleRotationEvent(RingRotationStateData stateData, BaseEvent evt, int index)
    {
        // Do nothing
    }
}
