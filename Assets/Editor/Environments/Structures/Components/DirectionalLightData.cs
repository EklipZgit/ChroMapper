using UnityEngine;

public class DirectionalLightData : EnvironmentComponentData<DirectionalLight>
{
    public float LightIntensity;
    public float LightRadius;

    public override void SearchAndFillComponents(GameObject self, DirectionalLight comp, CreateContainer container) { }

    public override void CopyTo(DirectionalLight comp)
    {
        comp.Intensity = LightIntensity;
        comp.Radius = LightRadius;
    }
}
