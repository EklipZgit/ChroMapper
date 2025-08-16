using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// Main class for EnvData information.
/// </summary>
public class EnvironmentInfo
{
    [JsonProperty("environmentData")]
    public EnvironmentData Data;

    [JsonProperty("objects")]
    public List<EnvironmentObject> Objects = new ();
}
