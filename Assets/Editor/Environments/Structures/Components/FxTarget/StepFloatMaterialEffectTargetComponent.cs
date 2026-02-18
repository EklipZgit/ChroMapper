public class StepFloatMaterialEffectTargetComponent
{
    public string MaterialPropertyBlockController;
    public string PropertyName;

    public float StepFactor;
    public float StepSize;

    public void CopyTo(MpbStepFx target)
    {
        target.PropertyName = PropertyName;
        target.StepFactor = StepFactor;
        target.StepSize = StepSize;
    }
}
