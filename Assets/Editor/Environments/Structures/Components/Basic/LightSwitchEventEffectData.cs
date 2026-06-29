using UnityEngine;

public class LightSwitchEventEffectData
{
    public string EventType;
    public float OffColorIntensity;
    public bool LightOnStart;
    public int LightsId;

    public void FillComponents(GameObject self, BasicLightEffect comp, CreateContainer container)
    {
        comp.OffIntensity = OffColorIntensity;
        comp.LightOnStart = LightOnStart;
    }
}
