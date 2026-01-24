public class FloatLocalScaleEffectComponent
{
    public string[] Transforms;
    public float[] ValueBounds;

    public void CopyTo(LocalScaleFx target) => target.ValueBounds = ConvertUtils.ToVector2(ValueBounds);
}
