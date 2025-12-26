using Newtonsoft.Json;

public class Parametric3SliceSpriteControllerComponent
{
    [JsonProperty("widthMultiplier")] public float WidthMultiplier;
    [JsonProperty("alphaStart")] public float AlphaStart;
    [JsonProperty("alphaEnd")] public float AlphaEnd;
    [JsonProperty("alphaMultiplier")] public float AlphaMultiplier;
    [JsonProperty("width")] public float Width;
    [JsonProperty("widthStart")] public float WidthStart;
    [JsonProperty("widthEnd")] public float WidthEnd;
    [JsonProperty("center")] public float Center;
    [JsonProperty("length")] public float Length;
    [JsonProperty("minAlpha")] public float MinAlpha;

    public void CopyTo(LightObjectParametric3SliceSprite lightObject)
    {
        lightObject.WidthMultiplier = WidthMultiplier;
        lightObject.AlphaStart = AlphaStart;
        lightObject.AlphaEnd = AlphaEnd;
        lightObject.AlphaMultiplier = AlphaMultiplier;
        lightObject.Width = Width;
        lightObject.WidthStart = WidthStart;
        lightObject.WidthEnd = WidthEnd;
        lightObject.Center = Center;
        lightObject.Length = Length;
        lightObject.MinAlpha = MinAlpha;
    }
}
