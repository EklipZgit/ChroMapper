using Newtonsoft.Json;

public class TrackLaneRingsManagerComponent : EnvDataComponent<BaseTrackLaneRingsManager>
{
    [JsonProperty("ringPositionZStep")] public float RingPositionZStep;
    [JsonProperty("rings")] public string[] Rings;

    public override void CopyTo(BaseTrackLaneRingsManager target)
    {
        target.RotationStep = RingPositionZStep;
    }
}
