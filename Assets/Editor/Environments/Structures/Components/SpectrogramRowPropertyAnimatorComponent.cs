public class SpectrogramRowPropertyAnimatorComponent : EnvDataComponent<SpectrogramRowPropertyAnimator>
{
    public string MaterialPropertyBlockController;
    public int DataIndex;
    public string PropertyName;
    public float Multiplier;
    public AnimationCurveComponent AnimationCurve;

    public override void CopyTo(SpectrogramRowPropertyAnimator target)
    {
        target.DataIndex = DataIndex;
        target.PropertyName = PropertyName;
        target.Multiplier = Multiplier;
        target.AnimationCurve = AnimationCurve.Create();
    }
}
