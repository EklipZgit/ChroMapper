using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class LightWithIdManagerComponent
{
    public LightId[][] Lights;

    public class LightId
    {
        [JsonProperty("objectId")] public string ObjectId;
        [JsonProperty("lightId")] public int Id;
        [JsonProperty("instanceId")] public int InstanceId;
    }
}
