using Newtonsoft.Json;
using UnityEngine;

public class SpriteLightWithIdComponent : EnvironmentComponent<LightingObject>
{
    [JsonProperty("intensity")]
    public float Intensity = -1;
    [JsonProperty("spriteName")]
    public string SpriteName = "";
    
    [JsonProperty("chromaLight")]
    public ChromaLightComponent ChromaLight;


    public override void CopyTo(LightingObject target)
    {
    }
}
