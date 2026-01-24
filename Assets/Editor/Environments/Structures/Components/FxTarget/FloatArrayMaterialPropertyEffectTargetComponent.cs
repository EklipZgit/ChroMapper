public class FloatArrayMaterialPropertyEffectTargetComponent
{
    public string[] MaterialPropertyBlockControllerId;
    public string PropertyName;

    public float[] ValueBounds;
    public float GranularityMultiplier;

    public void CopyTo(MpbArrayFx target)
    {
        target.PropertyName = PropertyName;
        target.ValueBounds = ConvertUtils.ToVector2(ValueBounds);
        target.GranularityMultiplier = GranularityMultiplier;
    }
}
