using Newtonsoft.Json;

public class TrackLaneRingsManagerComponent : EnvironmentComponent<TrackLaneRingsManager>
{
    [JsonProperty("ringPositionZStep")]
    public float RingPositionZStep = 0f;

    public override void CopyTo(TrackLaneRingsManager target)
    {
        target.RotationStep = RingPositionZStep;
    }
}
