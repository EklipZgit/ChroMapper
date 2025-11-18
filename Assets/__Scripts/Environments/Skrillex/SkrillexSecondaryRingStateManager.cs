using Beatmap.Base;
using UnityEngine;

public class SkrillexSecondaryRingManager : TrackLaneRingsManager
{
    [SerializeField] private InterscopeRingLaserManager[] laserManagers;

    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(RingRotationStateData stateData, BaseEvent data, int index)
    {
        base.HandlePositionEvent(stateData, data, index);
        base.HandleRotationEvent(stateData, data, index);
        foreach (var isRingLaserManager in laserManagers) isRingLaserManager.HandlePositionEvent(stateData, data, index);
    }

    public override void HandleRotationEvent(RingRotationStateData stateData, BaseEvent data, int index)
    {
        // Do nothing
    }
}
