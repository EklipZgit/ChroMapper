using UnityEngine;

public class SpriteLightWithIdComponent : EnvironmentComponent<LightingObject>
{
    public float Intensity = -1;
    public string SpriteName = "";

    public ChromaLightComponent ChromaLight;


    public override void CopyTo(LightingObject target)
    {
    }
}
