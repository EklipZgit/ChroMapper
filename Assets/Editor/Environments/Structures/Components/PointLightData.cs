using UnityEngine;

public class PointLightData : EnvironmentComponentData<PointLight>
{
    public float Intensity;

    public override void SearchAndFillComponents(GameObject self, PointLight comp, CreateContainer container) { }
    public override void CopyTo(PointLight comp) => comp.Intensity = Intensity;
}
