public class Parametric3SliceSpriteWidthEndFloatFxEffectTargetComponent
{
    public string SliceSpriteControllerId;

    public float[] ValueBounds;
    public float ValueMultiplier = 1f;

    public void CopyTo(ParametricSliceEndWidthFx target)
    {
        target.ValueBounds = ConvertUtils.ToVector2(ValueBounds);
        target.ValueMultiplier = ValueMultiplier;
    }
}
