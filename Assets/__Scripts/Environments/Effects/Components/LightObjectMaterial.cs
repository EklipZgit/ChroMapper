using UnityEngine;

public class LightObjectMaterial : LightObject
{
    public float AlphaIntensity;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public bool MultiplyColorWithAlpha;
    public bool MultiplyColor;
    public float ColorMultiplier;
    public float Alpha;

    protected virtual void Start() => Mpb = new MaterialPropertyBlock();

    public override void UpdateLighting(Color color)
    {
        Mpb.SetColor(colorId, ModifyColor(color));
        Renderer.SetPropertyBlock(Mpb);
    }

    protected virtual Color ModifyColor(Color color)
    {
        var adjustedColor = color;
        color.a *= AlphaIntensity;
        // if (SetAlphaOnly)
        //     adjustedColor.a = color.a;
        // else
        adjustedColor = (AlphaIntoColor ? new Color(color.a, color.a, color.a) : color);

        if (SetColorOnly) adjustedColor.a = Alpha;

        var alpha = 1f;
        if (MultiplyColorWithAlpha) alpha *= color.a;

        if (MultiplyColor) alpha *= ColorMultiplier;

        if (MultiplyColorWithAlpha || MultiplyColor)
        {
            adjustedColor.r *= alpha;
            adjustedColor.g *= alpha;
            adjustedColor.b *= alpha;
        }

        return adjustedColor;
    }
}
