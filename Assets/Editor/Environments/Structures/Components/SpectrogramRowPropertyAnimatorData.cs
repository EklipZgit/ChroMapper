using UnityEngine;

public class SpectrogramRowPropertyAnimatorData : EnvironmentComponentData<SpectrogramRowPropertyAnimator>
{
    public string MaterialPropertyBlockController;
    public int DataIndex;
    public string PropertyName;
    public float Multiplier;
    public AnimationCurveData AnimationCurve;

    public override void SearchAndFillComponents(
        GameObject self,
        SpectrogramRowPropertyAnimator comp,
        CreateContainer container)
    {
        comp.MpbController = container
            .GetGameObjectOrNull(MaterialPropertyBlockController, self)
            .GetComponent<MaterialPropertyBlockController>();
    }

    public override void CopyTo(SpectrogramRowPropertyAnimator comp)
    {
        comp.DataIndex = DataIndex;
        comp.PropertyName = PropertyName;
        comp.Multiplier = Multiplier;
        comp.AnimationCurve = AnimationCurve.Create();
    }
}
