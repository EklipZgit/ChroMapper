using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class SpriteArrayLightWithIdData : EnvironmentComponentData<SpriteArrayLightController>
{
    [JsonProperty("lightId")] public int Id;

    public string[] SpriteRenderers;

    public bool HideIfAlphaOutOfRange;
    public float HideAlphaRangeMin = 0.001f;
    public float HideAlphaRangeMax = 1f;

    public float Intensity = 1f;
    public float MinAlpha;
    public MultiplyColorByAlphaType MultiplyColorByAlpha;
    public bool SetColorOnly;
    public bool SetAlphaOnly;
    public bool SetOnlyOnce;

    public override void SearchAndFillComponents(
        GameObject self,
        SpriteArrayLightController comp,
        CreateContainer container)
    {
        comp.SpriteRenderers =
            SpriteRenderers.Select(x => container.GetGameObjectOrNull(x, self).GetComponent<SpriteRenderer>()).ToArray();
    }

    public override void CopyTo(SpriteArrayLightController comp)
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
