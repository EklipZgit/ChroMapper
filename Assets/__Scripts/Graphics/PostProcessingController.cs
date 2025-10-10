using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class PostProcessingController : MonoBehaviour
{
    [SerializeField] private PostProcessVolume volume;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityLabel;
    [SerializeField] private Toggle chromaticAberration;

    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.PostProcessingIntensity), UpdatePostProcessIntensity);
        Settings.NotifyBySettingName(nameof(Settings.ChromaticAberration), UpdateChromaticAberration);
        Settings.NotifyBySettingName(nameof(Settings.HighQualityBloom), UpdateHighQualityBloom);

        UpdatePostProcessIntensity(Settings.Instance.PostProcessingIntensity);
        UpdateChromaticAberration(Settings.Instance.ChromaticAberration);
        UpdateHighQualityBloom(Settings.Instance.HighQualityBloom);
    }

    public void UpdatePostProcessIntensity(object o)
    {
        var v = Convert.ToSingle(o);
        volume.profile.TryGetSettings(out CustomBloom bloom);
        bloom.intensity.value = v * 60f; // TODO: ok, default definitely needed to be change
    }

    public void UpdateChromaticAberration(object o)
    {
        var enabled = Convert.ToBoolean(o);
        volume.profile.TryGetSettings(out ChromaticAberration ca);
        ca.active = enabled;
    }

    public void UpdateHighQualityBloom(object obj)
    {
        var enabled = Convert.ToBoolean(obj);
        volume.profile.TryGetSettings(out CustomBloom bloom);
        bloom.fastMode.value = !enabled;
    }
}
