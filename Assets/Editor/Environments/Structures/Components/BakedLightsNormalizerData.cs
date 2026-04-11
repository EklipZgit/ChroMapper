using UnityEngine;

public class BakedLightsNormalizerData : EnvironmentComponentData<BakedLightsNormalizer>
{
    public float MaxTotalIntensity = 1f;

    public override void SearchAndFillComponents(GameObject self, BakedLightsNormalizer comp, CreateContainer container)
    {
    }

    public override void CopyTo(BakedLightsNormalizer comp) => comp.MaxTotalIntensity = MaxTotalIntensity;
}
