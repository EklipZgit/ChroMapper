using System.Linq;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientData : EnvironmentComponentData<BloomPrePassBackgroundColorsGradient>
{
    public int ExecutionTimeType;
    public Color TintColor;
    public ElementComponent[] Elements;

    public class ElementComponent
    {
        public Color Color;
        public float StartT;
        public float Exp;
    }

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradient comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(BloomPrePassBackgroundColorsGradient comp)
    {
        comp.ExecutionTimeType = (BloomPrePassNonLightPass.ExecutionTime)ExecutionTimeType;
        comp.TintColor = TintColor;
        comp.Elements = Elements
            .Select(x =>
                new BloomPrePassBackgroundColorsGradient.Element { Color = x.Color, StartT = x.StartT, Exp = x.Exp })
            .ToArray();
    }
}
