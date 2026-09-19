using UnityEngine;

[CreateAssetMenu(fileName = "PyramidBloomProfile", menuName = "Graphics/Pyramid Bloom Profile")]
public sealed class PyramidBloomProfileSO : ScriptableObject
{
    // Radius controls pyramid depth and fractional sampling scale.
    [SerializeField, Min(0f)] private float radius = 5f;
    // Intensity shapes merge weights; composition applies its own blend factor.
    [SerializeField, Min(0f)] private float intensity = 1f;
    // These values shape destination-level versus accumulated-pyramid weights.
    [SerializeField, Min(0f)] private float pyramidWeightsParam = 0.01f;
    [SerializeField] private float downIntensityOffset = 1f;
    // Boundary multipliers affect both inputs to their respective merge.
    [SerializeField] private float firstUpsampleBrightness = 1f;
    [SerializeField] private float finalUpsampleBrightness = 1f;
    // BloomThreshold gates RGB by source alpha; exposure controls apply only to bloom fog.
    [SerializeField, Min(0f)] private float bloomThreshold = 4f;
    [SerializeField, Min(0f)] private float autoExposureLimit = 1000f;
    [SerializeField] private bool legacyAutoExposure;

    public float Radius => radius;
    public float Intensity => intensity;
    public float PyramidWeightsParam => pyramidWeightsParam;
    public float DownIntensityOffset => downIntensityOffset;
    public float FirstUpsampleBrightness => firstUpsampleBrightness;
    public float FinalUpsampleBrightness => finalUpsampleBrightness;
    public float BloomThreshold => bloomThreshold;
    public float AutoExposureLimit => autoExposureLimit;
    public bool LegacyAutoExposure => legacyAutoExposure;
}
