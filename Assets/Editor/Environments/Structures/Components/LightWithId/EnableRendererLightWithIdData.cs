using Newtonsoft.Json;
using UnityEngine;

public class EnableRendererLightWithIdData : EnvironmentComponentData<EnableRendererLightController>
{
    [JsonProperty("lightId")] public int Id;

    public string Renderer;
    public float HideAlphaRangeMin = 0.001f;
    public float HideAlphaRangeMax = 1f;

    public override void
        SearchAndFillComponents(GameObject self, EnableRendererLightController comp, CreateContainer container) =>
        comp.Renderer = container.GetGameObjectOrNull(Renderer, self).GetComponent<Renderer>();

    public override void CopyTo(EnableRendererLightController comp)
    {
        comp.HideAlphaRangeMin = HideAlphaRangeMin;
        comp.HideAlphaRangeMax = HideAlphaRangeMax;
    }
}
