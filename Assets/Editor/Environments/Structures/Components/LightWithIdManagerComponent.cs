using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class LightWithIdManagerComponent
{
    [JsonProperty("lights")] public LightId[][] Lights;

    public class LightId
    {
        [JsonProperty("ID")] public string Name;
        [JsonProperty("id")] public int ID;
    }
}
