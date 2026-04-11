using UnityEngine;

public class Parametric3SliceSpriteControllerData : EnvironmentComponentData<ParametricSpriteLight>
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

    public override void SearchAndFillComponents(GameObject self, ParametricSpriteLight comp, CreateContainer container)
    {
    }

    public override void CopyTo(ParametricSpriteLight comp)
    {
        comp.WidthMultiplier = WidthMultiplier;
        comp.AlphaStart = AlphaStart;
        comp.AlphaEnd = AlphaEnd;
        comp.AlphaMultiplier = AlphaMultiplier;
        comp.Width = Width;
        comp.WidthStart = WidthStart;
        comp.WidthEnd = WidthEnd;
        comp.Center = Center;
        comp.Length = Length;
        comp.MinAlpha = MinAlpha;
    }
}
