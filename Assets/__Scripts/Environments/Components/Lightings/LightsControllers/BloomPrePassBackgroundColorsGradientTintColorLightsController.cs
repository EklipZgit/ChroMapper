using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorLightsController : CombinedLightsController
{
    [SerializeField] public BloomPrePassBackgroundColorsGradient BloomPrePassBackgroundColorsGradient;
    [SerializeField] public bool UseGrayscale;
    [SerializeField] public float GrayscaleFactor;

    protected override bool Initialize() => BloomPrePassBackgroundColorsGradient != null;

    public override void SetColor(Color color)
    {
        if (UseGrayscale)
            color = Color.Lerp(color, Color.white * color.maxColorComponent, Mathf.Clamp01(GrayscaleFactor));
        BloomPrePassBackgroundColorsGradient.TintColor = color;
    }
}
