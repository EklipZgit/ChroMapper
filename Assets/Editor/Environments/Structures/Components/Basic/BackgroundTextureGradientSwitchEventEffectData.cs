using UnityEngine;

public class BackgroundTextureGradientSwitchEventEffectData : EnvironmentComponentData<BackgroundTextureGradientSwitch>
{
    public string DefaultTextureGradient;
    public string BoostTextureGradient;

    public override void SearchAndFillComponents(
        GameObject self,
        BackgroundTextureGradientSwitch comp,
        CreateContainer container)
    {
        comp.DefaultTextureGradient = container
            .GetGameObjectOrNull(DefaultTextureGradient, self)
            .GetComponent<BloomPrePassBackgroundColorsGradient>();
        comp.BoostTextureGradient = container
            .GetGameObjectOrNull(BoostTextureGradient, self)
            .GetComponent<BloomPrePassBackgroundColorsGradient>();
    }

    public override void CopyTo(BackgroundTextureGradientSwitch comp)
    {
    }
}
