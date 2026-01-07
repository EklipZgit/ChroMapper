using Newtonsoft.Json;

public class RectangleFakeGlowComponent : EnvDataComponent<RectangleFakeGlowLightController>
{
    [JsonProperty("rectangleSize")] public float[] Size;
    public float EdgeSize = 0.1f;

    public override void CopyTo(RectangleFakeGlowLightController target)
    {
        target.Size = ConvertUtils.ToVector2(Size);
        target.EdgeSize = EdgeSize;
    }
}
