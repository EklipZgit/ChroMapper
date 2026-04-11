using UnityEngine;

public class ParametricBoxControllerData : EnvironmentComponentData<ParametricBoxLight>
{
    public float AlphaStart;
    public float AlphaEnd;
    public float AlphaMultiplier;
    public float Width;
    public float WidthStart;
    public float WidthEnd;
    public float Center;
    public float Height;
    public float Length;
    public float MinAlpha;

    public override void SearchAndFillComponents(GameObject self, ParametricBoxLight comp, CreateContainer container) { }

    public override void CopyTo(ParametricBoxLight comp)
    {
        comp.AlphaStart = AlphaStart;
        comp.AlphaEnd = AlphaEnd;
        comp.AlphaMultiplier = AlphaMultiplier;
        comp.Width = Width;
        comp.WidthStart = WidthStart;
        comp.WidthEnd = WidthEnd;
        comp.Center = Center;
        comp.Height = Height;
        comp.Length = Length;
        comp.MinAlpha = MinAlpha;
    }
}
