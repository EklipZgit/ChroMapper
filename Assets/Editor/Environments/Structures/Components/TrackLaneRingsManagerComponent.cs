using Newtonsoft.Json;

public class TrackLaneRingsManagerComponent : EnvDataComponent<BaseTrackLaneRingsManager>
{
    [JsonProperty("ringPositionZStep")]
    public float RingPositionZStep = 0f;

    public override void CopyTo(BaseTrackLaneRingsManager target)
    {
        target.RotationStep = RingPositionZStep;
    }
}
