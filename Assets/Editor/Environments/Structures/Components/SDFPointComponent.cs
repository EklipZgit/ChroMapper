public class SDFPointComponent : EnvDataComponent<SDFPoint>
{
    public float Radius;

    public override void CopyTo(SDFPoint target) => target.Radius = Radius;
}
