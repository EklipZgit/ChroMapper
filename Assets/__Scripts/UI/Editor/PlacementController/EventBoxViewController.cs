using System.Globalization;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

public class EventBoxViewController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private GameObject targetObject;

    [Header("Info Text")] [SerializeField] private TextMeshProUGUI eventBoxIdText;
    [SerializeField] private TextMeshProUGUI filteredIdText;

    [Header("Box")] [SerializeField] private ToggleComponent idPrefab;
    [SerializeField] private RectTransform idTransformTarget;

    [Header("Input")] [SerializeField] private ToggleComponent beatDistributionWaveToggle;
    [SerializeField] private ToggleComponent beatDistributionStepToggle;
    [SerializeField] private TextBoxComponent beatDistributionInput;
    [Space] [SerializeField] private ToggleComponent filterTypeSectionToggle;
    [SerializeField] private ToggleComponent filterTypeStepToggle;
    [SerializeField] private TextBoxComponent chunkInput;
    [SerializeField] private ToggleComponent reverseToggle;
    [SerializeField] private TextBoxComponent p0Input;
    [SerializeField] private TextBoxComponent p1Input;
    [SerializeField] private ToggleComponent randomToggle;
    [SerializeField] private ToggleComponent inOrderToggle;
    [SerializeField] private TextBoxComponent seedInput;
    [SerializeField] private GameObject axisObject;
    [SerializeField] private ToggleComponent axisXToggle;
    [SerializeField] private ToggleComponent axisYToggle;
    [SerializeField] private ToggleComponent axisZToggle;
    [SerializeField] private ToggleComponent flipToggle;
    [Space] [SerializeField] private TextBoxComponent limitInput;
    [SerializeField] private ToggleComponent limitDurationToggle;
    [SerializeField] private ToggleComponent limitDistributionToggle;
    [Space] [SerializeField] private ToggleComponent valueDistributionWaveToggle;
    [SerializeField] private ToggleComponent valueDistributionStepToggle;
    [SerializeField] private TextBoxComponent valueDistributionInput;
    [SerializeField] private ToggleComponent affectFirstToggle;
    [SerializeField] private DropdownComponent easeTypeDropdown;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        HandleEditModeChanged(editModeContext.EditingMode);
        easeTypeDropdown.WithOptions(Easing.IDToFullName.Values);
    }

    private void OnDestroy()
    {
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    private void HandleEditModeChanged(EditingMode mode) => targetObject.SetActive(mode.HasFlag(EditingMode.EventBox));

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        var (box, count) = group switch
        {
            BaseLightColorEventBoxGroup lcebg => ((BaseEventBox)lcebg.Boxes.FirstOrDefault(), lcebg.Boxes.Count),
            BaseLightRotationEventBoxGroup lrebg => (lrebg.Boxes.FirstOrDefault(), lrebg.Boxes.Count),
            BaseLightTranslationEventBoxGroup ltebg => (ltebg.Boxes.FirstOrDefault(), ltebg.Boxes.Count),
            BaseVfxEventEventBoxGroup ffebg => (ffebg.Boxes.FirstOrDefault(), ffebg.Boxes.Count),
            _ => (null, 0)
        };

        foreach (Transform t in idTransformTarget) Destroy(t.gameObject);
        for (var i = 0; i < count; i++)
        {
            var idButton = Instantiate(idPrefab, idTransformTarget);
            idButton.WithLabel((i + 1).ToString());
            idButton.gameObject.SetActive(true);
        }

        eventBoxIdText.text = $"1  |  {count}";
        HandleEventBoxChanged(group, box);
    }

    private void HandleEventBoxChanged(BaseEventBoxGroup group, BaseEventBox box)
    {
        var count = box switch
        {
            BaseLightColorEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager.IdToEffect
                .TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseLightRotationEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager
                .IdToEffect.TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseLightTranslationEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager
                .IdToEffect.TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseVfxEventEventBox =>
                beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager.IdToEffect.TryGetValue(
                    group.ID,
                    out var fx)
                    ? fx.Count
                    : 0,
            _ => 0
        };

        if (box == null) return;

        var ifh = IndexFilterHelper.Convert(box.IndexFilter, count);
        filteredIdText.text = $"{count}  |  {ifh.Count}  |  {ifh.VisibleCount}";

        beatDistributionWaveToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Wave);
        beatDistributionStepToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Step);
        beatDistributionInput.SetValueWithoutNotify(box.BeatDistribution.ToString(CultureInfo.InvariantCulture));

        filterTypeSectionToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.Division);
        filterTypeStepToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset);
        chunkInput.SetValueWithoutNotify(box.IndexFilter.Chunks.ToString(CultureInfo.InvariantCulture));

        reverseToggle.SetValueWithoutNotify(box.IndexFilter.Reverse == 1);
        if (box.IndexFilter.Type == (int)IndexFilterType.Division)
        {
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0.ToString(CultureInfo.InvariantCulture));
            p0Input.SetLabelText("Section");
            p1Input.SetValueWithoutNotify((box.IndexFilter.Param1 + 1).ToString(CultureInfo.InvariantCulture));
            p1Input.SetLabelText("ID");
        }
        else
        {
            p0Input.SetValueWithoutNotify((box.IndexFilter.Param0 + 1).ToString(CultureInfo.InvariantCulture));
            p0Input.SetLabelText("ID");
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1.ToString(CultureInfo.InvariantCulture));
            p1Input.SetLabelText("Step");
        }


        randomToggle.SetValueWithoutNotify((box.IndexFilter.Random & (int)RandomType.RandomElements) > 0);
        inOrderToggle.SetValueWithoutNotify((box.IndexFilter.Random & (int)RandomType.KeepOrder) > 0);
        seedInput.SetValueWithoutNotify(box.IndexFilter.Seed.ToString(CultureInfo.InvariantCulture));

        limitInput.SetValueWithoutNotify((box.IndexFilter.Limit * 100f).ToString(CultureInfo.InvariantCulture));
        limitDurationToggle.SetValueWithoutNotify(
            (box.IndexFilter.LimitAffectsType & (int)LimitAlsoAffectType.Duration) > 0);
        limitDistributionToggle.SetValueWithoutNotify(
            (box.IndexFilter.LimitAffectsType & (int)LimitAlsoAffectType.Distribution) > 0);

        switch (box)
        {
            case BaseLightColorEventBox lceb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lceb.BrightnessDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lceb.BrightnessDistributionType == (int)DistributionType.Wave);
                valueDistributionInput.SetValueWithoutNotify(
                    (lceb.BrightnessDistribution * 100f).ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(lceb.BrightnessAffectFirst == 1);
                break;
            case BaseLightRotationEventBox lreb:
                axisObject.SetActive(true);
                axisXToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.X);
                axisYToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.Y);
                axisZToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.Z);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lreb.RotationDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lreb.RotationDistributionType == (int)DistributionType.Wave);
                valueDistributionInput.SetValueWithoutNotify(
                    lreb.RotationDistribution.ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(lreb.RotationAffectFirst == 1);
                break;
            case BaseLightTranslationEventBox lteb:
                axisObject.SetActive(true);
                axisXToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.X);
                axisYToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.Y);
                axisZToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.Z);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lteb.TranslationDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lteb.TranslationDistributionType == (int)DistributionType.Wave);
                valueDistributionInput.SetValueWithoutNotify(
                    (lteb.TranslationDistribution * 100f).ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(lteb.TranslationAffectFirst == 1);
                break;
            case BaseVfxEventEventBox ffeb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Wave);
                valueDistributionInput.SetValueWithoutNotify(
                    (ffeb.VfxDistribution * 100f).ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(ffeb.VfxAffectFirst == 1);
                break;
            default:
                axisObject.SetActive(false);
                break;
        }
    }
}
