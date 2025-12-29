using UnityEngine;

public class TrackLaneRingsManager : MonoBehaviour
{
    public TrackLaneRing[] Rings;
    public float RingPositionStep;
    public bool SpawnAsChildren;
    private AudioTimeSyncController atsc;

    private bool hasAtsc;

    public AudioTimeSyncController Atsc
    {
        get => atsc;
        set
        {
            atsc = value;
            hasAtsc = atsc != null;
        }
    }

    public void Start()
    {
        hasAtsc = Atsc != null;
        var forward = transform.forward;
        for (var i = 0; i < Rings.Length; i++)
        {
            var t = Rings[i];
            if (SpawnAsChildren)
            {
                var pos = new Vector3(0f, 0f, i * RingPositionStep);
                t.Init(pos, Vector3.zero);
            }
            else
            {
                var pos = forward * (i * RingPositionStep);
                t.Init(pos, transform.position);
            }
        }
    }

    private void FixedUpdate()
    {
        var fdt = TimeHelper.FixedDeltaTime;
        for (var i = 0; i < Rings.Length; i++) Rings[i].FixedUpdateRing(fdt);
    }

    private void LateUpdate()
    {
        if (!hasAtsc) return;
        var intF = TimeHelper.InterpolationFactor;
        for (var i = 0; i < Rings.Length; i++) Rings[i].LateUpdateRing(intF);
    }
}
