using System;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientElementLightController : LightController
{
    [SerializeField] public BloomPrePassBackgroundColorsGradient BloomPrePassBackgroundColorsGradient;
    [SerializeField] public Element[] Elements;

    protected override bool Initialize() => BloomPrePassBackgroundColorsGradient != null;

    public override void SetColor(Color color)
    {
        foreach (var element in Elements)
        {
            BloomPrePassBackgroundColorsGradient.Elements[element.ElementNumber].Color =
                color * Mathf.Max(color.a * element.Intensity, element.MinIntensity);
        }

        BloomPrePassBackgroundColorsGradient.UpdateGradientTexture();
    }

    [Serializable]
    public class Element
    {
        public int ElementNumber;
        public float Intensity = 1f;
        public float MinIntensity;
    }
}
