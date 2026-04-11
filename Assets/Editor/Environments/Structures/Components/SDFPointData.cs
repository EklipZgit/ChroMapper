using UnityEngine;

public class SDFPointData : EnvironmentComponentData<SDFPoint>
{
    public float Radius;

    public override void SearchAndFillComponents(GameObject self, SDFPoint comp, CreateContainer container) { }
    public override void CopyTo(SDFPoint comp) => comp.Radius = Radius;
}
