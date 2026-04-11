using UnityEngine;

public class SpectrogramMultiplierFloatFxEffectTargetData : EnvironmentComponentData<SpectrogramMultiplierFx>
{
    public string Spectrogram;

    public override void SearchAndFillComponents(GameObject self, SpectrogramMultiplierFx comp, CreateContainer container)
    {
        comp.SpectrogramRow = container
            .GetGameObjectOrNull(Spectrogram, self)
            .GetComponent<SpectrogramRowPropertyAnimator>();
    }

    public override void CopyTo(SpectrogramMultiplierFx comp) { }
}
