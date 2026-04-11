using System.Linq;
using UnityEngine;

public class
    BloomPrePassBackgroundColorsGradientFromColorSchemeColorsData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientFromColorSchemeColors>
{
    public string BloomPrePassBackgroundColorsGradient;
    public ElementComponent[] Elements;

    public class ElementComponent
    {
        public bool LoadFromColorScheme;
        public int EnvironmentColor;
        public float Intensity;
        public Color Color;
    }

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientFromColorSchemeColors comp,
        CreateContainer container)
    {
        comp.BloomPrePassBackgroundColorsGradient = container
            .GetGameObjectOrNull(
                BloomPrePassBackgroundColorsGradient,
                self)
            .GetComponent<BloomPrePassBackgroundColorsGradient>();
    }

    public override void CopyTo(BloomPrePassBackgroundColorsGradientFromColorSchemeColors comp)
    {
        comp.Elements = Elements
            .Select(x => new BloomPrePassBackgroundColorsGradientFromColorSchemeColors.Element
            {
                LoadFromColorScheme = x.LoadFromColorScheme,
                EnvironmentColor =
                    (BloomPrePassBackgroundColorsGradientFromColorSchemeColors.EnvironmentColor)x.EnvironmentColor,
                Intensity = x.Intensity,
                Color = x.Color
            })
            .ToArray();
    }
}
