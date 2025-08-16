using Newtonsoft.Json;
using UnityEngine;

public class EnvironmentTransformComponent : EnvironmentComponent<Transform>
{
    [JsonProperty("position")]
    public Vector3 Position = Vector3.zero;

    [JsonProperty("rotation")]
    public Vector3 Rotation = Vector3.zero;

    [JsonProperty("scale")]
    public Vector3 Scale = Vector3.one;

    public override void CopyTo(Transform target)
    {
        target.position = Position;
        target.eulerAngles = Rotation;
        target.localScale = Scale;
    }
}
