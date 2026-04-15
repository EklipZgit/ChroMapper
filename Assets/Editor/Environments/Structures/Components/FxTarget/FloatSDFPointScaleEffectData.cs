using UnityEngine;

public class FloatSDFPointScaleEffectData : EnvironmentComponentData<SDFPointScaleFx>
{
    public int ColorPoints;
    public Vector2 ValueBounds;

    public override void FillComponents(GameObject self, SDFPointScaleFx comp, CreateContainer container)
    {
        comp.ColorPoints = container.GetComponentOrNull<SDFPoint>(ColorPoints);
        comp.ValueBounds = ValueBounds;
    }
}
