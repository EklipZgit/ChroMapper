using Newtonsoft.Json;

public class SpriteLightWithIdComponent : EnvDataComponent<LightController>
{
    [JsonProperty("lightId")] public float LightId;
    [JsonProperty("lightIntensity")] public float LightIntensity;

    [JsonProperty("sprite")] public SpriteData Sprite;

    public class SpriteData
    {
        public string Name;
        public string TextureName;
        public float[] Size;
        public string[] Materials;
    }

    public override void CopyTo(LightController target)
    {
    }
}
