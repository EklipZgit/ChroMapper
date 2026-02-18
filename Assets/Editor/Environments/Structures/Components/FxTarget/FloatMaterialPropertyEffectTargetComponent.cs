public class FloatMaterialPropertyEffectTargetComponent
{
    public string MaterialPropertyBlockController;
    public string PropertyName;

    public float[] ValueBounds;
    public float GranularityMultiplier;

    public void CopyTo(MpbFx target)
    {
        target.PropertyName = PropertyName;
        target.ValueBounds = ConvertUtils.ToVector2(ValueBounds);
        target.GranularityMultiplier = GranularityMultiplier;
    }
}
