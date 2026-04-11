using UnityEngine;

public class VertexDisplacementFloatFxGroupEffectTargetData : EnvironmentComponentData<VertexDisplacementFx>
{
    public Vector3 DisplacementRanges;
    public AnimationCurveData XAnimationCurve;
    public AnimationCurveData YAnimationCurve;
    public AnimationCurveData ZAnimationCurve;
    public string DisplacementController;
    public string Renderer;
    public bool UseTestValue;
    public float TestFloatValue;

    public override void SearchAndFillComponents(GameObject self, VertexDisplacementFx comp, CreateContainer container)
    {
        comp.DisplacementController = container
            .GetGameObjectOrNull(DisplacementController, self)
            .GetComponent<MaterialPropertyBlockController>();
        comp.Renderer = container.GetGameObjectOrNull(Renderer, self).GetComponent<Renderer>();
    }

    public override void CopyTo(VertexDisplacementFx comp)
    {
        comp.DisplacementRanges = DisplacementRanges;
        comp.XAnimationCurve = XAnimationCurve.Create();
        comp.YAnimationCurve = YAnimationCurve.Create();
        comp.ZAnimationCurve = ZAnimationCurve.Create();
        comp.UseTestValue = UseTestValue;
        comp.TestFloatValue = TestFloatValue;
    }
}
