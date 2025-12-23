using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseTrackLaneRingsManager : BaseTrackLaneRingsEffect
{
    public bool MoveFirstRing;

    public float MINPositionStep = 1;
    public float MAXPositionStep = 2;
    
    public float MoveSpeed = 1;
    [Header("Rotation")] public float RotationStep = 5;
    public float PropagationSpeed = 1;
    public float FlexySpeed = 1;

    public TrackLaneRingsRotationEffect RotationEffect;

    private bool zoomed;
    public TrackLaneRing[] Rings { get; private set; }

    private void FixedUpdate()
    {
        foreach (var ring in Rings) ring.FixedUpdateRing(TimeHelper.FixedDeltaTime);
    }

    private void LateUpdate()
    {
        foreach (var ring in Rings) ring.LateUpdateRing(TimeHelper.InterpolationFactor);
    }

    private void OnDrawGizmosSelected()
    {
        var forward = transform.forward;
        var position = transform.position;
        var d = 0.5f;
        var num = 45f;
        Gizmos.DrawRay(position, forward);
        var a = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 180f + num, 0f) * new Vector3(0f, 0f, 1f);
        var a2 = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 180f - num, 0f) * new Vector3(0f, 0f, 1f);
        Gizmos.DrawRay(position + forward, a * d);
        Gizmos.DrawRay(position + forward, a2 * d);
    }

    protected virtual bool IsAffectedByZoom() => !Mathf.Approximately(MAXPositionStep, MINPositionStep);

    public override void HandlePositionEvent(RingRotationStateData stateData, BaseEvent data, int index)
    {
        zoomed = index % 2 == 0;
        var step = zoomed ? MAXPositionStep : MINPositionStep;

        if (IsAffectedByZoom() && data.CustomStep != null) step = data.CustomStep.Value;

        // Multiplying MoveSpeed by 5 since I don't want to edit 20+ environment prefabs
        var speed = data.CustomSpeed ?? MoveSpeed * 5;

        for (var i = 0; i < Rings.Length; i++)
        {
            var destPosZ = (i + (MoveFirstRing ? 1 : 0)) * step;
            Rings[i].SetPosition(destPosZ, speed);
        }
    }

    public override void HandleRotationEvent(RingRotationStateData stateData, BaseEvent data, int index)
    {
        if (RotationEffect == null) return;

        RotationEffect.AddRingRotationEvent(
            stateData.RotationInitial, // TODO: this cause it to snap in unusual way
            Random.Range(0, RotationStep),
            PropagationSpeed,
            FlexySpeed,
            stateData.Direction,
            data);
    }

    public override float GetInitialRotation() => RotationEffect?.StartupRotationAngle ?? 0f;
    public override float GetRotationStep() => RotationEffect?.RotationStep ?? 0f;
    public override bool GetDirection() => Random.value < 0.5f;

    public override Object[] GetToDestroy() => new Object[] { this, RotationEffect };
}
