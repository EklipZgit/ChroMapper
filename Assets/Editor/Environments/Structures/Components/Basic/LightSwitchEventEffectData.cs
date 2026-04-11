using UnityEngine;

public class LightSwitchEventEffectData : EnvironmentComponentData<BasicLightEffect>
{
    public string EventType;
    public float OffColorIntensity;
    public bool LightOnStart;
    public int LightsId;

    public override void SearchAndFillComponents(GameObject self, BasicLightEffect comp, CreateContainer container) { }

    public override void CopyTo(BasicLightEffect comp)
    {
        comp.OffIntensity = OffColorIntensity;
        comp.LightOnStart = LightOnStart;
    }
}
