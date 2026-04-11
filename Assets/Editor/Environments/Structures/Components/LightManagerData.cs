using UnityEngine;

public class LightManagerData : EnvironmentComponentData<LightManager>
{
    public override void SearchAndFillComponents(GameObject self, LightManager comp, CreateContainer container) { }
    public override void CopyTo(LightManager comp) { }
}
