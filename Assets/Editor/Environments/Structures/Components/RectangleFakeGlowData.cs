using Newtonsoft.Json;
using UnityEngine;

public class RectangleFakeGlowData : EnvironmentComponentData<RectangleFakeGlowLightController>
{
    [JsonProperty("rectangleSize")] public Vector2 Size;
    public float EdgeSize = 0.1f;

    public override void SearchAndFillComponents(
        GameObject self,
        RectangleFakeGlowLightController comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(RectangleFakeGlowLightController comp)
    {
        comp.Size = Size;
        comp.EdgeSize = EdgeSize;
    }
}
