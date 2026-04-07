using UnityEngine;

public class VertexDisplacementFloatFxGroupEffectTargetComponent
{
    public Vector3 DisplacementRanges;
    public AnimationCurveComponent XAnimationCurve;
    public AnimationCurveComponent YAnimationCurve;
    public AnimationCurveComponent ZAnimationCurve;
    public string DisplacementController;
    public string Renderer;
    public bool UseTestValue;
    public float TestFloatValue;

    public void CopyTo(VertexDisplacementFx target)
    {
        target.DisplacementRanges = DisplacementRanges;
        target.XAnimationCurve = XAnimationCurve.Create();
        target.YAnimationCurve = YAnimationCurve.Create();
        target.ZAnimationCurve = ZAnimationCurve.Create();
        target.UseTestValue = UseTestValue;
        target.TestFloatValue = TestFloatValue;
    }
}
