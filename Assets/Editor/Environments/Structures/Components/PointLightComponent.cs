public class PointLightComponent : EnvDataComponent<PointLight>
{
    public float Intensity;

    public override void CopyTo(PointLight target) => target.Intensity = Intensity;
}
