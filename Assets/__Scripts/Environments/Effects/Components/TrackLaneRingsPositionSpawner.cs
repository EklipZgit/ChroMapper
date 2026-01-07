using UnityEngine;

public class TrackLaneRingsPositionSpawner : MonoBehaviour
{
    public TrackLaneRingsManager RingManager;
    public TrackLaneRingsPositionEffect EffectManager;

    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;

    private void Start() => RingManager.Atsc = EffectManager.Atsc;

    private void OnEnable() => EffectManager.OnStateChanged += HandleStateChanged;
    private void OnDisable() => EffectManager.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged((int index, TrackLaneRingsPositionStateData state) data)
    {
        var index = data.index;
        var state = data.state;

        var zoomed = index % 2 == 0;
        var step = state.Step ?? (zoomed ? MaxPositionStep : MinPositionStep);
        var speed = state.Speed ?? MoveSpeed;

        for (var i = 0; i < RingManager.Rings.Length; i++)
        {
            var destPosZ = i * step;
            RingManager.Rings[i].SetPosition(destPosZ, speed);
        }
    }
}
