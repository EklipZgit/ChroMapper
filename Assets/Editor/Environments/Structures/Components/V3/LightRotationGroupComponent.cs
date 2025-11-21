using UnityEngine;

public class LightRotationGroupComponent : ILightTransformGroup
{
    public bool MirrorX { get; set; }
    public bool MirrorY { get; set; }
    public bool MirrorZ { get; set; }
    public string[] XTransforms { get; set; }
    public string[] YTransforms { get; set; }
    public string[] ZTransforms { get; set; }
    public int Count { get; set; }
}
