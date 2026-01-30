using Newtonsoft.Json;

public class LightIntensityIdComponent
{
    [JsonProperty("lightId")] public int ID;
    public float Intensity;

    public void CopyTo(LightIntensityController target)
    {
        target.ID = ID;
        target.Intensity = Intensity;
    }
}
