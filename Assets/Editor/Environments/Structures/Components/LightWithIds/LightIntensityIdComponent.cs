using Newtonsoft.Json;

public class LightIntensityIdComponent
{
    public string Type;
    public int InstanceId;
    [JsonProperty("lightId")] public int ID;
    [JsonProperty("arrayId")] public int ArrayId;
    public float Intensity;

    public void CopyTo(LightIntensityController target)
    {
        target.ID = ID;
        target.Intensity = Intensity;
    }
}
