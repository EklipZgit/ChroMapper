using Newtonsoft.Json;
using UnityEngine;

public class LightIntensityIdData : EnvironmentComponentData<LightIntensityData>
{
    [JsonProperty("lightId")] public int ID;
    public float Intensity;

    public override void SearchAndFillComponents(
        GameObject self,
        LightIntensityData comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(LightIntensityData comp) => comp.Intensity = Intensity;
}
