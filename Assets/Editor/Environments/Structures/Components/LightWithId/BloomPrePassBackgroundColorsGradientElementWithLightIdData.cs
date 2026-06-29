using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientElementWithLightIdData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientElementLightController>
{
    [JsonProperty("lightId")] public int Id;

    public int BloomPrePassBackgroundColorsGradient;
    public ElementsComponent[] Elements;

    public class ElementsComponent
    {
        public int ElementNumber;
        public float Intensity = 1f;
        public float MinIntensity;
    }

    public override void FillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientElementLightController comp,
        CreateContainer container)
    {
        comp.BloomPrePassBackgroundColorsGradient =
            container.GetComponentOrNull<BloomPrePassBackgroundColorsGradient>(BloomPrePassBackgroundColorsGradient);
        comp.Elements = Elements
            .Select(x => new BloomPrePassBackgroundColorsGradientElementLightController.Element
            {
                ElementNumber = x.ElementNumber, Intensity = x.Intensity, MinIntensity = x.MinIntensity
            })
            .ToArray();
    }
}
