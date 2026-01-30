using UnityEngine;

public class FloatLocalScaleEffectComponent
{
    public string[] Transforms;
    public float[] ValueBounds;
    public Vector3 StartScale;

    public void CopyTo(LocalScaleFx target)
    {
        target.transform.localScale = StartScale;
        target.ValueBounds = ConvertUtils.ToVector2(ValueBounds);
    }
}
