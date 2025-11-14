using Newtonsoft.Json;
using UnityEngine;

public class TrackLaneRingsManagerComponent : EnvironmentComponent<TrackLaneRingsManager>
{
    [JsonProperty("ringCount")]
    public int RingCount = 0;

    [JsonProperty("ringPositionZStep")]
    public float RingPositionZStep = 0f;

    public override void CopyTo(TrackLaneRingsManager target)
    {
        target.RotationStep = RingPositionZStep;
        target.RingCount = RingCount;
    }
}
