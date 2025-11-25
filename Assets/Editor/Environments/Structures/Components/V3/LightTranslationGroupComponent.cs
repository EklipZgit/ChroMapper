public class LightTranslationGroupComponent : ILightTransformGroup
{
    public float[] xTranslationLimits;
    public float[] yTranslationLimits;
    public float[] zTranslationLimits;
    public float[] xDistributionLimits;
    public float[] yDistributionLimits;
    public float[] zDistributionLimits;
    
    public bool MirrorX { get; set; }
    public bool MirrorY { get; set; }
    public bool MirrorZ { get; set; }
    public string[] XTransforms { get; set; }
    public string[] YTransforms { get; set; }
    public string[] ZTransforms { get; set; }
    public int Count { get; set; }
}
