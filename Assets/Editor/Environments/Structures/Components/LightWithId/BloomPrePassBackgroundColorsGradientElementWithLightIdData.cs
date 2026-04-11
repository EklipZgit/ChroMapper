using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientElementWithLightIdData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientElementLightController>
{
    [JsonProperty("lightId")] public int Id;

    public string BloomPrePassBackgroundColorsGradient;
    public ElementsComponent[] Elements;

    public class ElementsComponent
    {
        public int ElementNumber;
        public float Intensity = 1f;
        public float MinIntensity;
    }

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientElementLightController comp,
        CreateContainer container)
    {
        comp.BloomPrePassBackgroundColorsGradient =
            container
                .GetGameObjectOrNull(BloomPrePassBackgroundColorsGradient, self)
                .GetComponent<BloomPrePassBackgroundColorsGradient>();
    }

    public override void CopyTo(BloomPrePassBackgroundColorsGradientElementLightController comp)
    {
        comp.Elements = Elements
            .Select(x => new BloomPrePassBackgroundColorsGradientElementLightController.Element
            {
                ElementNumber = x.ElementNumber, Intensity = x.Intensity, MinIntensity = x.MinIntensity
            })
            .ToArray();
    }
}
