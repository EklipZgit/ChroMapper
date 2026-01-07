using Newtonsoft.Json;

public class Parametric3SliceSpriteControllerComponent
{
    public float WidthMultiplier;
    public float AlphaStart;
    public float AlphaEnd;
    public float AlphaMultiplier;
    public float Width;
    public float WidthStart;
    public float WidthEnd;
    public float Center;
    public float Length;
    public float MinAlpha;

    public void CopyTo(ParametricSpriteLight parametricSpriteLightObject)
    {
        parametricSpriteLightObject.WidthMultiplier = WidthMultiplier;
        parametricSpriteLightObject.AlphaStart = AlphaStart;
        parametricSpriteLightObject.AlphaEnd = AlphaEnd;
        parametricSpriteLightObject.AlphaMultiplier = AlphaMultiplier;
        parametricSpriteLightObject.Width = Width;
        parametricSpriteLightObject.WidthStart = WidthStart;
        parametricSpriteLightObject.WidthEnd = WidthEnd;
        parametricSpriteLightObject.Center = Center;
        parametricSpriteLightObject.Length = Length;
        parametricSpriteLightObject.MinAlpha = MinAlpha;
    }
}
