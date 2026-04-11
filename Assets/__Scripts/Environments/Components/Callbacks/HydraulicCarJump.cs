using System.Collections.Generic;
using UnityEngine;

public class HydraulicCarJump : MonoBehaviour
{
    [SerializeField] public GenericCallbackEventEffect Effect;
    [SerializeField] public int[] EventValues;

    [Space] [SerializeField] public Vector3 Impulse;
    [SerializeField] public float Randomness = 0.1f;
    [SerializeField] public Vector3 Position;
    [SerializeField] public float MinDelayBetweenEvents = 0.5f;

    [Space] [SerializeField] public Rigidbody Rigidbody;

    private float lastEventTime;
    private HashSet<int> eventValuesHashSet;

    protected void Awake() => eventValuesHashSet = new HashSet<int>(EventValues);
    protected void OnEnable() => TrySubscribe();
    protected void OnDisable() => TryUnsubscribe();
    protected void OnDestroy() => TryUnsubscribe();

    private void TrySubscribe()
    {
        if (Effect != null) Effect.OnStateChanged += HandleStateChanged;
    }

    private void TryUnsubscribe()
    {
        if (Effect != null) Effect.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged((int index, BasicEventStateData state) data)
    {
        if (!eventValuesHashSet.Contains(data.state.Base.Value)) return;
        var timeSinceLevelLoad = Time.timeSinceLevelLoad;
        if (timeSinceLevelLoad - lastEventTime < MinDelayBetweenEvents) return;
        lastEventTime = timeSinceLevelLoad;
        Rigidbody.AddForceAtPosition(
            Impulse * (1f + Random.Range((0f - Randomness) * 0.5f, Randomness * 0.5f)),
            transform.TransformPoint(Position));
    }
}
