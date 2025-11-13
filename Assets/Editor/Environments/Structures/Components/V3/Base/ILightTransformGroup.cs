using UnityEngine;

public interface ILightTransformGroup
{
    bool MirrorX { get; set; }
    bool MirrorY { get; set; }
    bool MirrorZ { get; set; }
    string[] XTransforms { get; set; }
    string[] YTransforms { get; set; }
    string[] ZTransforms { get; set; }
    int Count { get; set; }
}
