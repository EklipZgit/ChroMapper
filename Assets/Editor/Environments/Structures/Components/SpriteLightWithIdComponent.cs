using Newtonsoft.Json;

public class SpriteLightWithIdComponent : EnvironmentComponent<LightController>
{
    [JsonProperty("intensity")]
    public float Intensity = -1;
    [JsonProperty("spriteName")]
    public string SpriteName = "";
    
    [JsonProperty("chromaLight")]
    public ChromaLightComponent ChromaLight;


    public override void CopyTo(LightController target)
    {
    }
}
