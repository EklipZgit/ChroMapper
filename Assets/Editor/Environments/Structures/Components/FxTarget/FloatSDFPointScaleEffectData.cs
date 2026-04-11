using UnityEngine;

public class FloatSDFPointScaleEffectData : EnvironmentComponentData<SDFPointScaleFx>
{
    public string ColorPoints;
    public Vector2 ValueBounds;

    public override void SearchAndFillComponents(GameObject self, SDFPointScaleFx comp, CreateContainer container) =>
        comp.ColorPoints = container.GetGameObjectOrNull(ColorPoints, self).GetComponent<SDFPoint>();

    public override void CopyTo(SDFPointScaleFx comp) => comp.ValueBounds = ValueBounds;
}
