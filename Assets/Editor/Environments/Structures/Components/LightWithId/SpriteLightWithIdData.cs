using Newtonsoft.Json;
using UnityEngine;

public class SpriteLightWithIdData : EnvironmentComponentData<SpriteLightController>
{
    [JsonProperty("lightId")] public int Id;

    public string SpriteRenderer;

    public bool HideIfAlphaOutOfRange;
    public float HideAlphaRangeMin;
    public float HideAlphaRangeMax;
    [JsonProperty("lightIntensity")] public float Intensity;
    public float MinAlpha;
    public MultiplyColorByAlphaType MultiplyColorByAlpha;
    public bool SetColorOnly;
    public bool SetAlphaOnly;
    public bool SetOnlyOnce;

    public override void
        SearchAndFillComponents(GameObject self, SpriteLightController comp, CreateContainer container) =>
        comp.Renderer = container.GetGameObjectOrNull(SpriteRenderer, self).GetComponent<SpriteRenderer>();

    public override void CopyTo(SpriteLightController comp)
    {
        comp.HideIfAlphaOutOfRange = HideIfAlphaOutOfRange;
        comp.HideAlphaRangeMin = HideAlphaRangeMin;
        comp.HideAlphaRangeMax = HideAlphaRangeMax;
        comp.Intensity = Intensity;
        comp.MinAlpha = MinAlpha;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.SetColorOnly = SetColorOnly;
        comp.SetAlphaOnly = SetAlphaOnly;
        comp.SetOnlyOnce = SetOnlyOnce;
    }
}
