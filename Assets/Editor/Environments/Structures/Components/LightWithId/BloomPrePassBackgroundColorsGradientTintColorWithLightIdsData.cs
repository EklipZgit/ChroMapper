using Newtonsoft.Json;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorWithLightIdsData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientTintColorLightController>
{
    [JsonProperty("lightId")] public int Id;

    public string BloomPrePassBackgroundColorsGradient;

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientTintColorLightController comp,
        CreateContainer container)
    {
        comp.BloomPrePassBackgroundColorsGradient =
            container
                .GetGameObjectOrNull(BloomPrePassBackgroundColorsGradient, self)
                .GetComponent<BloomPrePassBackgroundColorsGradient>();
    }

    public override void CopyTo(BloomPrePassBackgroundColorsGradientTintColorLightController comp) { }
}
