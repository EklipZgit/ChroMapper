using UnityEngine;

public class SpectrogramMultiplierFloatFxEffectTarget : FxTarget
{
    [SerializeField]
    private SpectrogramPropertyRowAnimator spectrogram;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void SetValue(int group, int id, float value)
    {
        if (!((Object)spectrogram != (Object)null)) return;
        spectrogram.SetMultiplier(value);
    }
    public override void TriggerValue(int group, int id, float value)
    {
        if (!((Object)spectrogram != (Object)null)) return;
        spectrogram.SetMultiplier(value);
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        if (spectrogram == null)
            spectrogram = GetComponent<SpectrogramPropertyRowAnimator>();
    }
#endif
}
