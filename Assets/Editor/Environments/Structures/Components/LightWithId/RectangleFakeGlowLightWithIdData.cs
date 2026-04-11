using Newtonsoft.Json;
using UnityEngine;

public class RectangleFakeGlowLightWithIdData : EnvironmentComponentData<RectangleFakeGlowLightController>
{
    [JsonProperty("lightId")] public int Id;

    public float MinAlpha;
    public float AlphaMultiplier = 1f;

    public override void SearchAndFillComponents(
        GameObject self,
        RectangleFakeGlowLightController comp,
        CreateContainer container) =>
        comp.MpbController = self.GetComponent<MaterialPropertyBlockController>();

    public override void CopyTo(RectangleFakeGlowLightController comp)
    {
        comp.MinAlpha = MinAlpha;
        comp.AlphaMultiplier = AlphaMultiplier;
    }
}
