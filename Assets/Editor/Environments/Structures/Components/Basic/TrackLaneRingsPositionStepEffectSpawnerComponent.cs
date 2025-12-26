using Newtonsoft.Json;

public class TrackLaneRingsPositionStepEffectSpawnerComponent
{
    [JsonProperty("eventType")] public string EventType;

    [JsonProperty("trackLaneRingsManager")]
    public string TrackLaneRingsManager;

    [JsonProperty("minPositionStep")] public float MinPositionStep;
    [JsonProperty("maxPositionStep")] public float MaxPositionStep;
    [JsonProperty("moveSpeed")] public float MoveSpeed;
}
