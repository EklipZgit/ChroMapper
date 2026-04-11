using Newtonsoft.Json;
using UnityEngine;

public class DirectionalLightWithIdData : EnvironmentComponentData<DirectionalLightController>
{
    [JsonProperty("lightId")] public int Id;

    public float Intensity;
    public float MinIntensity;
    public string Light;

    public override void SearchAndFillComponents(
        GameObject self,
        DirectionalLightController comp,
        CreateContainer container) =>
        comp.Light = container.ChromaIdObjects[Light].GetComponent<DirectionalLight>();

    public override void CopyTo(DirectionalLightController comp)
    {
        comp.Intensity = Intensity;
        comp.MinIntensity = MinIntensity;
    }
}
