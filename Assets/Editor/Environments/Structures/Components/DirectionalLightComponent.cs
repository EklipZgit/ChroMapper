public class DirectionalLightComponent : EnvDataComponent<DirectionalLight>
{
    public float lightIntensity;
    public float lightRadius;

    public override void CopyTo(DirectionalLight target)
    {
        target.Intensity = lightIntensity;
        target.Radius = lightRadius;
    }
}
