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
        Settings.NotifyBySettingName(nameof(Settings.ChromaticAberration), UpdateChromaticAberration);
        Settings.NotifyBySettingName(nameof(Settings.HighQualityBloom), UpdateHighQualityBloom);

        UpdateChromaticAberration(Settings.Instance.ChromaticAberration);
        UpdateHighQualityBloom(Settings.Instance.HighQualityBloom);
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
