using UnityEngine;

public class FloatSDFPointScaleEffectComponent
{
    public string ColorPoints;
    public float[] ValueBounds;

    public void CopyTo(SDFPointScaleFx target) => target.ValueBounds = ConvertUtils.ToVector2(ValueBounds);
}
