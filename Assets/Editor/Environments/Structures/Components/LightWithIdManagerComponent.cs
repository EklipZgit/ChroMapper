using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class LightWithIdManagerComponent
{
    public Dictionary<int, LightId[]> Lights;

    public class LightId
    {
        public string ObjectId;
        [JsonProperty("lightId")] public int Id;
        public int InstanceId;
    }
}
